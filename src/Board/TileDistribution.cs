using System;
using System.Collections.Generic;

namespace VitaMahjongNumber.Board
{
    /// <summary>Tile pool construction and balance validation for Classic and Math modes.</summary>
    public static class TileDistribution
    {
        /// <summary>
        /// Builds a Classic-mode tile pool: equal pairs of values 1–9.
        /// Total tiles = pairsPerValue * 9 * 2.
        /// </summary>
        public static List<int> BuildClassicPool(int pairsPerValue = 2)
        {
            var pool = new List<int>(pairsPerValue * 9 * 2);
            for (int v = 1; v <= 9; v++)
                for (int p = 0; p < pairsPerValue * 2; p++)
                    pool.Add(v);
            return pool;
        }

        /// <summary>
        /// Builds a Math-mode tile pool where every value v pairs with (10-v).
        /// Constraints: count(v) == count(10-v) for v=1..4; count(5) is always even.
        /// Total tiles = pairsPerValue * 9 * 2.
        /// </summary>
        public static List<int> BuildMathPool(int pairsPerValue = 2)
        {
            var pool = new List<int>(pairsPerValue * 9 * 2);

            // Add complementary pairs: (1,9),(2,8),(3,7),(4,6)
            for (int v = 1; v <= 4; v++)
                for (int p = 0; p < pairsPerValue; p++)
                {
                    pool.Add(v);
                    pool.Add(10 - v);
                }

            // Add 5s in even count (self-pairing)
            int fivesCount = pairsPerValue * 2;
            for (int p = 0; p < fivesCount; p++)
                pool.Add(5);

            return pool;
        }

        /// <summary>
        /// Validates pair-balance rules for the given mode.
        /// Classic: each value 1-9 must appear an even number of times.
        /// Math:    count(v) == count(10-v) for v=1..4; count(5) must be even.
        /// Throws InvalidOperationException if validation fails.
        /// </summary>
        public static void ValidatePairBalance(List<int> pool, MatchMode mode)
        {
            var counts = new int[10]; // index 1-9
            foreach (int v in pool)
            {
                if (v < 1 || v > 9)
                    throw new InvalidOperationException($"Pool contains out-of-range value {v}.");
                counts[v]++;
            }

            if (mode == MatchMode.Classic)
            {
                for (int v = 1; v <= 9; v++)
                    if (counts[v] % 2 != 0)
                        throw new InvalidOperationException(
                            $"Classic pool imbalance: value {v} has odd count {counts[v]}.");
            }
            else
            {
                for (int v = 1; v <= 4; v++)
                    if (counts[v] != counts[10 - v])
                        throw new InvalidOperationException(
                            $"Math pool imbalance: count({v})={counts[v]} != count({10-v})={counts[10-v]}.");
                if (counts[5] % 2 != 0)
                    throw new InvalidOperationException(
                        $"Math pool imbalance: count(5)={counts[5]} must be even.");
            }
        }

        /// <summary>Fisher-Yates in-place shuffle using the provided Random instance.</summary>
        public static void Shuffle(List<int> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }
    }
}
