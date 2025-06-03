using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace U3A.Model
{
    public class LuckyMemberDraw 
    {
        public Guid ID { get; set; }
        [Required]
        public string Name { get; set; } = "Lucky Member Draw";

        public bool IsComplete {get; set; }

        [Timestamp]
        public byte[] Version { get; set; }

        //Navigation properties
        public List<LuckyMemberDrawEntrant> LuckyMemberDrawEntrants { get; set; } = new List<LuckyMemberDrawEntrant>();

    }
}
