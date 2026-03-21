using System.Collections.Generic;

namespace VitaMahjongNumber.Board
{
    /// <summary>
    /// Convenience wrapper exposing IsFree under the FreeTileChecker name.
    /// Tests reference FreeTileChecker.IsFree — this delegates to TilePosition.IsFree.
    /// </summary>
    public static class FreeTileChecker
    {
        /// <summary>
        /// Returns true if the tile at (x,y,z) is selectable (free to remove).
        /// Rules: not covered above AND (left side open OR right side open).
        /// CRITICAL formula: !blockedLeft || !blockedRight — NOT blockedLeft || blockedRight.
        /// </summary>
        public static bool IsFree(int x, int y, int z, HashSet<int> occupied)
            => TilePosition.IsFree(x, y, z, occupied);
    }
}
