namespace VitaMahjongNumber.Board
{
    /// <summary>
    /// Strategy interface for mode-specific matching logic.
    /// All match decisions MUST route through this interface — never hardcode Classic rules.
    /// </summary>
    public interface IModeStrategy
    {
        /// <summary>Returns true if tileA and tileB are a valid match in this mode.</summary>
        bool IsValidMatch(TileData tileA, TileData tileB);

        /// <summary>Human-readable label shown in the HUD.</summary>
        string HUDLabel { get; }
    }
}
