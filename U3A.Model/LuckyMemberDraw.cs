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

        public bool IsComplete {get; set; }
        
        [NotMapped]
        public string Secret { 
            get {
                string result = string.Empty;
                if (CreatedOn.HasValue)
                {
                    var value = CreatedOn.Value.Millisecond * 1000 + CreatedOn.Value.Nanosecond;
                    result = $"{value.ToString("0000000")}";
                }
                return result;
            } 
        }

        //Navigation properties
        public List<LuckyMemberDrawEntrant> LuckyMemberDrawEntrants { get; set; } = new List<LuckyMemberDrawEntrant>();

    }
}
