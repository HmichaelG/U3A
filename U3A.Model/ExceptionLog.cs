using Microsoft.AspNetCore.Http.HttpResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U3A.Model
{
    public class ExceptionLog
    {
        public Guid ID { get; set; }
        public DateTime Date {  get; set; } = DateTime.UtcNow;

        public DateTime? LocalDate
        {
            get
            {
                return TimezoneAdjustment.GetLocalTime(Date);
            }
        }

        public string Tenant { get; set; }
        public string Log { get; set; }
    }
}
