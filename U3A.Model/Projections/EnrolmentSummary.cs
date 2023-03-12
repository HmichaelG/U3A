using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U3A.Model
{
    public class EnrolmentSummary
    {
        public int Year { get; set; }
        public int Term { get; set; }
        public string TermName { get {  
                return $"{Year} Term-{Term}"; } 
        }
        public string CourseType { get; set; }
        public int Count { get; set; }
    }
}
