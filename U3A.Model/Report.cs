using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U3A.Model
{
    public class Report : BaseEntity
    {
        public Guid ID { get; set; }
        public string Name { get; set; }
        public byte[] Definition { get; set; }
    }
}
