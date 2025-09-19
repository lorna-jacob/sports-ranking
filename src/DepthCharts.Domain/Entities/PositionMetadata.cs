namespace DepthCharts.Domain.Entities
{
    /// <summary>
    /// Metadata about positions for a specific league
    /// </summary>
    public class PositionMetadata
    {
        /// <summary>
        /// League this position belongs to (e.g., "NFL")
        /// </summary>
        public string League { get; set; } = string.Empty;

        /// <summary>
        /// Position code (e.g., "QB", "RB", "LWR")
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Full position name (e.g., "Quarterback", "Left Wide Receiver")
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Position group (e.g., "Offense", "Defense", "Special Teams")
        /// </summary>
        public string Group { get; set; } = string.Empty;

        /// <summary>
        /// Sort order for display purposes
        /// </summary>
        public int SortOrder { get; set; }
    }
}
