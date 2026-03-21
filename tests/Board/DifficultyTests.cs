using NUnit.Framework;
using VitaMahjongNumber.Board;

namespace VitaMahjongNumber.Tests.Board
{
    /// <summary>
    /// Tests for LevelConfig difficulty scaling across 100 levels.
    /// Verifies monotonicity, range bounds, and sigmoid midpoint.
    /// </summary>
    [TestFixture]
    public class DifficultyTests
    {
        // Test: Level 1 has fewer tiles than Level 50
        [Test]
        public void LevelConfig_Level1_FewerTilesThanLevel50()
        {
            var cfg1  = LevelConfig.GetConfig(1);
            var cfg50 = LevelConfig.GetConfig(50);

            Assert.Less(cfg1.TileCount, cfg50.TileCount,
                $"Level 1 tileCount={cfg1.TileCount} should be less than Level 50 tileCount={cfg50.TileCount}");
        }

        // Test: Level 1 has shallower maxZ than Level 100
        [Test]
        public void LevelConfig_Level1_ShallowerMaxZThanLevel100()
        {
            var cfg1   = LevelConfig.GetConfig(1);
            var cfg100 = LevelConfig.GetConfig(100);

            Assert.LessOrEqual(cfg1.MaxZ, cfg100.MaxZ,
                $"Level 1 maxZ={cfg1.MaxZ} should not exceed Level 100 maxZ={cfg100.MaxZ}");
        }

        // Test: Sigmoid midpoint at level 50 gives d ~= 0.5
        [Test]
        public void LevelConfig_Level50_SigmoidMidpointIsHalf()
        {
            float d = LevelConfig.GetDifficultyParameter(50);
            Assert.AreEqual(0.5f, d, delta: 0.05f,
                $"Level 50 difficulty parameter should be ~0.5 (sigmoid midpoint), got {d}");
        }

        // Test: Difficulty is monotonically non-decreasing across levels 1..100
        [Test]
        public void LevelConfig_AllLevels_DifficultyMonotonicallyIncreases()
        {
            float prevD = LevelConfig.GetDifficultyParameter(1);
            for (int level = 2; level <= 100; level++)
            {
                float d = LevelConfig.GetDifficultyParameter(level);
                Assert.GreaterOrEqual(d, prevD - 0.001f,
                    $"Difficulty decreased between level {level - 1} (d={prevD}) and level {level} (d={d})");
                prevD = d;
            }
        }

        // Test: tileCount stays in [36, 144] for all levels 1..100
        [Test]
        public void LevelConfig_AllLevels_TileCountInRange()
        {
            for (int level = 1; level <= 100; level++)
            {
                var cfg = LevelConfig.GetConfig(level);
                Assert.GreaterOrEqual(cfg.TileCount, 36,
                    $"Level {level} tileCount={cfg.TileCount} is below minimum (36)");
                Assert.LessOrEqual(cfg.TileCount, 144,
                    $"Level {level} tileCount={cfg.TileCount} exceeds maximum (144)");
                Assert.AreEqual(0, cfg.TileCount % 2,
                    $"Level {level} tileCount={cfg.TileCount} must be even");
            }
        }
    }
}
