using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace U3A.Model
{
    public enum ReportStyle

    {
        [Display(Name = "Detail Report Style", ShortName = "Detail", Description = "Detail List")]
        Detail,
        [Display(Name = "Summary Report Style", ShortName = "Summary", Description = "Summary List")]
        Summary,
        [Display(Name = "Brief Report Style", ShortName = "Brief", Description = "Brief List")]
        Brief,
    }

    [NotMapped]
    public class ReportStyleWrapper
    {
        public ReportStyle Type { get; set; }
        public string DisplayText { get; set; }
        public string ShortText { get; set; }
        public string ListText { get; set; }
    }

    [NotMapped]
    public class ReportStyleList : List<ReportStyleWrapper>
    {
        public ReportStyleList()
        {
            AddRange(Enum.GetValues(typeof(ReportStyle))
                            .OfType<ReportStyle>()
                            .Select(t => new ReportStyleWrapper()
                            {
                                Type = t,
                                DisplayText = t.GetAttribute<DisplayAttribute>().Name,
                                ShortText = t.GetAttribute<DisplayAttribute>().ShortName,
                                ListText = t.GetAttribute<DisplayAttribute>().Description,
                            }).ToList());
        }

    }

}
