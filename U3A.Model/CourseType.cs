using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace U3A.Model
{
    [Index(nameof(Name), IsUnique = true)]
    public class CourseType
    {
        [Key]
        public Guid ID { get; set; }

        [Required]
        public Boolean Discontinued { get; set; }

        [MaxLength(50)]
        [Required]
        public string Name { get; set; }

        [Required(AllowEmptyStrings = true)]
        public string Comment { get; set; } = string.Empty;

        public string NameWithStatus
        {
            get
            {
                if (!Discontinued) return Name; else return $"{Name} (Discontinued)";
            }
        }

        [JsonIgnore]
        public List<Course> Courses { get; set; } = new List<Course>();

        public override int GetHashCode()
        {
            int hash = 17; // Initial value (usually a prime number)
            return hash * ID.GetHashCode();
        }
        public override bool Equals(object? obj)
        {
            if (obj == null || !(obj is CourseType))
                return false;
            else
                return this.GetHashCode() == ((CourseType)obj).GetHashCode();
        }
    }
}