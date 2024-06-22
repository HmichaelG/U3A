using DevExpress.Data.Linq.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace U3A.Model
{
    public class Venue
    {
        public override int GetHashCode()
        {
            int hash = 17; // Initial value (usually a prime number)
            return hash * ID.GetHashCode();
        }
        public override bool Equals(object? obj)
        {
            if (obj == null || !(obj is Venue))
                return false;
            else
                return this.GetHashCode() == ((Venue)obj).GetHashCode();
        }
        [Key]
        public Guid ID { get; set; }

        [Required]
        public Boolean Discontinued { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        [DefaultValue(0)]
        public int MaxNumber { get; set; }

        public string Address { get; set; } = string.Empty;

        [Required]
        [DefaultValue(true)]
        public bool CanMapAddress { get; set; } = true;

        public string? Equipment { get; set; }
        public string? Coordinator { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? AccessDetail { get; set; }
        public string? KeyCode { get; set; }

        [JsonIgnore]
        public List<Class> Classes { get; set; } = new List<Class>();

        public string Comment { get; set; } = string.Empty;
    }
}
