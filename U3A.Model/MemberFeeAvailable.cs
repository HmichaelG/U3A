using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U3A.Model
{
    public class MemberPaymentAvailable
    {
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public int? TermsPaid { get; set; }

    }

}
