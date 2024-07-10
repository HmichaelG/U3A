using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U3A.Model
{
    public class BaseEntity
    {
        public DateTime? CreatedOn { get; set; }
        public DateTime? LocalCreatedOn
        {
            get
            {
                return (CreatedOn != null) ? TimezoneAdjustment.GetLocalTime(CreatedOn.Value) : null;
            }
        }
        public DateTime? UpdatedOn { get; set; }

        public string? User { get; set; }

    }
}
