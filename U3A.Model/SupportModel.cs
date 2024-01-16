using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U3A.Model
{
    [NotMapped]
    public class SupportModel
    {
        [Required]
        public string Name { get; set; }
        public string Phone { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;

        [EmailAddress]
        public string? Email { get; set; }
        [Required]
        public string Description { get; set; }
    }
}
