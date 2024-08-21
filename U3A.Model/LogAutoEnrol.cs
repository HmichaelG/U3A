using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U3A.Model
{
    public class LogAutoEnrol
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public string MessageTemplate { get; set; }
        public string Level { get; set; }
        public DateTime TimeStamp { get; set; }
        public string? Exception { get; set; }
        public string? Properties { get; set; }
        public string? LogEvent { get; set; }
        public string? Tenant { get; set; }
        public string? User { get; set; }
    }
}
