using System;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using VitaMahjongNumber.Board;

namespace VitaMahjongNumber.Tests.Board
{
    /// <summary>
    /// Tests for BoardGenerator: solvability, performance, pair balance, retry logic.
    /// </summary>
    [TestFixture]
    public class GeneratorTests
    {
        private BoardGenerator  _gen;
        private LayoutTemplate  _standardLayout; // 144-tile standard layout
        private LayoutTemplate  _smallLayout;    // 36-tile easy layout
        private LayoutTemplate  _impossibleLayout;

        [SetUp]
        public void SetUp()
        {
            _gen             = new BoardGenerator();
            _standardLayout  = LayoutTemplate.CreateStandard144();
            _smallLayout     = LayoutTemplate.CreateSmall36();
            _impossibleLayout = LayoutTemplate.CreateImpossible();
        }

        /// <summary>
        /// Convenience wrapper: builds a matching pool and calls Generate.
        /// pairsPerValue=2 gives 36 tiles (matches CreateSmall36).
        /// pairsPerValue=8 gives 144 tiles (matches CreateStandard144).
        /// </summary>
        private BoardResult GenerateBoard(LayoutTemplate layout, GameMode mode, int seed, int maxRetries)
        {
            var rng          = new Random(seed);
            int pairsPerVal  = Math.Max(1, layout.TileCount / 18); // 18 tiles per pairsPerValue for Classic
            var pool = mode == GameMode.MathMode
                ? BuildMathPoolForSize(layout.TileCount)
                : TileDistribution.BuildClassicPool(pairsPerVal);
            return _gen.Generate(layout, pool, mode.ToMatchMode(), rng, maxRetries);
        }

        private static List<int> BuildMathPoolForSize(int targetCount)
        {
            // BuildMathPool(n) = 10*n tiles. Find n such that pool >= targetCount, then trim.
            int n = Math.Max(1, (int)Math.Ceiling(targetCount / 10.0));
            var pool = TileDistribution.BuildMathPool(n);
            while (pool.Count > targetCount) pool.RemoveAt(pool.Count - 1);
            if (pool.Count % 2 != 0) pool.RemoveAt(pool.Count - 1);
            return pool;
        }

        // Test: 100 consecutive boards with seeds 0..99 are all solvable
        [Test]
        public void Generate_100Boards_AllSolvable()
        {
            int failCount = 0;
            for (int seed = 0; seed < 100; seed++)
            {
                var board = GenerateBoard(_smallLayout, GameMode.Classic, seed, maxRetries: 20);
                Assert.IsNotNull(board, $"Board with seed {seed} returned null");

                bool solvable = BFSValidator.HasSolution(board.Placement, MatchMode.Classic);
                if (!solvable) failCount++;
            }
            Assert.AreEqual(0, failCount, $"{failCount}/100 boards were not solvable");
        }

        // Test: Generation completes in under 500ms for 144-tile board
        [Test]
        public void Generate_StandardBoard_CompletesUnder500ms()
        {
            var pool = TileDistribution.BuildClassicPool(8); // 144 tiles
            var sw   = Stopwatch.StartNew();
            var board = _gen.Generate(_standardLayout, pool, MatchMode.Classic, new Random(42), maxRetries: 50);
            sw.Stop();

            Assert.IsNotNull(board, "Board generation returned null");
            Assert.Less(sw.ElapsedMilliseconds, 500,
                $"Generation took {sw.ElapsedMilliseconds}ms — must be under 500ms on target hardware");
        }

        // Test: Generated board has correct tile count matching layout
        [Test]
        public void Generate_BoardHasCorrectTileCount()
        {
            var board = GenerateBoard(_smallLayout, GameMode.Classic, seed: 1, maxRetries: 20);
            Assert.IsNotNull(board);
            Assert.AreEqual(_smallLayout.TileCount, board.OccupiedCount,
                "Generated board tile count must match layout definition");
        }

        // Test: Generated board has correct pair balance (each value appears even times)
        [Test]
        public void Generate_ClassicMode_PairBalanceCorrect()
        {
            var board = GenerateBoard(_smallLayout, GameMode.Classic, seed: 42, maxRetries: 20);
            Assert.IsNotNull(board);

            var counts = new Dictionary<int, int>();
            foreach (var (_, value) in board.AllTiles())
            {
                if (!counts.ContainsKey(value)) counts[value] = 0;
                counts[value]++;
            }

            foreach (var kv in counts)
                Assert.AreEqual(0, kv.Value % 2,
                    $"Value {kv.Key} appears {kv.Value} times — must be even (pairs only)");
        }

        // Test: Retry mechanism activates and eventually succeeds
        [Test]
        public void Generate_HardSeed_RetriesAndSucceeds()
        {
            var board = GenerateBoard(_smallLayout, GameMode.Classic, seed: 0, maxRetries: 50);
            Assert.IsNotNull(board, "Generator should succeed after retries even on a hard seed");
            Assert.Greater(board.OccupiedCount, 0);
        }

        // Test: When maxRetries exceeded on impossible layout, returns null
        [Test]
        public void Generate_ImpossibleLayout_ReturnsNull()
        {
            // Pool with 2 tiles — impossible layout has only 1 slot so _freeTiles < 2
            var pool  = new List<int> { 1, 1 };
            var board = _gen.Generate(_impossibleLayout, pool, MatchMode.Classic, new Random(99), maxRetries: 3);
            Assert.IsNull(board, "Impossible layout must return null after exhausting retries");
        }
    }
}
