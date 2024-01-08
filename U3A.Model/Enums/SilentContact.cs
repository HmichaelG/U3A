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
    public enum SilentContact
    {
        [Display(Name = "None, both email & phone are visible", ShortName = "No")]
        None,
        [Display(Name = "Email is not visible", ShortName = "Email")]
        EmailOnly,
        [Display(Name = "Phone is not visible", ShortName = "Phone")]
        PhoneOnly,
        [Display(Name = "Both email & phone are not visible", ShortName = "Both")]
        Both
    }

    [NotMapped]
    public class SilentContactWrapper
    {
        public SilentContact Type { get; set; }
        public string DisplayText { get; set; }
        public string ShortText { get; set; }
    }

    [NotMapped]
    public class SilentContactList : List<SilentContactWrapper>
    {

        public SilentContactList()
        {
            AddRange(Enum.GetValues(typeof(SilentContact))
                            .OfType<SilentContact>()
                            .Select(t => new SilentContactWrapper()
                            {
                                Type = t,
                                DisplayText = t.GetAttribute<DisplayAttribute>().Name,
                                ShortText = t.GetAttribute<DisplayAttribute>().ShortName
                            }).ToList());
        }

    }

}
