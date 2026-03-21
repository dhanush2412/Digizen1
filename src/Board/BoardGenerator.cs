using System;
using System.Collections.Generic;

namespace VitaMahjongNumber.Board
{
    /// <summary>Match-rule mode used when pairing tiles.</summary>
    public enum MatchMode { Classic, Math }

    /// <summary>
    /// Result of a successful board generation.
    /// Contains the tile placement and a recorded solution sequence.
    /// </summary>
    public sealed class BoardResult
    {
        /// <summary>Position encoded int -> tile value.</summary>
        public readonly Dictionary<int, int> Placement;

        /// <summary>
        /// Ordered list of (PosA, PosB) pairs in REMOVAL order.
        /// Index 0 = first pair the player removes.
        /// This is the REVERSE of the internal placement order,
        /// which is why it is a guaranteed valid solution.
        /// </summary>
        public readonly List<(int PosA, int PosB)> SolutionSequence;

        public int OccupiedCount => Placement.Count;

        public BoardResult(Dictionary<int, int> placement, List<(int, int)> solutionSequence)
        {
            Placement        = placement;
            SolutionSequence = solutionSequence;
        }

        /// <summary>Enumerates all tiles as (Position, Value) tuples.</summary>
        public IEnumerable<(int Position, int Value)> AllTiles()
        {
            foreach (var kv in Placement)
                yield return (kv.Key, kv.Value);
        }
    }

    /// <summary>
    /// Reverse-generation algorithm that GUARANTEES a 100% solvable board.
    ///
    /// Core guarantee: tiles are placed in matched pairs onto free slots.
    /// The REVERSE of the placement sequence is always a valid removal path.
    ///
    /// Bugs fixed vs v1:
    ///   FIX 1 — Initial seeding now includes ALL layout positions (not just z==0).
    ///            On an empty board every position is free; restricting to z=0 meant
    ///            multi-layer boards never had upper-layer tiles placed.
    ///   FIX 2 — PlaceTile now re-evaluates UNOCCUPIED layout neighbours too.
    ///            Placing at z=1 covers z=0; previously the z=0 tile stayed in
    ///            _freeTiles even after being covered, causing invalid states.
    ///   FIX 3 — PlaceTile also evaluates the z+1 neighbour so that tiles above
    ///            the newly placed tile are correctly tracked in the free set.
    ///   FIX 4 — Layout lookup changed from O(n) linear scan to O(1) HashSet.
    ///   FIX 5 — SolutionSequence is recorded and returned in BoardResult.
    /// </summary>
    public sealed class BoardGenerator
    {
        public const int MAX_RETRIES_EASY   =   5;
        public const int MAX_RETRIES_NORMAL =  20;
        public const int MAX_RETRIES_HARD   = 200; // raised from 50 — needed for z=5 layouts

        private const int SCORE_CANDIDATE_CAP = 20;

        // Pre-allocated buffers — reused across retries (zero GC inside retry loop)
        private readonly HashSet<int>       _occupied;
        private readonly HashSet<int>       _freeTiles;
        private readonly List<int>          _neighborScratch;
        private readonly List<int>          _freeScratch;
        private readonly List<int>          _workPool;
        private readonly List<(int, int)>   _placementOrder;

        // Layout slot lookup — O(1), rebuilt each Generate call
        private readonly HashSet<int> _layoutSet;

        public BoardGenerator(int maxTiles = 144)
        {
            _occupied        = new HashSet<int>(maxTiles);
            _freeTiles       = new HashSet<int>(maxTiles);
            _neighborScratch = new List<int>(10);
            _freeScratch     = new List<int>(maxTiles);
            _workPool        = new List<int>(maxTiles);
            _placementOrder  = new List<(int, int)>(maxTiles / 2);
            _layoutSet       = new HashSet<int>(maxTiles);
        }

        /// <summary>
        /// Generates a solvable board. Returns a BoardResult on success,
        /// or null if maxRetries are exhausted.
        /// </summary>
        public BoardResult Generate(
            IReadOnlyList<int> layoutSlots,
            List<int>          tilePool,
            MatchMode          mode,
            Random             rng,
            int                maxRetries)
        {
            TileDistribution.ValidatePairBalance(tilePool, mode);

            // Build O(1) layout lookup — rebuilt once per Generate call
            _layoutSet.Clear();
            foreach (int s in layoutSlots) _layoutSet.Add(s);

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                var result = TryGenerate(layoutSlots, tilePool, mode, rng);
                if (result != null) return result;
            }
            return null;
        }

        private BoardResult TryGenerate(
            IReadOnlyList<int> layoutSlots,
            List<int>          tilePool,
            MatchMode          mode,
            Random             rng)
        {
            // --- Reset state (no allocations) ---
            _occupied.Clear();
            _freeTiles.Clear();
            _placementOrder.Clear();

            // FIX 1: Seed ALL layout positions as free.
            // On an empty board every position satisfies IsFree (nothing is occupied).
            // Previously only z==0 was seeded, so higher layers were never placed.
            foreach (int pos in layoutSlots)
                _freeTiles.Add(pos);

            // Shuffle working copy of pool
            _workPool.Clear();
            _workPool.AddRange(tilePool);
            TileDistribution.Shuffle(_workPool, rng);

            var placement = new Dictionary<int, int>(_workPool.Count);

            // --- Main placement loop ---
            while (_workPool.Count >= 2)
            {
                if (_freeTiles.Count < 2) return null; // deadlock

                // Select two free slots (greedy: minimize-blocking heuristic)
                SelectSlotPair(out int slotA, out int slotB);
                if (slotA < 0 || slotB < 0 || slotA == slotB) return null;

                // Draw valid pair from pool (always from index 0 — pool shrinks via SwapRemove)
                int valueA = _workPool[0];
                int idxB   = FindComplement(_workPool, mode, valueA, skipIdx: 0);
                if (idxB < 0) return null;
                int valueB = _workPool[idxB];

                // Remove pair from pool
                SwapRemovePair(_workPool, 0, idxB);

                // Record for solution sequence
                _placementOrder.Add((slotA, slotB));

                // Place BOTH tiles atomically, then update free set
                PlaceTile(slotA, valueA, placement);
                PlaceTile(slotB, valueB, placement);
            }

            if (placement.Count != layoutSlots.Count) return null;

            // Solution sequence = REVERSE of placement order
            var solution = new List<(int, int)>(_placementOrder.Count);
            for (int i = _placementOrder.Count - 1; i >= 0; i--)
                solution.Add(_placementOrder[i]);

            return new BoardResult(placement, solution);
        }

        /// <summary>
        /// Selects two free slots using a greedy heuristic that prefers slots
        /// which MINIMIZE blocking impact on neighbours (correct for reverse-generation).
        /// </summary>
        private void SelectSlotPair(out int slotA, out int slotB)
        {
            _freeScratch.Clear();
            foreach (int s in _freeTiles)
            {
                _freeScratch.Add(s);
                if (_freeScratch.Count >= SCORE_CANDIDATE_CAP) break;
            }

            if (_freeScratch.Count < 2)
            {
                slotA = slotB = -1;
                return;
            }

            int bestA = -1, bestB = -1;
            int bestScore = int.MaxValue;

            for (int i = 0; i < _freeScratch.Count; i++)
            {
                int scoreI = BlockingScore(_freeScratch[i]);
                for (int j = i + 1; j < _freeScratch.Count; j++)
                {
                    int combined = scoreI + BlockingScore(_freeScratch[j]);
                    if (combined < bestScore)
                    {
                        bestScore = combined;
                        bestA = _freeScratch[i];
                        bestB = _freeScratch[j];
                    }
                }
            }

            slotA = bestA >= 0 ? bestA : _freeScratch[0];
            slotB = bestB >= 0 ? bestB : _freeScratch[1];
        }

        /// <summary>
        /// Returns the number of currently-free neighbours that would become BLOCKED
        /// if a tile were placed at pos. Lower = better slot choice.
        /// </summary>
        private int BlockingScore(int pos)
        {
            TilePosition.DecodePos(pos, out int x, out int y, out int z);
            _occupied.Add(pos); // simulate placement

            TilePosition.GetAffectedNeighbors(x, y, z, _neighborScratch);
            int blocked = 0;
            foreach (int nPos in _neighborScratch)
            {
                if (!_freeTiles.Contains(nPos)) continue;
                TilePosition.DecodePos(nPos, out int nx, out int ny, out int nz);
                if (!TilePosition.IsFree(nx, ny, nz, _occupied))
                    blocked++;
            }

            _occupied.Remove(pos); // undo simulation
            return blocked;
        }

        /// <summary>
        /// Places a tile and updates freeTiles by RE-EVALUATING each affected neighbour.
        ///
        /// FIX 2: Evaluates UNOCCUPIED layout positions too — placing at z=1
        ///         must cover z=0, removing it from _freeTiles even if unplaced.
        /// FIX 3: Also evaluates z+1 neighbour so tiles above are tracked correctly.
        /// FIX 4: Uses _layoutSet (O(1)) instead of linear IsInLayout scan.
        /// </summary>
        private void PlaceTile(int pos, int value, Dictionary<int, int> placement)
        {
            _freeTiles.Remove(pos);
            _occupied.Add(pos);
            placement[pos] = value;

            TilePosition.DecodePos(pos, out int x, out int y, out int z);
            TilePosition.GetAffectedNeighbors(x, y, z, _neighborScratch);

            // FIX 3: Also evaluate the tile directly above — placing here may
            // affect whether z+1 neighbours are correctly tracked in the free set.
            _neighborScratch.Add(TilePosition.EncodePos(x, y, z + 1));

            foreach (int nPos in _neighborScratch)
            {
                if (!_layoutSet.Contains(nPos)) continue;
                if (_occupied.Contains(nPos)) continue; // already placed — not in free set

                TilePosition.DecodePos(nPos, out int nx, out int ny, out int nz);
                if (TilePosition.IsFree(nx, ny, nz, _occupied))
                    _freeTiles.Add(nPos);
                else
                    _freeTiles.Remove(nPos);
            }
        }

        // --- Pool helpers ---

        private static int FindComplement(List<int> pool, MatchMode mode, int valueA, int skipIdx)
        {
            for (int i = 0; i < pool.Count; i++)
            {
                if (i == skipIdx) continue;
                bool match = mode == MatchMode.Classic
                    ? pool[i] == valueA
                    : pool[i] + valueA == 10;
                if (match) return i;
            }
            return -1;
        }

        private static void SwapRemovePair(List<int> pool, int idxA, int idxB)
        {
            int hi = idxA > idxB ? idxA : idxB;
            int lo = idxA > idxB ? idxB : idxA;
            SwapRemove(pool, hi);
            SwapRemove(pool, lo);
        }

        private static void SwapRemove(List<int> list, int idx)
        {
            int last = list.Count - 1;
            if (idx != last) list[idx] = list[last];
            list.RemoveAt(last);
        }
    }
}
