using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U3A.Model
{
    public class Schedule : BaseEntity
    {
        public Guid ID { get; set; }

        public bool IsOffScheduleActivity { get; set; }
        public int Year { get; set; }
        public int TermNumber { get; set; }
        public Guid CourseID { get; set; }
        public Guid ClassID { get; set; }
        public int OnDayID { get; set; }
        public int CourseNumber { get; set; }
        public string CourseName { get; set; }
        public string CourseDescription { get; set; }
        public string CourseType { get; set; }
        public int CourseMaximum {  get; set; }
        public int CourseMinimu {  get; set; }

        [Precision(precision: 18, 2)]
        public decimal CourseCost { get; set; }
        public string CourseCostDescription { get; set; }

        [Precision(precision: 18, 2)]
        public decimal CourseTermCost { get; set; }
        public string CourseCostTermDescription { get; set; }
        public string TermSummary { get; set; }
        public string ClassSummary { get; set; }
        public string VenueName { get; set; }
        public string VenueAddress { get;set; }

        public string GuestLeader {  get; set; }
        public List<string> LeaderName { get; set; } = new List<string>();
        public List<string> LeaderType { get; set; } = new List<string>();
        public List<string> LeaderEmail { get; set; } = new List<string>();
        public List<string> LeaderMobile { get; set; } = new List<string>();
        public List<string> LeaderPhone { get; set; } = new List<string>();

        private int _selectedContact = 0;
        public void GetNextCourseContact()
        {
            if (LeaderName.Count > 0)
            {
                _selectedContact++;
                if (_selectedContact >= LeaderName.Count) _selectedContact = 0;
            }
        }

    }
}
