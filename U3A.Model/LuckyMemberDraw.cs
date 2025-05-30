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
        [Required]
        public List<Guid> PersonID { get; set; } = new();

        public bool IsComplete {get; set; }  
    }
}
