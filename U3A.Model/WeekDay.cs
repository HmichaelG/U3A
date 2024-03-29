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
    }
}
