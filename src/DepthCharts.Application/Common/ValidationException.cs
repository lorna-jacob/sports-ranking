using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DepthCharts.Application.Common
{
    public sealed class ValidationException : Exception
    {
        public IReadOnlyList<string> Errors { get; }
        public ValidationException(string message): base(message)
        {
            Errors = new List<string>() { message };
        }
        public ValidationException(IEnumerable<string> errors) : base("Validation failed")
        {
            Errors =errors.ToList();
        }
    }
}
