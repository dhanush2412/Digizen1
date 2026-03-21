namespace VitaMahjongNumber.Board
{
    /// <summary>
    /// Immutable value object representing a single tile on the board.
    /// Pure C# struct — no MonoBehaviour dependency, safe for Edit Mode tests.
    /// </summary>
    public readonly struct TileData
    {
        /// <summary>Encoded position. Use TilePosition.DecodePos to get x,y,z.</summary>
        public readonly int Position;

        /// <summary>Tile value 1-9.</summary>
        public readonly int Value;

        public TileData(int position, int value)
        {
            Position = position;
            Value    = value;
        }

        public override string ToString() => $"TileData(pos={Position}, val={Value})";
    }
}
