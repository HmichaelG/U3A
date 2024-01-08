using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U3A.Model
{
    public class MemberSummary
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string Group { get; set; }
        public string MonthName
        {
            get
            {
                return new DateTime(2000, Month, 1).ToString("MMMM");
            }
        }
        public int Count { get; set; }
    }
}
