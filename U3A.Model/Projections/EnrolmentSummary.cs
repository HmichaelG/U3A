using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U3A.Model
{
    public class EnrolmentSummary
    {
        public DateTime Period { get; set; }
        public string CourseType { get; set; }
        public bool IsDropout { get; set; }
        public int Count { get; set; }
    }
}
