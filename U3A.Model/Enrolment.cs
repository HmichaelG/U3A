using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection.Metadata.Ecma335;

namespace U3A.Model
{
    public class Enrolment
    {
        public Enrolment() { Created = DateTime.Now; }

        [Key]
        public Guid ID { get; set; }

        public Guid TermID { get; set; }
        public Term Term { get; set; }

        public Guid CourseID { get; set; }
        public Course Course { get; set; }

        public Guid? ClassID { get; set; }
        public Class? Class { get; set; }

        public Guid PersonID { get; set; }
        [Required]
        public Person Person { get; set; }

        [Required]
        public DateTime Created { get; set; }

        public long Random { get { return Created.Millisecond * 1000 + Created.Microsecond; } }

        public DateTime? DateEnrolled { get; set; }

        bool mIsCourseClerk;
        [Required]
        [DefaultValue(false)]
        public bool IsCourseClerk { get; set; } = false;

        [Required]
        public bool IsWaitlisted { get; set; }  = true;

        [NotMapped]
        public DateTime? WaitlistSort
        {
            get { return (IsWaitlisted) ? Created : null; }
        }

    }
}
