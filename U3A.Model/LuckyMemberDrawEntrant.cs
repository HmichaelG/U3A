using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U3A.Model
{
    public class LuckyMemberDrawEntrant
    {
        public Guid ID { get; set; }
        public Guid LuckyMemberDrawID { get; set; }
        public Guid PersonID { get; set; }

        [Timestamp]
        public byte[] Version { get; set; }

        // Navigation properties
        public LuckyMemberDraw LuckyMemberDraw { get; set; }

        public Person Person { get; set; }
    }
}
