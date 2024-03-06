using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U3A.Model
{
    [Index(nameof(Year), nameof(Name))]
    public class Course : BaseEntity
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

        [DefaultValue(" ")]
        public string? Description { get; set; }

        [DefaultValue(0)]
        public int? CourseParticipationTypeID { get; set; } = (int)ParticipationType.SameParticipantsInAllClasses;
        public CourseParticipationType CourseParticipationType { get; set; }
        public bool EnforceOneStudentPerClass { get; set; } = true;

        [Required]
        [Precision(precision: 18, 2)]
        [DefaultValue(0.00)]
        [Comment("Optional once-only course enrolment fee")]
        public decimal CourseFeePerYear { get; set; }

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

        [Required]
        public bool ExcludeFromLeaderComplimentaryCount { get; set; }
        public SendLeaderReportsTo? SendLeaderReportsTo { get; set; }
        public CourseContactOrder? CourseContactOrder { get; set; }
        
        [Url]
        public string? AdditionalInformationURL { get; set; }

        [Required]
        public CourseType CourseType { get; set; }
        public Guid? CourseTypeID { get; set; }

        public List<Class> Classes { get; set; } = new List<Class>();

        public List<Enrolment> Enrolments { get; set; } = new List<Enrolment>();

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

}
