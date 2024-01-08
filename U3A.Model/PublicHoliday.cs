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
        public string Name { get; set; }
        public DateTime Date { get; set; }
    }
}
