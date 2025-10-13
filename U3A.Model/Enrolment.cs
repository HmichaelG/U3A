using Microsoft.AspNetCore.Http.HttpResults;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json.Serialization;

namespace U3A.Model
{
    // *** Defined in fluent API ***
    //CREATE UNIQUE NONCLUSTERED INDEX idxUniqueEnrolments
    //ON enrolment([TermID]
    //  , [CourseID]
    //  , [ClassID]
    //  , [PersonID])
    //WHERE isDeleted = 0
    public class Enrolment : BaseEntity, ISoftDelete
    {
        public Enrolment() { Created = DateTime.UtcNow; }

        [Key]
        public Guid ID { get; set; }

        public Guid TermID { get; set; }
        public Term Term { get; set; }

        public Guid CourseID { get; set; }
        [JsonIgnore] public Course Course { get; set; }

        public Guid? ClassID { get; set; }
        [JsonIgnore] public Class? Class { get; set; }

        [Required] public Guid PersonID { get; set; }

        [Required, JsonIgnore]
        public Person Person { get; set; }

        [Required]
        public DateTime Created { get; set; }

        public long Random { get { return Created.Millisecond * 1000 + Created.Microsecond; } }

        public DateTime? DateEnrolled { get; set; }

        [Required]
        [DefaultValue(false)]
        public bool IsCourseClerk { get; set; } = false;

        [Required]
        public bool IsWaitlisted { get; set; } = true;

        [NotMapped]
        public DateTime? WaitlistSort
        {
            get { return (IsWaitlisted) ? Created : null; }
        }

        [NotMapped]
        public string EnrolmentStatusText
        {
            get { return (IsWaitlisted) ? "Waitlisted" : DateEnrolled.Value.ToString("dd-MMM-yyyy"); }
        }

        [NotMapped]
        public bool isLeader { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
