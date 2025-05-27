using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing.Text;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json.Serialization;

namespace U3A.Model
{
    public class MultiCampusEnrolment : BaseEntity
    {
        public MultiCampusEnrolment() { Created = DateTime.UtcNow; }

        [Key]
        public Guid ID { get; set; }
        public string TenantIdentifier { get; set; }
        public Guid TermID { get; set; }
        public Guid CourseID { get; set; }
        public Guid? ClassID { get; set; }
        public Guid PersonID { get; set; }
        public DateTime Created { get; set; }
        public DateTime? DateEnrolled { get; set; }
        public bool IsCourseClerk { get { return false; } private set { } } // visitor cannot be clerk
        public bool IsWaitlisted { get; set; } = true;
    }
}
