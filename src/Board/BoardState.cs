using System.Collections.Generic;

namespace VitaMahjongNumber.Board
{
    /// <summary>
    /// Runtime board state snapshot. Used for no-moves detection and strategy routing.
    /// Wraps a TileData array and provides query methods via IModeStrategy.
    /// </summary>
    public sealed class BoardState
    {
        private readonly TileData[]   _tiles;
        private readonly HashSet<int> _occupied;

        private BoardState(TileData[] tiles)
        {
            _tiles    = tiles;
            _occupied = new HashSet<int>(tiles.Length);
            foreach (var t in tiles) _occupied.Add(t.Position);
        }

        /// <summary>Creates a BoardState from an array of TileData.</summary>
        public static BoardState FromTileData(TileData[] tiles)
            => new BoardState(tiles);

        /// <summary>Creates a BoardState from a BoardResult placement.</summary>
        public static BoardState FromBoardResult(BoardResult result)
        {
            var tiles = new TileData[result.OccupiedCount];
            int i = 0;
            foreach (var (pos, val) in result.AllTiles())
                tiles[i++] = new TileData(pos, val);
            return new BoardState(tiles);
        }

        /// <summary>
        /// Returns true if any two free tiles form a valid pair under the given strategy.
        /// Routes through IModeStrategy — never hardcodes Classic rules.
        /// </summary>
        public bool HasAnyValidPair(IModeStrategy strategy)
        {
            var freeTiles = new List<TileData>(_tiles.Length);
            foreach (var tile in _tiles)
            {
                TilePosition.DecodePos(tile.Position, out int x, out int y, out int z);
                if (TilePosition.IsFree(x, y, z, _occupied))
                    freeTiles.Add(tile);
            }

            for (int a = 0; a < freeTiles.Count; a++)
            for (int b = a + 1; b < freeTiles.Count; b++)
                if (strategy.IsValidMatch(freeTiles[a], freeTiles[b]))
                    return true;

            return false;
        }

        public IReadOnlyList<TileData> AllTiles => _tiles;
        public int TileCount => _tiles.Length;
    }
}
