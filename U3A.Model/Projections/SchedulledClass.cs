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

        public string Name { get; set; }
        public bool Featured { get; set; }
        public string Description { get; set; }
        public string CourseParticipationType { get; set; }
        public bool EnforceOneClassPerStudent { get; set; }
        public decimal FeePerYear { get; set; }
        public string? FeePerYearDescription { get; set; }
        public decimal FeePerTerm { get; set; }
        public string? FeePerTermDescription { get; set; }
        public decimal Duration { get; set; }
        public int RequiredStudents { get; set; }
        public int MaximumStudents { get; set; }
        public bool AllowAutoEnroll { get; set; } = true;
        public string Type { get; set; }
        public string? ProvidedBy { get; set; } // The U3A that owns the course

        // From Class

        public Boolean OfferedTerm1 { get; set; }
        public Boolean OfferedTerm2 { get; set; }
        public Boolean OfferedTerm3 { get; set; }
        public Boolean OfferedTerm4 { get; set; }
        public DateOnly? StartDate { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public string Occurs { get; set; }
        public int? Repeats { get; set; }
        public string Day { get; set; }
        public string ClassSummary { get; set; }
        public string Venue { get; set; }
        public string VenueAddress { get; set; }
        public int TotalActiveStudents { get; set; }
        public int TotalWaitlisted { get; set; }
        public double ParticipationRate { get; set; }
        //public List<DateTime> ClassDates { get; set; } = new();
        public List<ScheduledPerson> People { get; set; } = new();
    }

    public class ScheduledPerson
    {
        public string Class { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Mobile { get; set; }
        public List<string> Roles { get; set; } = new();
        public string SortOrder { get; set; }
    }
    public class AIChatClassData
    {
        public List<ScheduledClass> Classes { get; set; } = new();
        public List<Term> Terms { get; set; } = new();
    }

}
