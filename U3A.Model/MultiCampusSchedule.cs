using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U3A.Model
{
    [Index(nameof(TenantIdentifier),nameof(TermId),nameof(ClassID),IsUnique = true)]
    public class MultiCampusSchedule : BaseEntity
    {
        public Guid ID { get; set; }

        public string TenantIdentifier { get; set; }
        public Guid TermId { get; set; }
        public Guid ClassID { get; set; }
        public string jsonClass { get; set; }
        public string jsonClassEnrolments { get; set; }
        public string jsonCourseEnrolments { get; set; }
    }
}
