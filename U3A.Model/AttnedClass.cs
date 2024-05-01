using DevExpress.XtraRichEdit.Fields;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace U3A.Model
{
    [Index(new string[] { nameof(TermID), nameof(ClassID), nameof(Date) }, IsUnique = false)]
    public class AttendClass
    {

        [Key]
        public Guid ID { get; set; }

        public bool IsAdHoc { get; set; }
        public Guid TermID { get; set; }
        public Term Term { get; set; }

        public Guid ClassID { get; set; }
        public Class Class { get; set; }

        public DateTime Date { get; set; }

        public Guid PersonID { get; set; }
        public Person Person { get; set; }

        [Required]
        public AttendClassStatus AttendClassStatus { get; set; }

        public int AttendClassStatusID { get; set; }

        public string Comment { get; set; }

        public DateTime? DateProcessed { get; set; }
        public DateTime? SignIn { get; set; }
        public DateTime? SignOut { get; set; }

        //Pivot Grid Counts
        [NotMapped]
        public string CourseTypeName
        {
            get
            {
                return Class.Course.CourseType.Name;
            }
        }
        [NotMapped]
        public string CourseName
        {
            get
            {
                return Class.Course.Name;
            }
        }
        [NotMapped]
        public string VenueName
        {
            get
            {
                return Class.Venue.Name;
            }
        }
        [NotMapped]
        public string PersonName
        {
            get
            {
                return Person.FullNameAlpha;
            }
        }
        [NotMapped]
        public DateTime WeekEndDate
        {
            get
            {
                var weekdayDate = Date.Date;
                while (weekdayDate.DayOfWeek != DayOfWeek.Sunday) weekdayDate = weekdayDate.AddDays(1);
                return DateCalculations.GetWeekEndingDate(Date);
            }
        }
        [NotMapped]
        public int Actual
        {
            get
            {
                return (AttendClassStatusID == 0) ? 1 : 0;
            }
        }
        [NotMapped]
        public int Expected
        {
            get
            {
                return (IsAdHoc) ? -1 : 1;
            }
        }
        [NotMapped]
        public int Difference
        {
            get
            {
                return Actual - Expected;
            }
        }

    }

    [NotMapped]
    public class AttendClassSummary
    {

        public Guid PersonID { get; set; }
        public Person Person { get; set; }
        public int Present { get; set; }
        public int AbsentWithApology { get; set; }
        public int AbsentWithoutApology { get; set; }

        public int Total
        {
            get
            {
                return Present + AbsentWithApology + AbsentWithoutApology;
            }
        }

    }
    [NotMapped]
    public class AttendClassSummaryByWeek
    {
        public DateTime WeekEnd { get; set; }
        public int Present { get; set; }
        public int AbsentWithApology { get; set; }
        public int AbsentWithoutApology { get; set; }

        public int Total
        {
            get
            {
                return Present + AbsentWithApology + AbsentWithoutApology;
            }
        }
    }
    [NotMapped]
    public class AttendClassDetailByWeek
    {
        public string ID
        {
            get
            {
                return CourseID.ToString() + ClassID.ToString();
            }
        }
        public Guid CourseID { get; set; }
        public Guid CourseTypeID { get; set; }
        public Guid ClassID { get; set; }
        public int OccurrenceTypeID { get; set; }
        public string CourseDescription { get; set; }
        public string CourseTypeDescription { get; set; }
        public string FullDescription
        {
            get
            {
                return $"{CourseTypeDescription}: {CourseDescription}";
            }
        }
        public DateTime WeekEnd { get; set; }
        public int Present { get; set; }
        public int AbsentWithApology { get; set; }
        public int AbsentWithoutApology { get; set; }

        public int Total
        {
            get
            {
                return Present + AbsentWithApology + AbsentWithoutApology;
            }
        }
        public double PresentPC
        {
            get
            {
                return (Total != 0) ? (double)Present / (double)Total : 0.00;
            }
        }
        public double AbsentWithApologyPC
        {
            get
            {
                return (Total != 0) ? (double)AbsentWithApology / (double)Total : 0.00;
            }
        }
        public double AbsentWithoutApologyPC
        {
            get
            {
                return (Total != 0) ? (double)AbsentWithoutApology / (double)Total : 0.00;
            }
        }

    }

    [NotMapped]
    public class AttendClassSummaryByCourse
    {
        public string Course { get; set; }
        public Guid ClassID { get; set; }
        public string DateSummary { get; set; }
        public int ClassesRecorded { get; set; }
        public int Present { get; set; }
        public int AbsentWithApology { get; set; }
        public int AbsentWithoutApology { get; set; }

        public int Total
        {
            get
            {
                return Present + AbsentWithApology + AbsentWithoutApology;
            }
        }
    }
    [NotMapped]
    public class AttendanceRecorded
    {
        public Guid ClassID { get; set; }
        public string CourseDetail { get; set; }
        public string CourseName { get; set; }
        public DateTime ClassDate { get; set; }
        public bool IsCancelled { get; set; }
        public int Present { get; set; }
        public int AbsentWithApology { get; set; }
        public int AbsentWithoutApology { get; set; }

        public int Total
        {
            get
            {
                return Present + AbsentWithApology + AbsentWithoutApology;
            }
        }
    }
}
