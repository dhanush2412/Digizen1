namespace VitaMahjongNumber.Board
{
    /// <summary>
    /// Game mode that determines how tile pairs are matched.
    /// Used as the primary mode selector in tests and public API.
    /// </summary>
    public enum GameMode
    {
        Classic,   // Two tiles match if they have the same value (1-9)
        MathMode,  // Two tiles match if their values sum to 10
        ActiveMind // Observation + recall phase (same matching rules as Classic)
    }

    /// <summary>Extension helpers for GameMode.</summary>
    public static class GameModeExtensions
    {
        /// <summary>Converts a GameMode to the internal MatchMode used by BoardGenerator.</summary>
        public static MatchMode ToMatchMode(this GameMode mode)
            => mode == GameMode.MathMode ? MatchMode.Math : MatchMode.Classic;
    }
}
