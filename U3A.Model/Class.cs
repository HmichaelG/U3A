using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace U3A.Model
{
    public class Class
        : BaseEntity, INotifyPropertyChanged, ISoftDelete
    {

        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool mIsSelected;
        [NotMapped]
        public bool IsSelected
        {
            get { return mIsSelected; }
            set
            {
                mIsSelected = value;
                NotifyPropertyChanged();
            }
        }

        // The enrolment that caused Iselected = true
        [NotMapped] public Enrolment? IsSelectedByEnrolment { get; set; }

        [NotMapped] public bool IsNotSelected { get { return !IsSelected; } }

        [NotMapped] public string TenantIdentifier { get; set; }

        private DateTime mStartTime { get; set; }

        [NotMapped]
        public int SotrOrder { get; set; }

        [Key]
        public Guid ID { get; set; }

        [Comment("Is the course offered in term 1?")]
        [DefaultValue(true)]
        public Boolean OfferedTerm1 { get; set; } = true;

        [Comment("Is the course offered in term 2?")]
        [DefaultValue(true)]
        public Boolean OfferedTerm2 { get; set; } = true;

        [Comment("Is the course offered in term 3?")]
        [DefaultValue(true)]
        public Boolean OfferedTerm3 { get; set; } = true;

        [Comment("Is the course offered in term 4?")]
        [DefaultValue(true)]
        public Boolean OfferedTerm4 { get; set; } = true;

        [NotMapped]
        public string OfferedSummary
        {
            get
            {
                string result = string.Empty;
                if (OfferedTerm1) { result = $"{result} T1"; }
                if (OfferedTerm2) { result = $"{result} T2"; }
                if (OfferedTerm3) { result = $"{result} T3"; }
                if (OfferedTerm4) { result = $"{result} T4"; }
                if (Course != null) { result = $"{Course.Year}{result}"; }
                return result.Trim();
            }
        }
        [NotMapped]
        public string OfferedSummaryAdjusted
        {
            get
            {
                string result = string.Empty;
                switch (TermNumber)
                {
                    case 0:
                    case 1:
                        if (OfferedTerm1) { result = $"{result} T1"; }
                        if (OfferedTerm2) { result = $"{result} T2"; }
                        if (OfferedTerm3) { result = $"{result} T3"; }
                        if (OfferedTerm4) { result = $"{result} T4"; }
                        break;
                    case 2:
                        if (OfferedTerm2) { result = $"{result} T2"; }
                        if (OfferedTerm3) { result = $"{result} T3"; }
                        if (OfferedTerm4) { result = $"{result} T4"; }
                        break;
                    case 3:
                        if (OfferedTerm3) { result = $"{result} T3"; }
                        if (OfferedTerm4) { result = $"{result} T4"; }
                        break;
                    case 4:
                        if (OfferedTerm4) { result = $"{result} T4"; }
                        break;
                }
                if (Course != null) { result = $"{Course.Year}{result}"; }
                return result.Trim();
            }
        }

        [Description("Optional Start Date. Only set when Start Date is not the start of term")]
        public DateTime? StartDate { get; set; }

        [DefaultValue(2)]
        public int? OccurrenceID { get; set; }
        public Occurrence Occurrence { get; set; }

        [Description("Optional. Number of recurrancies. Only set when not the end of term")]
        public int? Recurrence { get; set; }

        public int OnDayID { get; set; }
        [Required]
        public WeekDay OnDay { get; set; }

        [Required]
        public DateTime StartTime
        {
            get { return mStartTime; }
            set
            {
                if (value != this.mStartTime)
                {
                    this.mStartTime = new DateTime(1, 1, 1, value.Hour, value.Minute, 0);
                    NotifyPropertyChanged();
                }
            }
        }

        [NotMapped]
        public string OccurrenceTextBrief
        {
            get
            {
                string result = string.Empty;
                if (OccurrenceID != null)
                {
                    switch ((OccurrenceType)OccurrenceID)
                    {
                        case OccurrenceType.OnceOnly:
                            result += (StartDate.HasValue) ? StartDate.Value.ToString("ddd dd-MMM-yy") : "Term Start";
                            break;
                        case OccurrenceType.Daily:
                            result = "Daily: Mon - Fri";
                            break;
                        case OccurrenceType.Weekly:
                            result = $"{OnDay.Day}, Weekly";
                            break;
                        case OccurrenceType.Fortnightly:
                            result = $"{OnDay.Day}, Fortnightly";
                            break;
                        case OccurrenceType.FirstWeekOfMonth:
                            result = $"{OnDay.Day}, Week 1";
                            break;
                        case OccurrenceType.SecondWeekOfMonth:
                            result = $"{OnDay.Day}, Week 2";
                            break;
                        case OccurrenceType.ThirdWeekOfMonth:
                            result = $"{OnDay.Day}, Week 3";
                            break;
                        case OccurrenceType.FourthWeekOfMonth:
                            result = $"{OnDay.Day}, Week 4";
                            break;
                        case OccurrenceType.LastWeekOfMonth:
                            result = $" {OnDay.Day}, Last Week";
                            break;
                        case OccurrenceType.Every5Weeks:
                            result = $" {OnDay.Day}, Week 5";
                            break;
                        case OccurrenceType.Every6Weeks:
                            result = $" {OnDay.Day}, Week 6";
                            break;
                        case OccurrenceType.FirstAndThirdWeekOfMonth:
                            result = $" {OnDay.Day}, Weeks 1 & 3";
                            break;
                        case OccurrenceType.SecondAndFourthWeekOfMonth:
                            result = $" {OnDay.Day}, Weeks 2 & 4";
                            break;
                        case OccurrenceType.Unscheduled:
                            result = "Varies";
                            break;
                    }
                }
                return result;
            }
        }
        [NotMapped]
        public string OccurrenceText
        {
            get
            {
                string result = string.Empty;
                switch ((OccurrenceType)OccurrenceID)
                {
                    case OccurrenceType.OnceOnly:
                        result += (StartDate.HasValue) ? StartDate.Value.ToString("ddd dd-MMM-yy") : "Term Start";
                        break;
                    case OccurrenceType.Daily:
                        result = "Daily: Mon - Fri";
                        result = $"{result}{GetDateRange()}";
                        break;
                    case OccurrenceType.Weekly:
                        result = $"{OnDay.Day}, Weekly";
                        result = $"{result}{GetDateRange()}";
                        break;
                    case OccurrenceType.Fortnightly:
                        result = $"{OnDay.Day}, Fortnightly";
                        result = $"{result}{GetDateRange()}";
                        break;
                    case OccurrenceType.FirstWeekOfMonth:
                        result = $"{OnDay.Day}, Week 1";
                        result = $"{result}{GetDateRange()}";
                        break;
                    case OccurrenceType.SecondWeekOfMonth:
                        result = $"{OnDay.Day}, Week 2";
                        result = $"{result}{GetDateRange()}";
                        break;
                    case OccurrenceType.ThirdWeekOfMonth:
                        result = $"{OnDay.Day}, Week 3";
                        result = $"{result}{GetDateRange()}";
                        break;
                    case OccurrenceType.FourthWeekOfMonth:
                        result = $"{OnDay.Day}, Week 4";
                        result = $"{result}{GetDateRange()}";
                        break;
                    case OccurrenceType.LastWeekOfMonth:
                        result = $" {OnDay.Day}, Last Week";
                        result = $"{result}{GetDateRange()}";
                        break;
                    case OccurrenceType.Every5Weeks:
                        result = $" {OnDay.Day}, Weeks 5";
                        result = $"{result}{GetDateRange()}";
                        break;
                    case OccurrenceType.Every6Weeks:
                        result = $" {OnDay.Day}, Week 6";
                        result = $"{result} {GetDateRange()}";
                        break;
                    case OccurrenceType.FirstAndThirdWeekOfMonth:
                        result = $" {OnDay.Day}, Weeks 1 & 3";
                        result = $"{result} {GetDateRange()}";
                        break;
                    case OccurrenceType.SecondAndFourthWeekOfMonth:
                        result = $" {OnDay.Day}, Weeks 2 & 4";
                        result = $"{result} {GetDateRange()}";
                        break;
                    case OccurrenceType.Unscheduled:
                        result = "Unscheduled (Varies)";
                        break;
                }
                return result;
            }
        }

        string GetDateRange()
        {
            string result = string.Empty;
            if (StartDate.HasValue && Recurrence.HasValue)
            {
                result = $": From {StartDate.Value.ToString("dd-MMM-yy")}, {Recurrence} repeats.";
            }
            if (!StartDate.HasValue && !Recurrence.HasValue) { result = ": For full term."; }
            if (StartDate.HasValue && !Recurrence.HasValue) { result = $": From {StartDate.Value.ToString("dd-MMM-yy")} till End of Term."; }
            if (!StartDate.HasValue && Recurrence.HasValue) { result = $": From Start of Term, {Recurrence} repeats."; }
            return result;
        }


        [NotMapped]
        public DateTime? EndTime
        {
            get
            {
                DateTime? result = null;
                if (Course != null) { result = StartTime.AddHours((double)Course.Duration); }
                return result;
            }
        }
        [NotMapped]
        public string StrEndTime
        {
            get
            {
                string result = String.Empty;
                if (Course != null) { result = StartTime.AddHours((double)Course.Duration).ToString("t").Trim(); }
                return result;
            }
        }

        [NotMapped]
        public string CourseSummary
        {
            get
            {
                var s = $"{Course.Name} {OccurrenceText}: {GetDurationText}";
                if (Venue != null) { s = $"{s} {Venue.Name}"; }
                return s;
            }
        }
        [NotMapped]
        public string ClassSummary
        {
            get
            {
                var s = $"{OccurrenceText}: {GetDurationText}";
                if (Venue != null) { s = $"{s} {Venue.Name}"; }
                return s;
            }
        }
        [NotMapped]
        public string ClassDetail
        {
            get
            {
                var s = $"{OccurrenceText} {GetDurationText}";
                if (Venue != null) { s = $"{s} {Venue.Name}"; }
                return s;
            }
        }
        [NotMapped]
        public string ClassDetailWithoutVenue
        {
            get
            {
                var s = $"{OccurrenceText} {GetDurationText}";
                return s;
            }
        }

        private string GetDurationText
        {
            get
            {
                var s = (StartTime.Hour == 0)
                    ? $"{Course?.Duration.ToString("n2")} hours"
                    : $"{StartTime.ToString("t").Trim()} to {StrEndTime}";
                return s;
            }
        }

        public Course Course { get; set; }
        public Guid CourseID { get; set; }

        [Required]
        public Venue Venue { get; set; }
        public Guid? VenueID { get; set; }

        public string? GuestLeader { get; set; }
        public Guid? LeaderID { get; set; }

        [ForeignKey("LeaderID")]
        public Person? Leader { get; set; }

        public Guid? Leader2ID { get; set; }

        [ForeignKey("Leader2ID")]
        public Person? Leader2 { get; set; }

        public Guid? Leader3ID { get; set; }

        [ForeignKey("Leader3ID")]
        public Person? Leader3 { get; set; }

        [JsonIgnore] public List<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
        [JsonIgnore] public List<CancelClass> CancelledClasses { get; set; } = new List<CancelClass>();

        [NotMapped]
        public string LeaderSummary
        {
            get
            {
                var result = "";
                if (!string.IsNullOrWhiteSpace(GuestLeader))
                    result = GuestLeader.ToString();
                else if (Leader != null) { result = Leader.PersonSummary; }
                return result.Trim();
            }
        }
        [NotMapped]
        public string LeaderSummaryBrief
        {
            get
            {
                var result = "";
                if (!string.IsNullOrWhiteSpace(GuestLeader))
                    result = GuestLeader.ToString();
                else if (Leader != null) { result = Leader.FullName; }
                return result.Trim();
            }
        }

        [NotMapped]
        public List<Person> Clerks { get; set; } = new List<Person>();

        [NotMapped]
        public string ContactDetails
        {
            get
            {
                var contactDetails = string.Empty;
                foreach (var c in CourseContacts)
                {
                    var p = c.Person;
                    contactDetails += $"({c.ContactType}) {p.PersonSummary}{Environment.NewLine}";
                }
                return (string.IsNullOrEmpty(contactDetails)) ? "Not Assigned" : contactDetails;
            }
        }

        [NotMapped]
        public string LeaderDetails
        {
            get
            {
                var result = string.Empty;
                if (!string.IsNullOrWhiteSpace(GuestLeader)) result = $"{GuestLeader}{Environment.NewLine}";
                result += $"{Leader?.PersonSummary}{Environment.NewLine}";
                result += $"{Leader2?.PersonSummary}{Environment.NewLine}";
                result += $"{Leader3?.PersonSummary}{Environment.NewLine}";
                return result.Trim();
            }
        }
        [NotMapped]
        public string LeaderNamesOnly
        {
            get
            {
                var result = string.Empty;
                if (!string.IsNullOrWhiteSpace(GuestLeader)) result = $"{GuestLeader}{Environment.NewLine}";
                result += $"{Leader?.FullNameWithPostNominals}{Environment.NewLine}";
                result += $"{Leader2?.FullNameWithPostNominals}{Environment.NewLine}";
                result += $"{Leader3?.FullNameWithPostNominals}{Environment.NewLine}";
                return result.Trim();
            }
        }

        [NotMapped]
        public string PrimaryLeader
        {
            get
            {
                var result = "The Group";
                if (Leader3 != null) result = Leader3?.FullNameWithPostNominals;
                if (Leader2 != null) result = Leader2?.FullNameWithPostNominals;
                if (Leader != null) result = Leader?.FullNameWithPostNominals;
                if (!string.IsNullOrWhiteSpace(GuestLeader)) result = GuestLeader;
                return result.Trim();
            }
        }

        [NotMapped]
        public List<CourseContact> CourseContacts { get; set; } = new List<CourseContact>();

        public string CourseContactDetails
        {
            get
            {
                var result = string.Empty;
                var type = string.Empty;
                foreach (var contact in CourseContacts)
                {
                    type = (contact.ContactType == CourseContactType.Leader) ? "Leader" : "Clerk";
                    result += $"({type}) {contact.Person.PersonSummary}{Environment.NewLine}";
                }
                return result.Trim();
            }
        }

        [NotMapped]
        public CourseContact? SelectedCourseContact
        {
            get
            {
                if (CourseContacts.Count > 0) return CourseContacts[_selectedContact]; else return null;
            }
        }

        private int _selectedContact = 0;
        public void GetNextCourseContact()
        {
            if (CourseContacts.Count > 0)
            {
                _selectedContact++;
                if (_selectedContact >= CourseContacts.Count) _selectedContact = 0;
            }
        }

        [NotMapped]
        [Comment("Set By Business Rule")]
        public int TotalActiveStudents { get; set; }
        [NotMapped]
        [Comment("Set By Business Rule")]
        public int TotalWaitlistedStudents { get; set; }

        [NotMapped]
        [Comment("Set By Business Rule")]
        public double ParticipationRate { get; set; }

        [NotMapped]
        [Comment("Only used in Member Portal - Member Enrolment")]
        public int TermNumber { get; set; }
        [NotMapped]
        [Comment("Only used in Member Portal - Member Enrolment")]
        public bool ShowMap { get; set; } = false;
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public override int GetHashCode()
        {
            int hash = 17; // Initial value (usually a prime number)
            return hash * ID.GetHashCode();
        }
        public override bool Equals(object? obj)
        {
            if (obj == null || !(obj is Class))
                return false;
            else
                return this.GetHashCode() == ((Class)obj).GetHashCode();
        }

    }
}
