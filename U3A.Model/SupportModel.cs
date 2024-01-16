using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U3A.Model
{
    [NotMapped]
    public class SupportModel : IValidatableObject
    {
        [Required]
        public string Name { get; set; }
        public string Phone { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        [EmailAddress]
        public string? Email { get; set; }
        [Required]
        public string Description { get; set; }
        public string Captcha { get; set; } = "";
        public string CaptchaResponse { get; set; } = "";
        public bool IsCaptchaRequired { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (IsCaptchaRequired && Captcha.ToLower() != CaptchaResponse.ToLower()) {
                yield return new ValidationResult("Your entry must match the Captcha code exactly.");
            }
            if (string.IsNullOrWhiteSpace(Email) && string.IsNullOrWhiteSpace(Phone) && string.IsNullOrWhiteSpace(Mobile))
            {
                yield return new ValidationResult("Please enter a contact method (Phone, mobile or email).");
            }
        }
    }
}
