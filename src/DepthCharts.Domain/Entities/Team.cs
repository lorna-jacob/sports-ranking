namespace DepthCharts.Domain.Entities
{
    /// <summary>
    /// Represents a sports team in the depth chart system
    /// </summary>
    public class Team
    {
        /// <summary>
        /// Team identifier (e.g., "TB", "NE", "KC")
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Full team name (e.g., "Tampa Bay Buccaneers")
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// League the team belongs to (e.g., "NFL")
        /// </summary>
        public string League { get; set; } = string.Empty;        
    }
}
