using System.Collections.Generic;

namespace VitaMahjongNumber.Board
{
    /// <summary>
    /// Encodes a 3-D tile position into a single int for O(1) hashing and lookup.
    /// Bit layout: bits 0-4 = x (max 31), bits 5-9 = y (max 31), bits 10-12 = z (max 7).
    /// Max supported grid: 32 x 32 x 8.
    /// </summary>
    public static class TilePosition
    {
        private const int X_BITS  = 5;
        private const int Y_BITS  = 5;
        private const int X_MASK  = (1 << X_BITS) - 1;
        private const int Y_MASK  = (1 << Y_BITS) - 1;
        private const int Z_MASK  = 0x07;
        private const int Y_SHIFT = X_BITS;
        private const int Z_SHIFT = X_BITS + Y_BITS;

        /// <summary>Encodes (x, y, z) into a single compact int key.</summary>
        public static int EncodePos(int x, int y, int z)
            => (x & X_MASK) | ((y & Y_MASK) << Y_SHIFT) | ((z & Z_MASK) << Z_SHIFT);

        /// <summary>Decodes a packed int back into (x, y, z) coordinates.</summary>
        public static void DecodePos(int pos, out int x, out int y, out int z)
        {
            x = pos & X_MASK;
            y = (pos >> Y_SHIFT) & Y_MASK;
            z = (pos >> Z_SHIFT) & Z_MASK;
        }

        /// <summary>
        /// Returns true if the tile at (x,y,z) is selectable.
        /// Rules: not covered above AND (left side open OR right side open).
        /// CRITICAL: condition is !blockedLeft || !blockedRight — NOT blockedLeft || blockedRight.
        /// </summary>
        public static bool IsFree(int x, int y, int z, HashSet<int> occupied)
        {
            // Condition 1: nothing directly above
            if (occupied.Contains(EncodePos(x, y, z + 1))) return false;

            // Condition 2: at least one horizontal side is open
            bool blockedLeft  = occupied.Contains(EncodePos(x - 1, y, z));
            bool blockedRight = occupied.Contains(EncodePos(x + 1, y, z));
            return !blockedLeft || !blockedRight;
        }

        /// <summary>
        /// Populates results with positions whose free-status may change when
        /// a tile is placed/removed at (x,y,z). Used for incremental free-set updates.
        /// After placing at (x,y,z): re-evaluate ALL returned positions via IsFree.
        /// Do NOT blindly add/remove — always re-evaluate.
        /// </summary>
        public static void GetAffectedNeighbors(int x, int y, int z, List<int> results)
        {
            results.Clear();
            results.Add(EncodePos(x - 1, y, z));
            results.Add(EncodePos(x + 1, y, z));
            if (z > 0)
            {
                results.Add(EncodePos(x,     y, z - 1));
                results.Add(EncodePos(x - 1, y, z - 1));
                results.Add(EncodePos(x + 1, y, z - 1));
            }
        }
    }
}
