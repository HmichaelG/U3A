using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U3A.Model
{
    [NotMapped]
    public class StudentDetails
    {
        public Guid ID { get; set; }
        public bool IsVisitor { get; set; }
        public Person Person { get; set; }
        public Enrolment Enrolment { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public string CarerSummary => Person.CarerSummaryHtml;
        public string Status
        {
            get
            {
                if (Enrolment.IsWaitlisted) { return "Waitlisted"; }
                if (Enrolment.isLeader) { return "Leader"; }
                if (Enrolment.IsCourseClerk) { return "Clerk"; }
                return "Enrolled";
            }
        }

    }

}
