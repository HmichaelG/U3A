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
    public partial class CourseByParticipantList : DevExpress.XtraReports.UI.XtraReport, IXtraReportWithDbContext
    {
        public CourseByParticipantList()
        {
            InitializeComponent();
            prmFinancialStatus.Value = null;
        }

        public U3ADbContext DbContext { get; set; }

        private void CourseByVenueList_ParametersRequestBeforeShow(object sender, DevExpress.XtraReports.Parameters.ParametersRequestEventArgs e)
        {
            var terms = BusinessRule.SelectableTerms(DbContext);
            objectDataSource2.DataSource = terms;
            var term = BusinessRule.CurrentTerm(DbContext);
            paramTermSummary.Value = term?.TermSummary;
            paramTerm.Value = term?.ID;
        }

        private void CourseByVenueList_ParametersRequestSubmit(object sender, DevExpress.XtraReports.Parameters.ParametersRequestEventArgs e)
        {
            var term = DbContext.Term.Find((Guid)paramTerm.Value);
            paramTermSummary.Value = term?.TermSummary;
            DataSource = GetData(term);
            if (paramWaitlistStatus.Value != null)
            {
                paramReportTitle.Value = $"Courses By {(((bool)paramWaitlistStatus.Value) ? "Waitlisted" : "Active")} Participant List";
            }
            else
            {
                paramReportTitle.Value = "Courses By Participant List";
            }
        }

        List<Person> GetData(Term term)
        {
            var result = BusinessRule.SelectablePersonsWithEnrolments(DbContext, term.ID, (bool?)paramWaitlistStatus.Value);
            if (prmFinancialStatus.Value == null) { return result; }
            switch ((int)prmFinancialStatus.Value)
            {
                case 0:
                    //Financial
                    result = result.Where(x => x.FinancialTo >= term.Year).ToList();
                    break;
                case 1:
                    //Unfinancial
                    result = result.Where(x => x.FinancialTo < term.Year).ToList();
                    break;
            }
            return result;
        }

    }
}
