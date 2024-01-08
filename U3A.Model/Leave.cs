using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace U3A.Model
{
    public class Leave : BaseEntity
    {

        [Key]
        public Guid ID { get; set; }


        public Guid PersonID { get; set; }
        [Required] public Person Person { get; set; }

        public Guid? CourseID { get; set; }
        public Course? Course { get; set; }
        [Required] public DateTime StartDate { get; set; }
        [Required] public DateTime EndDate { get; set; }
        [Required] public string Reason { get; set; }

        [NotMapped]
        public string ToString
        {
            get
            {
                var result = $"{StartDate: dd-MMM} - {EndDate: dd-MMM-yy} ";
                if (Course != null)
                    result = $"{result} {Course.Name}: {Reason}";
                else result = $"{result} All Classes: {Reason}";
                return result;
            }
        }

    }
}
