using System;
using System.Collections.Generic;
using NUnit.Framework;
using VitaMahjongNumber.Board;

namespace VitaMahjongNumber.Tests.Board
{
    /// <summary>
    /// Tests for Math Mode tile distribution and matching rules.
    /// In Math Mode, two tiles match when their values sum to 10.
    /// </summary>
    [TestFixture]
    public class MathModeTests
    {
        private BoardGenerator _gen;
        private LayoutTemplate _smallLayout;

        [SetUp]
        public void SetUp()
        {
            _gen         = new BoardGenerator();
            _smallLayout = LayoutTemplate.CreateSmall36();
        }

        private BoardResult GenerateBoard(LayoutTemplate layout, GameMode mode, int seed, int maxRetries)
        {
            var rng  = new Random(seed);
            var pool = mode == GameMode.MathMode
                ? TileDistribution.BuildMathPool(4)  // 40 tiles; generator trims/fills as needed
                : TileDistribution.BuildClassicPool(Math.Max(1, layout.TileCount / 18));
            // Trim or extend pool to match layout tile count exactly
            while (pool.Count > layout.TileCount) pool.RemoveAt(pool.Count - 1);
            if (pool.Count % 2 != 0) pool.RemoveAt(pool.Count - 1);
            return _gen.Generate(layout, pool, mode.ToMatchMode(), rng, maxRetries);
        }

        // Test: Pool distribution is symmetric — count(v) == count(10-v) for v=1..4
        [Test]
        public void BuildPool_MathMode_CountSymmetryHolds()
        {
            var pool   = TileDistribution.BuildMathPool(4); // 40 tiles
            var counts = new Dictionary<int, int>();
            foreach (int v in pool)
            {
                if (!counts.ContainsKey(v)) counts[v] = 0;
                counts[v]++;
            }

            for (int v = 1; v <= 4; v++)
            {
                int complement = 10 - v;
                Assert.AreEqual(counts[v], counts[complement],
                    $"count({v})={counts[v]} must equal count({complement})={counts[complement]}");
            }
        }

        // Test: count(5) is always even (5 is self-pairing)
        [Test]
        public void BuildPool_MathMode_Count5IsAlwaysEven()
        {
            // BuildMathPool(n) produces 10*n tiles; test various n values
            foreach (int n in new[] { 2, 4, 7, 10, 14 })
            {
                var pool   = TileDistribution.BuildMathPool(n);
                int count5 = 0;
                foreach (int v in pool)
                    if (v == 5) count5++;

                Assert.AreEqual(0, count5 % 2,
                    $"pairsPerValue={n}: count(5)={count5} must be even");
            }
        }

        // Test: All matched pairs on a generated Math Mode board sum to 10
        [Test]
        public void Generate_MathMode_AllMatchedPairsSumTo10()
        {
            var board = GenerateBoard(_smallLayout, GameMode.MathMode, seed: 42, maxRetries: 50);
            Assert.IsNotNull(board, "Math Mode board generation should succeed");

            var sequence = board.SolutionSequence;
            Assert.IsNotNull(sequence, "Board must expose its solution sequence");

            foreach (var (posA, posB) in sequence)
            {
                int valA = board.Placement[posA];
                int valB = board.Placement[posB];
                Assert.AreEqual(10, valA + valB,
                    $"Pair ({valA}, {valB}) does not sum to 10");
            }
        }

        // Test: Pool of only 5s (all self-pairing) is valid for Math Mode
        [Test]
        public void BuildPool_OnlyFives_IsValidForMathMode()
        {
            var pool = new List<int> { 5, 5, 5, 5, 5, 5, 5, 5 }; // 8 fives = 4 pairs of 5+5=10

            Assert.AreEqual(8, pool.Count, "Pool should contain exactly 8 tiles");
            foreach (int v in pool)
                Assert.AreEqual(5, v, "All tiles should be value 5");

            bool isValid = true;
            try   { TileDistribution.ValidatePairBalance(pool, MatchMode.Math); }
            catch { isValid = false; }
            Assert.IsTrue(isValid, "Pool of 8 fives should be valid for Math Mode");
        }

        // Test: No-moves detection uses Math Mode rules, not Classic rules
        [Test]
        public void NoMovesDetection_MathMode_UsesCorrectMatchRule()
        {
            // Board with only 1s and no 9s: no valid Math Mode pairs (1+1 != 10)
            // Classic Mode would find pairs (1==1), so this verifies mode routing
            var tileData = new[]
            {
                new TileData(TilePosition.EncodePos(0, 0, 0), value: 1),
                new TileData(TilePosition.EncodePos(2, 0, 0), value: 1),
            };

            var board          = BoardState.FromTileData(tileData);
            var mathStrategy   = new MathModeStrategy();
            var classicStrategy = new ClassicModeStrategy();

            bool mathHasMoves    = board.HasAnyValidPair(mathStrategy);
            bool classicHasMoves = board.HasAnyValidPair(classicStrategy);

            Assert.IsFalse(mathHasMoves,    "Math Mode: two 1s cannot match (1+1 != 10)");
            Assert.IsTrue(classicHasMoves,  "Classic Mode: two 1s can match (same value)");
        }
    }
}
