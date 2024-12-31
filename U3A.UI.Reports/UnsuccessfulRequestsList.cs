using DevExpress.XtraReports.UI;
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using U3A.BusinessRules;
using U3A.Database;
using U3A.Model;

namespace U3A.UI.Reports
{
    public partial class UnsuccessfulRequestsList : DevExpress.XtraReports.UI.XtraReport, IXtraReportWithDbContext
    {
        public UnsuccessfulRequestsList()
        {
            InitializeComponent();
            prmFinancialStatus.Value = null;
        }

        public U3ADbContext DbContext { get; set; }

        private void CourseByParticipantList_ParametersRequestBeforeShow(object sender, DevExpress.XtraReports.Parameters.ParametersRequestEventArgs e)
        {
            var terms = BusinessRule.SelectableTerms(DbContext);
            objectDataSource2.DataSource = terms;
            var term = BusinessRule.CurrentTerm(DbContext);
            paramTermSummary.Value = term?.TermSummary;
            paramTerm.Value = term?.ID;
            objectDataSource3.DataSource = BusinessRule.SelectableCourses(DbContext, term);
        }

        private void CourseByParticipantList_ParametersRequestValueChanged(object sender, DevExpress.XtraReports.Parameters.ParametersRequestValueChangedEventArgs e)
        {
            if (e.ChangedParameterInfo.Parameter == paramTerm)
            {
                var term = DbContext.Term.Find((Guid)paramTerm.Value);
                objectDataSource3.DataSource = BusinessRule.SelectableCourses(DbContext, term);
            }
        }
        private void CourseByParticipantList_ParametersRequestSubmit(object sender, DevExpress.XtraReports.Parameters.ParametersRequestEventArgs e)
        {
            var term = DbContext.Term.Find((Guid)paramTerm.Value);
            paramTermSummary.Value = term?.TermSummary;
            DataSource = GetData(term);
        }

        List<Person> GetData(Term term)
        {
            var result = BusinessRule.SelectablePersonsWithEnrolments(DbContext, term.ID, null);
            result = result.Where(x => !x.Enrolments.Any(c => !c.IsWaitlisted)).ToList();
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
