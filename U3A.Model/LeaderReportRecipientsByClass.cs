using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U3A.Model
{

    [NotMapped]
    public class LeaderReportRecipientsByClass
    {
        public Class Class { get; set; }
        public CourseContactType ContactType { get; set; }
        public int SortOrder { get; set; }
        public Person Person { get; set; }
    }
}
