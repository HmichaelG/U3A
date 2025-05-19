using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Threading.Tasks;

namespace U3A.Model
{
    [NotMapped]
    public class StudentClassSummary
    {
        public string Term { get; set; }
        public string Course { get; set; }
        public string Class { get; set; }
        public bool IsWaitlisted { get; set; }
    }
}
