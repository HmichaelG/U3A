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
    public enum CourseContactOrder
    {
        [Display(Name = "Leaders then clerks", ShortName = "Leader, Clerk")]
        LeadersThenClerks,
        [Display(Name = "Clerks then leaders", ShortName = "Clerk, Leader")]
        ClerksThenLeaders
    }

    [NotMapped]
    public class CourseContactOrderWrapper
    {
        public CourseContactOrder Type { get; set; }
        public string DisplayText { get; set; }
        public string ShortText { get; set; }
    }

    [NotMapped]
    public class CourseContactOrderList : List<CourseContactOrderWrapper>
    {

        public CourseContactOrderList()
        {
            AddRange(Enum.GetValues(typeof(CourseContactOrder))
                            .OfType<CourseContactOrder>()
                            .Select(t => new CourseContactOrderWrapper()
                            {
                                Type = t,
                                DisplayText = t.GetAttribute<DisplayAttribute>().Name,
                                ShortText = t.GetAttribute<DisplayAttribute>().ShortName
                            }).ToList());
        }

    }
}
