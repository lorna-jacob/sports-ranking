namespace DepthCharts.Domain.Entities
{
    /// <summary>
    /// Represents a player's position assignment in a team's depth chart
    /// </summary>
    public class DepthChartEntry
    {
        /// <summary>
        /// Unique identifier for this depth chart entry
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Team identifier this entry belongs to
        /// </summary>
        public string TeamId { get; set; } = string.Empty;

        /// <summary>
        /// Position code (e.g., "QB", "RB", "LWR")
        /// </summary>
        public string Position { get; set; } = string.Empty;

        /// <summary>
        /// Player's jersey number
        /// </summary>
        public int PlayerNumber { get; set; }

        /// <summary>
        /// Depth ranking at this position (0 = starter, 1 = backup, etc.)
        /// </summary>
        public int PositionDepth { get; set; }        
    }
}
