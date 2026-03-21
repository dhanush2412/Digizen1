namespace VitaMahjongNumber.Board
{
    /// <summary>Math Mode: two tiles match when their values sum to 10.</summary>
    public sealed class MathModeStrategy : IModeStrategy
    {
        public string HUDLabel => "Math Mode";

        public bool IsValidMatch(TileData tileA, TileData tileB)
            => tileA.Value + tileB.Value == 10;
    }
}
