using DevExpress.CodeParser;
using DevExpress.Web.Internal;
using DevExpress.XtraReports.UI;
using Microsoft.EntityFrameworkCore;
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
    public partial class AttendanceAnalysis
        : DevExpress.XtraReports.UI.XtraReport, IXtraReportWithDbContextFactory, IXtraReportWithDbContext
    {
        public AttendanceAnalysis()
        {
            InitializeComponent();
        }
        public IDbContextFactory<U3ADbContext> U3Adbfactory { get; set; }
        public U3ADbContext DbContext { get; set; }

        List<AttendClassDetailByWeek> data;
        string[] courseFilter = new string[] { };
        private void AttendanceAnalysis_ParametersRequestBeforeShow(object sender, DevExpress.XtraReports.Parameters.ParametersRequestEventArgs e)
        {
            using (var DbContext = U3Adbfactory.CreateDbContext())
            {
                if (prmYear.Value == null || (int)prmYear.Value <= 0)
                {
                    prmYear.Value = DbContext.GetLocalTime().Year;
                }
            }
        }
        private void AttendanceAnalysis_DataSourceDemanded(object sender, EventArgs e)
        {
            if (U3Adbfactory != null)
            {
                using (DbContext = U3Adbfactory.CreateDbContext())
                {
                    ProcessReport(DbContext);
                }
            }
            else if (DbContext != null)
            {
                ProcessReport(DbContext);
            }
            else
            {
                throw new NullReferenceException("DbContext or U3AdbFactory must be set");
            }
        }

        private void ProcessReport(U3ADbContext DbContext)
        {
            int year = (int)prmYear.Value;
            if (year == 0) { year = DbContext.GetLocalTime().Year; }
            xrChart1.Titles[0].Text = $"{year} Attendance Analysis";
            data = BusinessRule.GetClassAttendanceDetailByWeek(DbContext, year);
            if (prmCourseFilter.Value != null)
            {
                courseFilter = (string[])prmCourseFilter.Value;
                data = data.Where(x => courseFilter.Contains(x.CourseID.ToString())).ToList();
            }
            objectDataSource1.DataSource = data.OrderBy(x => x.CourseDescription).ThenBy(x => x.CourseID).ThenBy(x => x.ClassID);
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
            xrChart1.Series[2].FilterString = $"CourseID = '{courseIDFilter}' AND ClassID = '{LastClass}'";    // Present
            xrChart1.Series[3].FilterString = $"CourseID = '{courseIDFilter}' AND ClassID = '{LastClass}'";    // Absent with
            xrChart1.Series[4].FilterString = $"CourseID = '{courseIDFilter}' AND ClassID = '{LastClass}'";    // Absent without
        }

        private void ReportFooter_BeforePrint(object sender, CancelEventArgs e)
        {
            e.Cancel = hasPrinted;
        }

    }
}
