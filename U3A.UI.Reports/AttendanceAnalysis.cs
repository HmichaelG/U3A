using DevExpress.Web.Internal;
using DevExpress.XtraReports.UI;
using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using U3A.BusinessRules;
using U3A.Database;
using U3A.Model;

namespace U3A.UI.Reports
{
    public partial class AttendanceAnalysis : DevExpress.XtraReports.UI.XtraReport, IXtraReportWithDbContext
    {
        public AttendanceAnalysis()
        {
            InitializeComponent();
        }
        public U3ADbContext DbContext { get; set; }
        List<AttendClassDetailByWeek> data;
        string[] courseFilter = new string[]{};
        private void AttendanceAnalysis_ParametersRequestBeforeShow(object sender, DevExpress.XtraReports.Parameters.ParametersRequestEventArgs e)
        {
            //prmYear.Value = TimezoneAdjustment.GetLocalTime().Year;
        }
        private void AttendanceAnalysis_DataSourceDemanded(object sender, EventArgs e)
        {
            int year = (int)prmYear.Value;
            if (year == 0) { year = DbContext.GetLocalTime().Year; }
            xrChart1.Titles[0].Text = $"{year} Attendance Analysis";
            data = BusinessRule.GetClassAttendanceDetailByWeek(DbContext, year);
            objectDataSource1.DataSource = data;
            if (prmCourseFilter.Value != null)
            {
                courseFilter = (string[])prmCourseFilter.Value;
            }
        }

        Guid LastClass { get; set; } = Guid.Empty;
        bool hasPrinted = false;
        private void Detail_BeforePrint(object sender, CancelEventArgs e)
        {
            var ac = (AttendClassDetailByWeek)this.GetCurrentRow();
            if (ac == null || ac.ClassID == LastClass) { e.Cancel = true; return; }

            // Min No of Months report filter
            int minMonths = (int)prmMinMonths.Value;
            if (minMonths > 0)
            {
                int months = data
                    .Where(x => x.ClassID == ac.ClassID)
                    .GroupBy(x => new { x.WeekEnd.Year, x.WeekEnd.Month }).Count();
                if (months < minMonths) { e.Cancel = true; return; }
            }

            // Course filter for individual leader report
            Guid? thisCourseID = (this.prmCourseID.Value != null) ? (Guid)this.prmCourseID.Value : null;
            if (thisCourseID.HasValue && thisCourseID != ac.CourseID) { e.Cancel = true; return; }

            // Course selection filter
            if (courseFilter.Length > 0 && !courseFilter.Contains(ac.CourseID.ToString())) { e.Cancel = true; return; }

            LastClass = ac.ClassID;
            hasPrinted = true;
            string courseIDFilter = ac.CourseID.ToString();
            if (thisCourseID != null) { courseIDFilter = thisCourseID.ToString(); }
            lblTitle.Text = $"{ac.CourseDescription}{Environment.NewLine}Course Type: {ac.CourseTypeDescription}";
            var occurrenceFilter = $"OccurrenceTypeID == {(int)OccurrenceType.Weekly}";
            // All U3A
            xrChart1.Series[0].FilterString = occurrenceFilter;
            // All CourseType
            xrChart1.Series[1].FilterString = $"CourseTypeID = '{ac.CourseTypeID}' and {occurrenceFilter}";
            xrChart1.Series[1].Name = $"All {ac.CourseTypeDescription}: Present";
            // The Course
            xrChart1.Series[2].FilterString = $"CourseID = '{courseIDFilter}'";    // Present
            xrChart1.Series[3].FilterString = $"CourseID = '{courseIDFilter}'";    // Absent with
            xrChart1.Series[4].FilterString = $"CourseID = '{courseIDFilter}'";    // Absent without
        }

        private void ReportFooter_BeforePrint(object sender, CancelEventArgs e)
        {
            e.Cancel = hasPrinted;
        }

    }
}
