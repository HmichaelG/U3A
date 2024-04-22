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
    public enum MemberFeePaymentType
    {
        [Display(Name = "Per year only", ShortName = "Per Year")]
        PerYearOnly,
        [Display(Name = "Per Year & per semester", ShortName = "Year / Semester")]
        PerYearAndPerSemester
    }

    [NotMapped]
    public class MemberFeePaymentTypeWrapper
    {
        public MemberFeePaymentType Type { get; set; }
        public string DisplayText { get; set; }
        public string ShortText { get; set; }
    }

    [NotMapped]
    public class MemberFeePaymentTypeList : List<MemberFeePaymentTypeWrapper>
    {

        public MemberFeePaymentTypeList()
        {
            AddRange(Enum.GetValues(typeof(MemberFeePaymentType))
                            .OfType<MemberFeePaymentType>()
                            .Select(t => new MemberFeePaymentTypeWrapper()
                            {
                                Type = t,
                                DisplayText = t.GetAttribute<DisplayAttribute>().Name,
                                ShortText = t.GetAttribute<DisplayAttribute>().ShortName
                            }).ToList());
        }

    }
}
