using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U3A.Model
{
    public class CourseParticipationType
    {
        [Key]
        public int ID { get; set; }
        public string Name { get; set; }

        [NotMapped]
        public string ShortName
        {
            get
            {
                string result;
                if (ID == 0) { result = "Course"; } else { result = "Class"; }
                return result;
            }
        }
        List<Course> Courses { get; set; } = new List<Course>();

        public override int GetHashCode()
        {
            int hash = 17; // Initial value (usually a prime number)
            return hash * ID.GetHashCode();
        }
        public override bool Equals(object? obj)
        {
            if (obj == null || !(obj is CourseParticipationType))
                return false;
            else
                return this.GetHashCode() == ((CourseParticipationType)obj).GetHashCode();
        }

    }

    public enum ParticipationType
    {
        SameParticipantsInAllClasses,
        DifferentParticipantsInEachClass
    }

}
