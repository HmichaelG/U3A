using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace U3A.Model
{
    public class LuckyMemberDraw : BaseEntity
    {
        public Guid ID { get; set; }
        [Required]
        public string Name { get; set; } = "Lucky Member Draw";

        public bool IsComplete { get; set; }

        [NotMapped]
        public string Secret
        {
            get
            {
                string result = string.Empty;
                if (CreatedOn.HasValue)
                {
                    var value = CreatedOn.Value.Ticks;
                    // get right most 6 digits
                    value = value % 1000000;
                    result = $"{value.ToString("0")}";
                }
                return result;
            }
        }

        //Navigation properties
        public List<LuckyMemberDrawEntrant> LuckyMemberDrawEntrants { get; set; } = new List<LuckyMemberDrawEntrant>();

    }
}
