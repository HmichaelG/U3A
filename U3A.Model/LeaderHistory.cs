using Microsoft.EntityFrameworkCore;

namespace U3A.Model
{
    [Index(nameof(PersonID), nameof(ClassID), nameof(TermID), IsUnique = true)]
    public class LeaderHistory
    {
        public Guid ID { get; set; }
        public Guid PersonID { get; set; }
        public Guid ClassID { get; set; }
        public Guid TermID { get; set; }
        public int Year { get; set; }
        public int Term { get; set; }
        public LeaderType Type { get; set; }
        public String Course { get; set; }
        public String Class { get; set; }

        public String TypeName
        {
            get
            {
                return Type switch
                {
                    LeaderType.Leader => "Leader",
                    LeaderType.Clerk => "Clerk",
                    _ => "Unknown"
                };
            }
        }
        public string TermName
        {
            get
            {
                return $"Term-{Term}";
            }
        }
        // Navigation properties
        public virtual Person Person { get; set; }
    }

   
    public enum LeaderType
    {
        Leader,
        Clerk
    }
}
