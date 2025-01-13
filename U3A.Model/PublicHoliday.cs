using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace U3A.Model
{
    public class PublicHoliday
    {
        public Guid ID { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public DateTime Date { get; set; }
    }
}
