using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace VitaMahjongNumber.Board
{
    /// <summary>
    /// Bounded BFS post-validator. Used as a debug assertion — NOT a runtime guarantee.
    /// The reverse-generation sequence itself is the true solvability proof.
    ///
    /// State hashing: up to 144 tiles encoded into 3 x ulong bitvectors (192 bits).
    /// Budget: MAX_STATES=500 expanded nodes, MAX_DEPTH=10 match steps.
    /// Returns true if a fully-cleared board is found within budget.
    ///
    /// BFS limitation: 500 states at depth 10 explores ~0% of the full state space
    /// for 144-tile boards. Use only as a quick smoke-test. Trust reverse-generation.
    /// </summary>
    public static class BFSValidator
    {
        private const int MAX_STATES = 500;
        private const int MAX_DEPTH  = 10;

        private readonly struct BoardState : IEquatable<BoardState>
        {
            public readonly ulong W0, W1, W2;
            public readonly int   Depth;

            public BoardState(ulong w0, ulong w1, ulong w2, int depth)
            { W0 = w0; W1 = w1; W2 = w2; Depth = depth; }

            public bool Equals(BoardState o) => W0 == o.W0 && W1 == o.W1 && W2 == o.W2;
            public override bool Equals(object obj) => obj is BoardState s && Equals(s);
            public override int GetHashCode() => HashCode.Combine(W0, W1, W2);
        }

        /// <summary>
        /// Returns true if a solution path exists within the bounded search budget.
        /// </summary>
        public static bool HasSolution(Dictionary<int, int> board, MatchMode mode)
        {
            if (board == null || board.Count == 0) return true;
            if (board.Count % 2 != 0) return false;

            // Index positions for bitvector mapping
            var positions = new int[board.Count];
            int b = 0;
            foreach (int pos in board.Keys) positions[b++] = pos;

            // Build initial bitvector (all tiles present)
            ulong w0 = 0, w1 = 0, w2 = 0;
            for (int i = 0; i < positions.Length; i++) SetBit(ref w0, ref w1, ref w2, i);

            var visited = new HashSet<BoardState>(MAX_STATES);
            var queue   = new Queue<BoardState>(MAX_STATES);
            var initial = new BoardState(w0, w1, w2, 0);
            queue.Enqueue(initial);
            visited.Add(initial);

            int expanded = 0;

            while (queue.Count > 0 && expanded < MAX_STATES)
            {
                var state = queue.Dequeue();
                expanded++;

                if (state.W0 == 0 && state.W1 == 0 && state.W2 == 0) return true;
                if (state.Depth >= MAX_DEPTH) continue;

                var occupied = BuildOccupied(positions, state);
                var freeTiles = new List<int>(16);

                for (int i = 0; i < positions.Length; i++)
                {
                    if (!TestBit(state.W0, state.W1, state.W2, i)) continue;
                    TilePosition.DecodePos(positions[i], out int x, out int y, out int z);
                    if (TilePosition.IsFree(x, y, z, occupied)) freeTiles.Add(i);
                }

                for (int ia = 0; ia < freeTiles.Count; ia++)
                for (int ib = ia + 1; ib < freeTiles.Count; ib++)
                {
                    int posA = positions[freeTiles[ia]], valueA = board[posA];
                    int posB = positions[freeTiles[ib]], valueB = board[posB];
                    bool matched = mode == MatchMode.Classic ? valueA == valueB : valueA + valueB == 10;
                    if (!matched) continue;

                    ulong cw0 = state.W0, cw1 = state.W1, cw2 = state.W2;
                    ClearBit(ref cw0, ref cw1, ref cw2, freeTiles[ia]);
                    ClearBit(ref cw0, ref cw1, ref cw2, freeTiles[ib]);
                    var child = new BoardState(cw0, cw1, cw2, state.Depth + 1);
                    if (!visited.Contains(child) && expanded + queue.Count < MAX_STATES)
                    { visited.Add(child); queue.Enqueue(child); }
                }
            }

            return false;
        }

        /// <summary>
        /// Debug-only assertion. Compiled out in release/non-assertion builds.
        /// Faster alternative: replay the stored reverse-generation sequence instead.
        /// </summary>
        [Conditional("UNITY_ASSERTIONS")]
        public static void AssertSolvable(Dictionary<int, int> board, MatchMode mode)
        {
            if (!HasSolution(board, mode))
                throw new InvalidOperationException(
                    "BFSValidator: generated board has no solution within bounded search.");
        }

        /// <summary>
        /// Validates solvability by REPLAYING the stored solution sequence.
        /// This is the definitive O(N) solvability check — 100% accurate.
        ///
        /// Unlike HasSolution (bounded BFS), this replays the exact removal path
        /// recorded during reverse-generation and verifies each step is valid:
        ///   1. Both tiles are currently on the board
        ///   2. Both tiles are free (IsFree == true) at the time of removal
        ///   3. Both tiles match according to the given mode
        /// Board must be empty at the end.
        /// </summary>
        public static bool ValidateSolutionSequence(
            Dictionary<int, int>       placement,
            List<(int PosA, int PosB)> sequence,
            MatchMode                  mode)
        {
            if (placement == null || placement.Count == 0) return true;
            if (sequence  == null || sequence.Count * 2 != placement.Count) return false;

            // Build mutable occupied set from placement
            var occupied = new HashSet<int>(placement.Keys);

            foreach (var (posA, posB) in sequence)
            {
                // Both must still be on the board
                if (!occupied.Contains(posA) || !occupied.Contains(posB)) return false;

                // Both must be free at this point in the game
                TilePosition.DecodePos(posA, out int ax, out int ay, out int az);
                TilePosition.DecodePos(posB, out int bx, out int by, out int bz);
                if (!TilePosition.IsFree(ax, ay, az, occupied)) return false;
                if (!TilePosition.IsFree(bx, by, bz, occupied)) return false;

                // Values must form a valid match
                int valA    = placement[posA];
                int valB    = placement[posB];
                bool matched = mode == MatchMode.Classic ? valA == valB : valA + valB == 10;
                if (!matched) return false;

                // Remove the pair
                occupied.Remove(posA);
                occupied.Remove(posB);
            }

            return occupied.Count == 0; // board must be empty after all removals
        }

        // --- Bitvector helpers ---

        private static void SetBit(ref ulong w0, ref ulong w1, ref ulong w2, int i)
        {
            if      (i < 64)  w0 |= 1UL << i;
            else if (i < 128) w1 |= 1UL << (i - 64);
            else              w2 |= 1UL << (i - 128);
        }

        private static void ClearBit(ref ulong w0, ref ulong w1, ref ulong w2, int i)
        {
            if      (i < 64)  w0 &= ~(1UL << i);
            else if (i < 128) w1 &= ~(1UL << (i - 64));
            else              w2 &= ~(1UL << (i - 128));
        }

        private static bool TestBit(ulong w0, ulong w1, ulong w2, int i)
        {
            if      (i < 64)  return (w0 >> i         & 1UL) != 0;
            else if (i < 128) return (w1 >> (i - 64)  & 1UL) != 0;
            else              return (w2 >> (i - 128) & 1UL) != 0;
        }

        private static HashSet<int> BuildOccupied(int[] positions, in BoardState state)
        {
            var occupied = new HashSet<int>(positions.Length);
            for (int i = 0; i < positions.Length; i++)
                if (TestBit(state.W0, state.W1, state.W2, i))
                    occupied.Add(positions[i]);
            return occupied;
        }
    }
}
