using System;
using System.Collections.Generic;
using NUnit.Framework;
using VitaMahjongNumber.Board;

namespace VitaMahjongNumber.Tests.Board
{
    /// <summary>
    /// Stress tests: generate boards at scale and verify EVERY one using the
    /// solution-sequence replay validator (O(N), 100% accurate — not bounded BFS).
    ///
    /// These tests are the definitive proof of the 100% solvability guarantee.
    /// If any test here fails, the algorithm has a bug.
    /// </summary>
    [TestFixture]
    public class StressTests
    {
        private BoardGenerator _gen;
        private LayoutTemplate _smallLayout;
        private LayoutTemplate _standardLayout;

        [SetUp]
        public void SetUp()
        {
            _gen            = new BoardGenerator();
            _smallLayout    = LayoutTemplate.CreateSmall36();
            _standardLayout = LayoutTemplate.CreateStandard144();
        }

        private BoardResult MakeClassic(LayoutTemplate layout, int seed, int maxRetries = 50)
        {
            var pool = TileDistribution.BuildClassicPool(Math.Max(1, layout.TileCount / 18));
            return _gen.Generate(layout, pool, MatchMode.Classic, new Random(seed), maxRetries);
        }

        private BoardResult MakeMath(LayoutTemplate layout, int seed, int maxRetries = 50)
        {
            // BuildMathPool(n) = 10*n tiles; pick n so pool >= layout, then trim to exact size
            int n    = Math.Max(1, (int)Math.Ceiling(layout.TileCount / 10.0));
            var pool = TileDistribution.BuildMathPool(n);
            while (pool.Count > layout.TileCount) pool.RemoveAt(pool.Count - 1);
            if (pool.Count % 2 != 0) pool.RemoveAt(pool.Count - 1);
            return _gen.Generate(layout, pool, MatchMode.Math, new Random(seed), maxRetries);
        }

        // -----------------------------------------------------------------------
        // Classic Mode — small layout
        // -----------------------------------------------------------------------

        /// <summary>
        /// 1 000 Classic Mode boards, seeds 0..999.
        /// Every board must generate successfully AND pass solution-sequence replay.
        /// </summary>
        [Test]
        [Timeout(30000)]
        public void Stress_1000ClassicBoards_AllVerifiedSolvable()
        {
            int nullCount = 0;
            int failCount = 0;
            var failedSeeds = new List<int>();

            for (int seed = 0; seed < 1000; seed++)
            {
                var board = MakeClassic(_smallLayout, seed);
                if (board == null) { nullCount++; failedSeeds.Add(seed); continue; }

                if (!BFSValidator.ValidateSolutionSequence(board.Placement, board.SolutionSequence, MatchMode.Classic))
                { failCount++; failedSeeds.Add(seed); }
            }

            Assert.AreEqual(0, nullCount,
                $"{nullCount}/1000 boards returned null. Failing seeds: {string.Join(",", failedSeeds.GetRange(0, System.Math.Min(10, failedSeeds.Count)))}");
            Assert.AreEqual(0, failCount,
                $"{failCount}/1000 boards failed solution-sequence validation.");
        }

        // -----------------------------------------------------------------------
        // Math Mode — small layout
        // -----------------------------------------------------------------------

        /// <summary>
        /// 200 Math Mode boards. Every board must generate AND pass validation.
        /// </summary>
        [Test]
        [Timeout(30000)]
        public void Stress_200MathModeBoards_AllVerifiedSolvable()
        {
            int nullCount = 0;
            int failCount = 0;

            for (int seed = 0; seed < 200; seed++)
            {
                var board = MakeMath(_smallLayout, seed);
                if (board == null) { nullCount++; continue; }

                if (!BFSValidator.ValidateSolutionSequence(board.Placement, board.SolutionSequence, MatchMode.Math))
                    failCount++;
            }

            Assert.AreEqual(0, nullCount, $"{nullCount}/200 Math Mode boards returned null");
            Assert.AreEqual(0, failCount, $"{failCount}/200 Math Mode boards failed validation");
        }

        // -----------------------------------------------------------------------
        // Standard 144-tile layout
        // -----------------------------------------------------------------------

        /// <summary>
        /// 100 standard 144-tile boards. Tests the hardest layout configuration.
        /// </summary>
        [Test]
        [Timeout(60000)]
        public void Stress_100StandardBoards_AllVerifiedSolvable()
        {
            int nullCount = 0;
            int failCount = 0;

            for (int seed = 0; seed < 100; seed++)
            {
                var pool  = TileDistribution.BuildClassicPool(8); // 144 tiles
                var board = _gen.Generate(_standardLayout, pool, MatchMode.Classic,
                    new Random(seed), maxRetries: BoardGenerator.MAX_RETRIES_HARD);

                if (board == null) { nullCount++; continue; }

                if (!BFSValidator.ValidateSolutionSequence(board.Placement, board.SolutionSequence, MatchMode.Classic))
                    failCount++;
            }

            Assert.AreEqual(0, nullCount, $"{nullCount}/100 standard boards returned null");
            Assert.AreEqual(0, failCount, $"{failCount}/100 standard boards failed validation");
        }

        // -----------------------------------------------------------------------
        // Solution sequence structural integrity
        // -----------------------------------------------------------------------

        /// <summary>
        /// Every solution sequence must contain exactly TileCount/2 pairs and
        /// every position in the sequence must appear in the placement map.
        /// </summary>
        [Test]
        [Timeout(15000)]
        public void Stress_SolutionSequence_StructuralIntegrity()
        {
            for (int seed = 0; seed < 200; seed++)
            {
                var board = MakeClassic(_smallLayout, seed);
                if (board == null) continue;

                Assert.AreEqual(board.OccupiedCount / 2, board.SolutionSequence.Count,
                    $"seed={seed}: SolutionSequence should have {board.OccupiedCount / 2} pairs");

                foreach (var (posA, posB) in board.SolutionSequence)
                {
                    Assert.IsTrue(board.Placement.ContainsKey(posA),
                        $"seed={seed}: posA={posA} not found in Placement");
                    Assert.IsTrue(board.Placement.ContainsKey(posB),
                        $"seed={seed}: posB={posB} not found in Placement");
                }
            }
        }
    }
}
