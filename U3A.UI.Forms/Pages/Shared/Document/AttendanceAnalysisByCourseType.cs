using DevExpress.Web.Internal;
using DevExpress.XtraReports.UI;
using System;
using System.Collections;
using System.ComponentModel;
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
        private void AttendanceAnalysis_DataSourceDemanded(object sender, EventArgs e)
        {
            objectDataSource1.DataSource = BusinessRule.GetClassAttendanceDetailByWeek(DbContext);
        }

        Guid LastClass { get; set; } = Guid.Empty;
        bool hasPrinted = false;
        private void Detail_BeforePrint(object sender, CancelEventArgs e)
        {
            var ac = (AttendClassDetailByWeek)this.GetCurrentRow();
            if (ac.ClassID == LastClass) { e.Cancel = true; return; }
            Guid? prmCourseID = (this.prmCourseID.Value != null) ? (Guid)this.prmCourseID.Value : null;
            if (prmCourseID.HasValue && prmCourseID != ac.CourseID) { e.Cancel = true; return; }
            if (ac != null)
            {
                LastClass = ac.ClassID;
                hasPrinted = true;
                string courseIDFilter = ac.CourseID.ToString();
                if (prmCourseID != null) { courseIDFilter = prmCourseID.ToString(); }
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
        }

        private void ReportFooter_BeforePrint(object sender, CancelEventArgs e)
        {
            e.Cancel = hasPrinted;
        }
    }
}
