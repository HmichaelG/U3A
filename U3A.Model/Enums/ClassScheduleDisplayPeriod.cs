using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace U3A.Model
{
    public enum ClassScheduleDisplayPeriod
    {
        [Display(Name = "For the full year", ShortName = "Full year")]
        FullYear,
        [Display(Name = "For the current semester only", ShortName = "Current Semester")]
        CurrentSemester,
        [Display(Name = "For the current term only", ShortName = "Current Term")]
        CurrentTerm
    }

    [NotMapped]
    public class ClassScheduleDisplayPeriodWrapper
    {
        public ClassScheduleDisplayPeriod Type { get; set; }
        public string DisplayText { get; set; }
        public string ShortText { get; set; }
    }

    [NotMapped]
    public class ClassScheduleDisplayPeriodList : List<ClassScheduleDisplayPeriodWrapper>
    {

        public ClassScheduleDisplayPeriodList()
        {
            AddRange(Enum.GetValues(typeof(ClassScheduleDisplayPeriod))
                            .OfType<ClassScheduleDisplayPeriod>()
                            .Select(t => new ClassScheduleDisplayPeriodWrapper()
                            {
                                Type = t,
                                DisplayText = t.GetAttribute<DisplayAttribute>().Name,
                                ShortText = t.GetAttribute<DisplayAttribute>().ShortName
                            }).ToList());
        }

    }
}
