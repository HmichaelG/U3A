using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace U3A.Model
{

    public class ScheduledClass
    {
        public Guid ID { get; set; }

        // From Course

        public string CourseName { get; set; }
        public bool IsFeaturedCourse { get; set; }
        public string CourseParticipationType { get; set; }
        public bool EnforceOneClassPerStudent { get; set; }
        public decimal CourseFeePerYear { get; set; }
        public string? CourseFeePerYearDescription { get; set; }
        public decimal CourseFeePerTerm { get; set; }
        public string? CourseFeePerTermDescription { get; set; }
        public decimal Duration { get; set; }
        public int RequiredStudents { get; set; }
        public int MaximumStudents { get; set; }
        public bool AllowAutoEnroll { get; set; } = true;
        public string CourseType { get; set; }
        public string? OfferedBy { get; set; } // The U3A that owns the course

        // From Class

        public Boolean OfferedTerm1 { get; set; } 
        public Boolean OfferedTerm2 { get; set; } 
        public Boolean OfferedTerm3 { get; set; } 
        public Boolean OfferedTerm4 { get; set; } 
        public DateOnly? StartDate { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public string Occurrence { get; set; }
        public int? Recurrence { get; set; }
        public string OnDay { get; set; }
        public string OccurrenceTextBrief { get; set; }
        public string OccurrenceText { get; set; }
        public string Venue { get; set; }
        public string VenueAddress { get; set; }
        public List<ScheduledPerson> Leaders { get; set; } = new ();
        public List<ScheduledPerson> Clerks { get; set; } = new ();
        public int TotalActiveStudents { get; set; }
        public int TotalWaitlistedStudents { get; set; }
        public double ParticipationRate { get; set; }
        public List<DateTime> ClassDates { get; set; } = new();

    }

    public class ScheduledPerson
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Mobile { get; set; }
        public bool IsGuestLeader { get; set; } = false;
    }
    public class AIChatClassData
    {
        public List<ScheduledClass> Classes { get; set; }
        public List<Term> Terms { get; set; }
    }

}
