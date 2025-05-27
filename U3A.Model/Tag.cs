using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U3A.Model
{
    public class Tag : BaseEntity
    {
        public Guid Id { get; set; }

        [Required]
        public string Name { get; set; }
        public bool CanLead { get; set; } = false;
        public bool CanEnrol { get; set; } = false;
        public IEnumerable<Contact> Contacts { get; set; } = new List<Contact>();
    }
}
