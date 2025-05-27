using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace U3A.Model
{
    public class MultiCampusTerm : BaseEntity
    {
        [Key]
        public Guid ID { get; set; }
        public string TenantIdentifier { get; set; }
        public int Year { get; set; }
        public int TermNumber { get; set; }
        public DateTime StartDate { get; set; }
        public int Duration { get; set; }
        public int EnrolmentStarts { get; set; }
        public int EnrolmentEnds { get; set; }
        public bool IsDefaultTerm { get; set; }
        public bool IsClassAllocationFinalised { get; set; }

    }
}
