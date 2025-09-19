using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DepthCharts.Domain.Entities
{
    public class DepthChartEntry
    {
        public int Id { get; set; }
        public string TeamId { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public int PlayerNumber { get; set; }
        public int PositionDepth { get; set; }
    }
}
