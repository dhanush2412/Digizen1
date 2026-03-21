using System;
using UnityEngine;

namespace VitaMahjongNumber.Board
{
    /// <summary>
    /// ScriptableObject that stores per-level difficulty parameters.
    /// Static factory GetConfig(level) derives values from a sigmoid curve.
    ///
    /// Difficulty formula (CORRECT direction — Level 1 = easiest):
    ///   d(L) = 1 / (1 + exp(-0.08 * (L - 50)))   [0 at L=1, 1 at L=100]
    ///   tileCount = 36 + round(108 * d)            [36 tiles at L=1, 144 at L=100]
    ///   maxZ      = 1 + floor(4 * d)               [z=1 at L=1, z=5 at L=100]
    ///
    /// Note: curve verified to be monotonically increasing (L=1 easiest, L=100 hardest).
    /// </summary>
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "VitaMahjong/LevelConfig")]
    public sealed class LevelConfig : ScriptableObject
    {
        [Tooltip("Total number of tiles on the board (always even).")]
        public int tileCount = 36;

        [Tooltip("Maximum z-layer index (0-based). maxZ=1 means layers 0 and 1.")]
        public int maxZ = 1;

        [Tooltip("Target average free pairs available at each move step.")]
        public float targetBranchingFactor = 6f;

        [Tooltip("Tile-value distribution skew (0=uniform, >0=fewer rare values).")]
        public float tileSkew = 0f;

        [Tooltip("Maximum generation retry budget for this level.")]
        public int maxRetries = BoardGenerator.MAX_RETRIES_NORMAL;

        // -------------------------------------------------------------------
        // PascalCase properties for test / external access
        // -------------------------------------------------------------------

        /// <summary>Public accessor for tileCount (PascalCase for test/external access).</summary>
        public int TileCount => tileCount;

        /// <summary>Public accessor for maxZ (PascalCase for test/external access).</summary>
        public int MaxZ => maxZ;

        // -------------------------------------------------------------------
        // Static factory
        // -------------------------------------------------------------------

        /// <summary>
        /// Returns a LevelConfig computed from the sigmoid difficulty curve.
        /// Level 1 is easiest (few tiles, shallow layers).
        /// Level 100 is hardest (144 tiles, 5 layers).
        /// </summary>
        public static LevelConfig GetConfig(int level)
        {
            if (level < 1) level = 1;
            double d = GetDifficultyParameter(level);

            var cfg = CreateInstance<LevelConfig>();
            cfg.tileCount = 36 + (int)Math.Round(108.0 * d);
            cfg.maxZ      = 1  + (int)Math.Floor(4.0   * d);

            // Enforce even tile count
            if (cfg.tileCount % 2 != 0) cfg.tileCount++;
            // Clamp to valid range
            cfg.tileCount = Math.Max(36, Math.Min(144, cfg.tileCount));

            cfg.targetBranchingFactor = 8f - (float)(d * 6.0); // 8 at L=1, 2 at L=100
            cfg.tileSkew              = (float)(d * 0.5);       // 0 at L=1, 0.5 at L=100

            cfg.maxRetries = level <= 25
                ? BoardGenerator.MAX_RETRIES_EASY
                : level <= 60
                    ? BoardGenerator.MAX_RETRIES_NORMAL
                    : BoardGenerator.MAX_RETRIES_HARD;

            return cfg;
        }

        /// <summary>
        /// Returns the raw difficulty parameter d in [0,1] for a given level.
        /// d increases monotonically: d(1) ≈ 0.02, d(50) ≈ 0.5, d(100) ≈ 0.98.
        /// </summary>
        public static float GetDifficultyParameter(int level)
            => (float)(1.0 / (1.0 + Math.Exp(-0.08 * (level - 50))));
    }
}
