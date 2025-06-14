﻿using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace U3A.Model
{
    public class ReceiptDataImport : BaseEntity
    {
        public Guid ID { get; set; }
        public DateTime Date { get; set; }
        [Precision(precision: 18, 2)]
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public string? Identifier { get; set; }
        public Guid? PersonID { get; set; }
        public Person? Person { get; set; }
        public int? FinancialTo { get; set; }
        public int? FinancialToTerm { get; set; }
        public bool IsOnFile { get; set; }

        [NotMapped] public string ImportIssues { get; set; }

    }
}
