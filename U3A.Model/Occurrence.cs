using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U3A.Model
{
    public class Occurrence
    {
        public override int GetHashCode()
        {
            int hash = 17; // Initial value (usually a prime number)
            return hash * ID.GetHashCode();
        }
        public override bool Equals(object? obj)
        {
            if (obj == null || !(obj is Occurrence))
                return false;
            else
                return this.GetHashCode() == ((Occurrence)obj).GetHashCode();
        }

        [Key]
        public int ID { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }

        List<Class> Courses { get; set; } = new List<Class>();
    }

    public enum OccurrenceType
    {
        OnceOnly,
        Daily,
        Weekly,
        Fortnightly,
        FirstWeekOfMonth,
        SecondWeekOfMonth,
        ThirdWeekOfMonth,
        FourthWeekOfMonth,
        LastWeekOfMonth,
        Every5Weeks,
        Every6Weeks,
        FirstAndThirdWeekOfMonth,
        SecondAndFourthWeekOfMonth,
        Unscheduled = 999
    }

}
