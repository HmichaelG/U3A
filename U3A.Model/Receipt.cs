using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO.IsolatedStorage;
using System.Runtime.CompilerServices;

namespace U3A.Model
{
    [Index(nameof(ProcessingYear))]
    [Index(nameof(Date), nameof(Description))]
    public class Receipt : BaseEntity
    {
        public Guid ID { get; set; }
        [Required]
        public DateTime Date { get; set; }
        [Required]
        public int ProcessingYear { get; set; }
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "You must enter a positive Amount.")]
        [Precision(precision: 18, 2)]
        public decimal Amount { get; set; }
        [Required]
        public string Description { get; set; }
        public string? Identifier { get; set; }
        [Required]
        public Guid PersonID { get; set; }
        [Required]
        [ForeignKey("PersonID")]
        public Person Person { get; set; }
        public int FinancialTo { get; set; }
        public int? TermPaid { get; set; }
        public DateTime DateJoined { get; set; }

        [NotMapped]
        public bool IsOnlinePayment
        {
            get
            {
                var result = false;
                if (Description.ToLower().StartsWith("eway online ")) { result = true; }
                return result;
            }
        }
        [NotMapped]
        public decimal? OnlineAmount
        {
            get { return (IsOnlinePayment) ? Amount : null; }
        }
        [NotMapped]
        public decimal? OtherAmount
        {
            get { return (!IsOnlinePayment) ? Amount : null; }
        }
    }
}
