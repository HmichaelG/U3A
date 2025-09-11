using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace U3A.Model
{
    [Index(nameof(Year), nameof(Name))]
    public class Course : BaseEntity, ISoftDelete
    {
        [Key]
        public Guid ID { get; set; }

        public CourseEditViewType EditType { get; set; }

        public int ConversionID { get; set; }

        [Required]
        [DefaultValue(2022)]
        public int Year { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [FeaturedCourseCannotBeOffSchedule(ErrorMessage = "Featured courses cannot be off-schedule")]
        public bool IsFeaturedCourse { get; set; }

        public DateTime? AllowMultiCampsuFrom { get; set; }

        [DefaultValue(" ")]
        public string? Description { get; set; }

        public string DisplayDescription { get; set; }
        public string DisplayDescriptionWithoutImages { get; set; }

        public bool IsOffScheduleActivity { get; set; }

        [DefaultValue(0)]
        public int? CourseParticipationTypeID { get; set; } = (int)ParticipationType.SameParticipantsInAllClasses;
        public CourseParticipationType CourseParticipationType { get; set; }
        public bool EnforceOneStudentPerClass { get; set; } = true; // Ooops! One Class Per Student

        public bool OneStudentPerClass
        {
            get
            {
                return (((ParticipationType)CourseParticipationTypeID) == ParticipationType.DifferentParticipantsInEachClass && EnforceOneStudentPerClass);
            }
        }

        [Required]
        [Precision(precision: 18, 2)]
        [DefaultValue(0.00)]
        [Comment("Optional once-only course enrolment fee")]
        public decimal CourseFeePerYear { get; set; }
        public DateOnly? CourseFeePerYearDueDate { get; set; }

        [DefaultValue(" ")]
        public string? CourseFeePerYearDescription { get; set; }

        [Comment("Overrides the System Settings complimentary status for year fees")]
        public bool OverrideComplimentaryPerYearFee { get; set; }
        public bool LeadersPayYearFee { get; set; }

        [Required]
        [Precision(precision: 18, 2)]
        [DefaultValue(0.00)]
        [Comment("Optional fee per term)")]
        public decimal CourseFeePerTerm { get; set; }
        
        [Required]
        [Precision(precision: 18, 2)]
        [DefaultValue(0.00)]
        [Comment("Optional fee per term)")]
        public decimal CourseFeeTerm1 { get; set; }
        [Required]
        [Precision(precision: 18, 2)]
        [DefaultValue(0.00)]
        [Comment("Optional fee per term)")]
        public decimal CourseFeeTerm2 { get; set; }
        [Required]
        [Precision(precision: 18, 2)]
        [DefaultValue(0.00)]
        [Comment("Optional fee per term)")]
        public decimal CourseFeeTerm3 { get; set; }
        [Required]
        [Precision(precision: 18, 2)]
        [DefaultValue(0.00)]
        [Comment("Optional fee per term)")]
        public decimal CourseFeeTerm4 { get; set; }

        [NotMapped]
        public bool HasTermFees
        {
            get
            {
                bool result = false;
                if (CourseFeeTerm1 != 0) result = true;
                if (CourseFeeTerm2 != 0) result = true;
                if (CourseFeeTerm3 != 0) result = true;
                if (CourseFeeTerm4 != 0) result = true;
                return result;
            }
        }
        [NotMapped]
        public bool HasFees
        {
            get
            {
                return HasTermFees || CourseFeePerYear != 0;
            }
        }

        [NotMapped]
        public string TermFeesText
        {
            get
            {
                string result = TermFeesTextNoTitle;
                if (result.Length > 0)
                {
                    result = "Term Fees: " + result;
                }
                return result;
            }
        }
        [NotMapped]
        public string TermFeesTextNoTitle
        {
            get
            {
                var sb = new StringBuilder();
                decimal[] fees = { CourseFeeTerm1, CourseFeeTerm2, CourseFeeTerm3, CourseFeeTerm4 };
                if (fees.Any(x => x > 0))
                {
                    //special case for all fees are equal
                    if (fees.All(x => x == fees[0]) && fees[0] > 0)
                    {
                        sb.Append($"{fees[0]:C} per term");
                        return sb.ToString();
                    }

                    for (int i = 0; i < fees.Length; i++)
                    {
                        if (fees[i] > 0)
                        {
                                sb.Append($"Term {i + 1}: {fees[i]:C} ");
                        }
                    }
                }
                return sb.ToString().Trim();
            }
        }

        public int? CourseFeePerTermDueWeeks { get; set; }

        [DefaultValue(" ")]
        public string? CourseFeePerTermDescription { get; set; }
        [Comment("Overrides the System Settings complimentary status for term fees")]
        public bool OverrideComplimentaryPerTermFee { get; set; }
        public bool LeadersPayTermFee { get; set; }

        [Required]
        [Comment("The time in hours each class is expected to take")]
        [Precision(precision: 18, 2)]
        [DefaultValue(2.00)]
        public decimal Duration { get; set; }

        [Required]
        [Comment("The required number of students per class")]
        [DefaultValue(6)]
        public int RequiredStudents { get; set; }

        [Required]
        [Comment("The maximum number of students per class")]
        [DefaultValue(28)]
        public int MaximumStudents { get; set; }
        [Required]

        [DefaultValue(true)]
        public bool AllowAutoEnrol { get; set; } = true;

        [MaxLength(50)]
        public string? AutoEnrolDisabledReason { get; set; }

        [Required]
        public bool ExcludeFromLeaderComplimentaryCount { get; set; }
        public SendLeaderReportsTo? SendLeaderReportsTo { get; set; }
        public CourseContactOrder? CourseContactOrder { get; set; }

        [Url]
        public string? AdditionalInformationURL { get; set; }

        [Required]
        public CourseType CourseType { get; set; }
        public Guid? CourseTypeID { get; set; }

        [JsonIgnore]
        public List<Class> Classes { get; set; } = new List<Class>();

        [Comment("Ignore dB values. Used for JSON only")]
        public List<String> ClassSummaries { get; set; } = new();

        [JsonIgnore]
        public List<Enrolment> Enrolments { get; set; } = new List<Enrolment>();

        [JsonIgnore]
        public List<Leave> Leave { get; set; } = new List<Leave>();

        [NotMapped]
        public string NameAndNumber
        {
            get
            {
                return (ConversionID > 0)
                    ? $"{ConversionID}: {Name}"
                    : Name.Trim();
            }
        }

        [NotMapped]
        public string? OfferedBy { get; set; } // The U3A that owns the course
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public override int GetHashCode()
        {
            int hash = 17; // Initial value (usually a prime number)
            return hash * ID.GetHashCode();
        }
        public override bool Equals(object? obj)
        {
            if (obj == null || !(obj is Course))
                return false;
            else
                return this.GetHashCode() == ((Course)obj).GetHashCode();
        }

    }

    public class UrlAttribute : ValidationAttribute
    {
        public UrlAttribute()
        {
        }

        public override bool IsValid(object value)
        {
            var text = value as string;
            Uri uri;

            return (string.IsNullOrWhiteSpace(text) || Uri.TryCreate(text, UriKind.Absolute, out uri));
        }
    }

    public class FeaturedCourseCannotBeOffScheduleAttribute : ValidationAttribute
    {

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            ErrorMessage = ErrorMessageString;

            var offSchedule = validationContext.ObjectType.GetProperty("IsOffScheduleActivity");

            if (offSchedule == null)
                throw new ArgumentException("Property with this name not found");

            var isOffSchedule = (bool)offSchedule.GetValue(validationContext.ObjectInstance);

            if ((bool)value && isOffSchedule)
                return new ValidationResult(ErrorMessage);

            return ValidationResult.Success;
        }
    }


}
