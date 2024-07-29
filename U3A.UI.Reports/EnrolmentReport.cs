using DevExpress.ClipboardSource.SpreadsheetML;
using DevExpress.Web;
using DevExpress.XtraReports.UI;
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Reactive.Threading.Tasks;
using U3A.BusinessRules;
using U3A.Database;
using U3A.Model;

namespace U3A.UI.Reports;

public partial class EnrolmentReport : DevExpress.XtraReports.UI.XtraReport, IXtraReportWithDbContext
{
    public EnrolmentReport()
    {
        InitializeComponent();
    }

    public U3ADbContext DbContext { get; set; }

    List<Course> Courses;
    private void CourseList_ParametersRequestSubmit(object sender, DevExpress.XtraReports.Parameters.ParametersRequestEventArgs e)
    {
        Guid id = (Guid)paramCourseYear.Value;
        var term = DbContext.Term.Find(id);
        if (term != null)
        {
            Courses = BusinessRule.SelectableCourses(DbContext, term);
            var data = BusinessRule.SelectableEnrolments(DbContext, term);
            int FinToYear = term.Year;
            switch ((int)prmEnrolmentStatus.Value)
            {
                case 1:
                    data = data.Where(x => !x.IsWaitlisted).ToList();
                    paramReportTitle.Value = "Active Student Enrolment Report";
                    data = AddFinancialStatus(true, FinToYear, data);
                    break;
                case 2:
                    data = data.Where(x => x.IsWaitlisted).ToList();
                    paramReportTitle.Value = "Waitlisted Student Enrolment Report";
                    data = AddFinancialStatus(true, FinToYear, data);
                    break;
                default:
                    paramReportTitle.Value = "Student Enrolment Report";
                    data = AddFinancialStatus(false, FinToYear, data);
                    break;
            }
            objectDataSource1.DataSource = data;
            paramTermSummary.Value = term?.TermSummary;
            paramCourseYear.Value = term?.ID;
        }
    }

    List<Enrolment> AddFinancialStatus(bool AddAmpersand, int FinTo, List<Enrolment> Data)
    {
        int financialStatus = (int)prmFinancialStatus.Value;
        string ampersand = (AddAmpersand) ? " &" : "";
        switch (financialStatus)
        {
            case 1: //financial
                paramReportTitle.Value = $"Financial{ampersand} " + (string)paramReportTitle.Value;
                Data = Data.Where(x => x.Person.FinancialTo >= FinTo).ToList();
                break;
            case 2: //not financial
                paramReportTitle.Value = $"Not Financial{ampersand} " + (string)paramReportTitle.Value;
                Data = Data.Where(x => x.Person.FinancialTo < FinTo).ToList();
                break;
        }
        return Data;
    }

    private void CourseList_ParametersRequestBeforeShow(object sender, DevExpress.XtraReports.Parameters.ParametersRequestEventArgs e)
    {
        var terms = BusinessRule.SelectableTerms(DbContext);
        objectDataSource2.DataSource = terms;
        var term = BusinessRule.CurrentTerm(DbContext);
        paramCourseYear.Value = term?.ID;
    }

    private void xrReportTotalMax_BeforePrint(object sender, CancelEventArgs e)
    {
        xrReportTotalMax.Text = Courses.Sum(x => x.MaximumStudents).ToString("n0");
    }

    private void xrReportTotalMin_BeforePrint(object sender, CancelEventArgs e)
    {
        xrReportTotalMin.Text = Courses.Sum(x => x.RequiredStudents).ToString("n0");
    }

    private void xrActiveStudents_BeforePrint(object sender, CancelEventArgs e)
    {
        xrActiveStudents.Text = "";
        var row = (Enrolment)GetCurrentRow();
        if (row != null)
        {
            xrActiveStudents.Text = (row.Class == null)
                ? CourseActiveStudents(row.CourseID).ToString()
                : row.Class.TotalActiveStudents.ToString();
        }
    }

    private void xrWaitlistStudents_BeforePrint(object sender, CancelEventArgs e)
    {
        xrWaitlistStudents.Text = "";
        var row = (Enrolment)GetCurrentRow();
        if (row != null)
        {
            xrWaitlistStudents.Text = (row.Class == null)
                ? CourseWaitlistStudents(row.CourseID).ToString()
                : row.Class.TotalWaitlistedStudents.ToString();
        }
    }

    private void xrTotalStudents_BeforePrint(object sender, CancelEventArgs e)
    {
        xrTotalStudents.Text = "";
        var row = (Enrolment)GetCurrentRow();
        if (row != null)
        {
            var total = 0;
            if (row.Class == null)
            {
                total = CourseActiveStudents(row.CourseID) + CourseWaitlistStudents(row.CourseID);
            }
            else { total = row.Class.TotalActiveStudents + row.Class.TotalWaitlistedStudents; }
            xrTotalStudents.Text = total.ToString();
        }
    }
    int CourseActiveStudents(Guid CourseID)
    {
        int result = 0;
        var course = Courses.Find(x => x.ID == CourseID);
        if (course != null && course.Classes.Count > 0)
        {
            result = course.Classes[0].TotalActiveStudents;
        }
        return result;
    }
    int CourseWaitlistStudents(Guid CourseID)
    {
        int result = 0;
        var course = Courses.Find(x => x.ID == CourseID);
        if (course != null && course.Classes.Count > 0)
        {
            result = course.Classes[0].TotalWaitlistedStudents;
        }
        return result;
    }
}
