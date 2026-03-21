namespace VitaMahjongNumber.Board
{
    /// <summary>Classic Mode: two tiles match when they share the same value (1-9).</summary>
    public sealed class ClassicModeStrategy : IModeStrategy
    {
        public string HUDLabel => "Classic";

        public bool IsValidMatch(TileData tileA, TileData tileB)
            => tileA.Value == tileB.Value;
    }
}
