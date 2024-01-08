using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using U3A;
using U3A.Model;

namespace U3A.Model
{
    public enum SendLeaderReportsTo
    {
        [Display(Name = "Leaders, if none then clerks", ShortName = "Leader")]
        LeadersThenClerks,
        [Display(Name = "Clerks, if none then leaders", ShortName = "Clerk")]
        ClerksThenLeaders,
        [Display(Name = "Both leaders & clerks", ShortName = "Leader & Clerk")]
        Both = 10
    }

    [NotMapped]
    public class SendLeaderReportsToWrapper
    {
        public SendLeaderReportsTo Type { get; set; }
        public string DisplayText { get; set; }
        public string ShortText { get; set; }
    }

    [NotMapped]
    public class SendLeadersReportsToList : List<SendLeaderReportsToWrapper>
    {

        public SendLeadersReportsToList()
        {
            AddRange(Enum.GetValues(typeof(SendLeaderReportsTo))
                            .OfType<SendLeaderReportsTo>()
                            .Select(t => new SendLeaderReportsToWrapper()
                            {
                                Type = t,
                                DisplayText = t.GetAttribute<DisplayAttribute>().Name,
                                ShortText = t.GetAttribute<DisplayAttribute>().ShortName
                            }).ToList());
        }

    }
}
