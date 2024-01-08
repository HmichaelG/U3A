using System.ComponentModel.DataAnnotations.Schema;

namespace U3A.Model
{
    [NotMapped]
    public class ClassDate
    {
        public DateTime TermStart { get; set; }
        public DateTime Date { get; set; }
        public string DateName
        {
            get
            {
                return Date.ToString("ddd, dd MMM yyyy hh:mm tt");
            }
        }
        public string WeekDateName
        {
            get
            {
                var result = DateName;
                if (TermStart != null)
                {
                    var wk = ((Date - TermStart).Days / 7) + 1;
                    result = $"Wk {wk}: {DateName}";
                }
                return result;
            }
        }
    }
}
