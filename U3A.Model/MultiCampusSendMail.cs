using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U3A.Model
{
    public class MultiCampusSendMail : BaseEntity
    {
        public Guid ID { get; set; }
        public string TenantIdentifier { get; set; }
        public string DocumentName { get; set; }
        public Guid PersonID { get; set; }
        public Guid RecordKey { get; set; }
        public Guid? TermID { get; set; }
        public string Status { get; set; } = String.Empty;

    }
}
