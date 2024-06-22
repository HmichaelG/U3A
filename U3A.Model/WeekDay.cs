using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace U3A.Model
{
    public class WeekDay
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ID { get; set; }
        public string Day { get; set; }
        public string ShortDay { get; set; }
        [JsonIgnore] public List<Class> Classes { get; set; } = new List<Class>();

        public override int GetHashCode()
        {
            int hash = 17; // Initial value (usually a prime number)
            return hash * ID.GetHashCode();
        }
        public override bool Equals(object? obj)
        {
            if (obj == null || !(obj is WeekDay))
                return false;
            else
                return this.GetHashCode() == ((WeekDay)obj).GetHashCode();
        }

    }
}
