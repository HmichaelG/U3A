using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U3A.Model
{
    [NotMapped]
    public class MemberFee
    {
        public Guid ID { get; set; } = Guid.NewGuid();
        public Guid PersonID { get; set; }
        public MemberFeeSortOrder SortOrder { get; set; }
        public DateTime? Date { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public decimal? Allocated { get; set; } = null;
        public decimal? Balance { get; set; } = null;
        public bool IsNotAllocated { get; set; } = false;
    }

    public enum MemberFeeSortOrder
    {
        MemberFee,
        Complimentary,
        MailSurcharge,
        CourseFee,
        TermFee,
        AdditionalFee,
        Refund,
        Receipt
    }

}
