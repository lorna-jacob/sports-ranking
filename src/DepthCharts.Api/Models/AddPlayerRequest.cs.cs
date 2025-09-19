using DepthCharts.Domain.Entities;

namespace DepthCharts.Api.Models
{
    public class AddPlayerRequest
    {
        public string Position { get; set; } = string.Empty;
        public Player Player { get; set; } = new();
        public int? PositionDepth { get; set; }
    }
}
