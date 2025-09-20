using System.ComponentModel.DataAnnotations;

namespace DepthCharts.Domain.Entities
{
    /// <summary>
    /// Represents a player in the depth chart system
    /// </summary>
    public class Player
    {
        /// <summary>
        /// Player's jersey number (unique within team)
        /// </summary>
        [Range(0, 99)]
        public int Number { get; set; }

        /// <summary>
        /// Player's full name
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Team the player belongs to
        /// </summary>
        [Required]
        public string TeamId { get; set; } = string.Empty;

        /// <summary>
        /// Gets a unique identifier for this player (team + number)
        /// </summary>
        public string GetUniqueId() => $"{TeamId}_{Number}";
    }
}
