using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DepthCharts.Domain.Entities
{
    public class PositionMetadata
    {
        public string League { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Group { get; set; }
        public int SortOrder { get; set; }
    }
}
