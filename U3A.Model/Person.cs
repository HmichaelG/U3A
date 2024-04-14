using DevExpress.XtraRichEdit.Forms;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Text.Json.Serialization;
using U3A.Model;

namespace U3A.Model
{
    [Index(nameof(DataImportTimestamp))]
    [Index(nameof(ConversionID))]
    [Index(nameof(PersonID))]
    [Index(nameof(LastName), nameof(FirstName), nameof(Email))]
    public class Person : BaseEntity
    {
        
        [Key]

        [NotMapped]
        TextInfo info = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo;

        public Guid ID { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PersonID { get; set; }

        public int ConversionID { get; set; }

        public string? DataImportTimestamp { get; set; }

        public string? Title { get; set; }
        public string? PostNominals { get; set; }

        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; }
        [Required]
        [MaxLength(50)]
        public string LastName { get; set; }
        [Required]
        [MaxLength(50)]
        public string Address { get; set; }
        [Required]
        [MaxLength(50)]
        public string City { get; set; }
        [Required]
        [MaxLength(3)]
        public string State { get; set; }

        [Range(1, 9999)]
        public int Postcode { get; set; }
        public string AddressFull
        {
            get
            {
                return $"{Address.Trim()} {City.Trim().ToUpper()} {State} {Postcode}";
            }
        }

        [Required]
        [DefaultValue("Male")]
        public string Gender { get; set; }

        public DateTime? BirthDate { get; set; }
        public DateTime? DateJoined { get; set; }
        public DateTime? PreviousDateJoined { get; set; }

        public int? MembershipYears
        {
            get
            {
                return Age.Calculate(DateCeased, DateJoined);
            }
        }
        public DateTime? DateCeased { get; set; }

        [Range(constants.START_OF_TIME, 9999)]
        [DefaultValue(constants.START_OF_TIME)]
        public int FinancialTo { get; set; } = constants.START_OF_TIME;

        public int? PreviousFinancialTo { get; set; }


        [MaxLength(256)]
        [EmailAddressAlowNull]
        public string? Email { get; set; }

        public string? AdjustedEmail
        {
            get
            {
                string? result = null;
                if (Email != null && !IsEmailSilent)
                {
                    result = Email;
                }
                return result;
            }
        }

        public string Domain
        {
            get
            {
                var result = string.Empty;
                if (!string.IsNullOrWhiteSpace(Email))
                {
                    var splits = Email.Split("@");
                    if (splits.Length > 0) result = splits[1].ToLower();
                }
                return result;
            }
        }
        [MaxLength(20)]
        public string? HomePhone { get; set; }
        public string? AdjustedHomePhone
        {
            get
            {
                return adjustPhone(HomePhone);
            }
        }

        public string? HomePhoneOrSilent
        {
            get
            {
                return adjustSilentPhone(HomePhone);
            }
        }

        [MaxLength(20)]
        public string? Mobile { get; set; }

        public string? AdjustedMobile
        {
            get
            {
                return adjustPhone(Mobile);
            }
        }

        public string? MobileOrSilent
        {
            get
            {
                var ph = adjustSilentPhone(Mobile);
                if (SMSOptOut && ph != "Silent") ph = $"{ph} {constants.NO_SMS}";
                return ph;
            }
        }

        private string adjustPhone(string phoneNo)
        {
            string? result = null;
            if (phoneNo != null && !IsPhoneSilent)
            {
                result = phoneNo.Replace(" ", "").Replace("+61", "0");
                if (result.Length == 9 && !result.StartsWith("0")) { result = "0" + result; }
            }
            return result;
        }
        private string adjustSilentPhone(string phoneNo)
        {
            string? result = null;
            if (phoneNo != null)
            {
                if (IsPhoneSilent)
                {
                    result = "Silent";
                }
                else
                {
                    result = adjustPhone(phoneNo);
                }
            }
            return result;
        }

        [Obsolete]
        [MaxLength(20)]
        public string? SilentNumber { get; set; }
        public SilentContact SilentContact { get; set; } = SilentContact.None;
        [NotMapped]
        public string SilentNumberDisplayText
        {
            get
            {
                var silentNumberList = new SilentContactList();
                return silentNumberList.FirstOrDefault(x => x.Type == SilentContact).DisplayText;
            }
        }
        [NotMapped]
        public string SilentNumberShortDisplayText
        {
            get
            {
                var silentNumberList = new SilentContactList();
                return silentNumberList.FirstOrDefault(x => x.Type == SilentContact).ShortText;
            }
        }

        [NotMapped]
        public bool IsPhoneSilent
        {
            get
            {
                return (SilentContact == SilentContact.PhoneOnly
                            || SilentContact == SilentContact.Both);
            }
        }

        [NotMapped]
        public bool IsEmailSilent
        {
            get
            {
                return (SilentContact == SilentContact.EmailOnly
                            || SilentContact == SilentContact.Both);
            }
        }


        public bool SMSOptOut { get; set; }

        [Required]
        [MaxLength(50)]
        public string ICEContact { get; set; }
        [Required]
        [MaxLength(50)]
        public string ICEPhone { get; set; }
        [DefaultValue(false)]
        public string? AdjustedICEPhone
        {
            get
            {
                return adjustPhone(ICEPhone);
            }
        }

        public bool VaxCertificateViewed { get; set; }

        public string? Occupation { get; set; }

        [Required]
        [EmailCommunicationRequiresEmailAddress("Email", ErrorMessage = "Email communication method requires an email address")]
        public string Communication { get; set; } = "Email";

        [DefaultValue(false)]
        public Boolean IsLifeMember { get; set; }

        [NotMapped]
        public string FullName
        {
            get
            {
                string result;
                if (Title == null) { result = $"{ToTitleText(FirstName.Trim())} {ToTitleText(LastName.Trim())}"; }
                else
                {
                    result = $"{Title.Trim()} {ToTitleText(FirstName.Trim())} {ToTitleText(LastName.Trim())}";
                    if (result.Length > 25) { result = $"{Title.Trim()} {FirstName.Substring(0, 1).ToUpper()} {ToTitleText(LastName.Trim())}"; }
                }
                return result;
            }
        }
        public string FullNameWithPostNominals
        {
            get
            {
                string result = FullName;
                if (PostNominals != null) { result = $"{result} {PostNominals}"; }
                return result;
            }
        }

        [NotMapped]
        public string FullNameAlpha
        {
            get
            {
                string vTag = (IsMultiCampusVisitor) ? "[V]" : "";
                return ($"{ToTitleText(LastName.Trim())}, {ToTitleText(FirstName.Trim())} {vTag}").Trim();
            }
        }
        public string LastNameWithVisitorTag
        {
            get
            {
                string vTag = (IsMultiCampusVisitor) ? "[V]" : "";
                return ($"{ToTitleText(LastName.Trim())} {vTag}").Trim();
            }
        }
        public string FullNameWithVisitorTag
        {
            get
            {
                string vTag = (IsMultiCampusVisitor) ? "[V]" : "";
                return ($"{FullName} {vTag}").Trim();
            }
        }
        public string FullNameAlphaKey
        {
            get
            {
                return $"{ToTitleText(LastName.Trim())}{ToTitleText(FirstName.Trim())}{PersonID}";
            }
        }

        string ToTitleText(string Name)
        {
            string result = Name;
            if (result.ToUpper() == Name)
            {
                result = result.Replace("'", "\t");
                result = info.ToTitleCase(result.ToLower());
                result = result.Replace("\t", "'");
            }
            return result;
        }

        [NotMapped]
        public string AlphaGroup
        {
            get
            {
                return LastName.Substring(0, 1).ToUpper();
            }
        }
        [NotMapped]
        public string PersonSummary
        {
            get
            {
                var s = $"{FullName} ";
                if (!string.IsNullOrWhiteSpace(AdjustedMobile)) { s = $"{s} Mob: {AdjustedMobile}"; }
                if (SMSOptOut) { s = $"{s} {constants.NO_SMS}"; }
                else if (!string.IsNullOrWhiteSpace(AdjustedHomePhone)) { s = $"{s} Ph: {AdjustedHomePhone}"; }
                if (!string.IsNullOrWhiteSpace(Email)) { s = $"{s} Email: {Email}"; }
                return s.Trim();
            }
        }

        [NotMapped]
        public string PersonIdentity
        {
            get
            {
                if (FirstName != null && LastName != null)
                {
                    return $"{FirstName.Substring(0, 1).ToLower()}{LastName.Substring(0, 1).ToLower()}{PersonID.ToString("0000")}";
                }
                else
                {
                    return "To be calculated";
                }
            }
        }

        [NotMapped]
        [DefaultValue(false)]
        [Comment("Set in business rule: ApplyGroupsAsync")]
        public Boolean IsCourseLeader { get; set; }

        [NotMapped]
        [DefaultValue(false)]
        [Comment("Set in business rule: ApplyGroupsAsync")]
        public Boolean IsCourseClerk { get; set; }

        [NotMapped]
        [DefaultValue(false)]
        [Comment("Set in business rule: ApplyGroupsAsync")]
        public Boolean IsCommitteeMember { get; set; }

        [NotMapped]
        [DefaultValue(false)]
        [Comment("Set in business rule: ApplyGroupsAsync")]
        public Boolean IsVolunteer { get; set; }

        [InverseProperty("Leader")]
        [JsonIgnore] public List<Class> LeaderOf { get; set; } = new List<Class>();
        [InverseProperty("Leader2")]
        [JsonIgnore] public List<Class> Leader2Of { get; set; } = new List<Class>();
        [InverseProperty("Leader3")]
        [JsonIgnore] public List<Class> Leader3Of { get; set; } = new List<Class>();

        /// <summary>
        /// Populated in BusinessRule. Use when printing enrolled classes for a student.
        /// </summary>
        [NotMapped]
        [JsonIgnore] public List<Class> EnrolledClasses { get; set; } = new List<Class>();

        [JsonIgnore] public List<Leave> Leave { get; set; } = new List<Leave>();
        [JsonIgnore] public List<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
        [JsonIgnore] public List<Receipt> Receipts { get; set; } = new List<Receipt>();
        [JsonIgnore] public List<Fee> Fees { get; set; } = new List<Fee>();
        [JsonIgnore] public List<ReceiptDataImport> ReceiptDataImports { get; set; } = new List<ReceiptDataImport>();

        [NotMapped][JsonIgnore] public bool IsMultiCampusVisitor { get; set; } = false;

    }
    public class PersonList : BindingList<Person> { }

    public class PersonTitles : List<string>
    {
        public PersonTitles()
        {
            AddRange(new string[] { "Mr", "Mrs", "Ms", "Mx" });
        }
    }

    public class EmailCommunicationRequiresEmailAddressAttribute : ValidationAttribute
    {
        private readonly string _emailAddress;

        public EmailCommunicationRequiresEmailAddressAttribute(string EmailAddress)
        {
            _emailAddress = EmailAddress;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            ErrorMessage = ErrorMessageString;
            var CommunicationMethod = (string?)value;

            var property = validationContext.ObjectType.GetProperty(_emailAddress);

            if (property == null)
                throw new ArgumentException("Property with this name not found");

            var emailAddress = (string?)property.GetValue(validationContext.ObjectInstance);

            if (CommunicationMethod.ToLower() == "email" && string.IsNullOrWhiteSpace(emailAddress))
                return new ValidationResult(ErrorMessage);

            return ValidationResult.Success;
        }
    }
}

public class EmailAddressAlowNullAttribute : ValidationAttribute
{

    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        ErrorMessage = ErrorMessageString;
        var emailAddress = (string)value;

        if (string.IsNullOrWhiteSpace(emailAddress)) return ValidationResult.Success;

        var a = new EmailAddressAttribute();
        return a.GetValidationResult(value, validationContext);
    }
}

