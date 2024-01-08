using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace U3A.Model
{
    public static class DateCalculations
    {
        public static DateTime GetWeekEndingDate(DateTime date)
        {
            var weekdayDate = date.Date;
            while (weekdayDate.DayOfWeek != DayOfWeek.Sunday) weekdayDate = weekdayDate.AddDays(1);
            return weekdayDate.AddHours(23).AddMinutes(59).AddSeconds(59);
        }
    }
}
