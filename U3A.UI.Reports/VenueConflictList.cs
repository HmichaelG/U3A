using DevExpress.Blazor;
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
    public partial class VenueConflictList : DevExpress.XtraReports.UI.XtraReport, IXtraReportWithDbContext
    {
        public VenueConflictList()
        {
            InitializeComponent();
        }

        public U3ADbContext DbContext { get; set; }

        private void VenueConflictList_DataSourceDemanded(object sender, EventArgs e)
        {
            var term = DbContext.Term.Find((Guid)prmTerm.Value); 
            DataSource = BusinessRule.ReportableVenueConflicts(DbContext, term);
        }

        private void VenueConflictList_ParametersRequestBeforeShow(object sender, DevExpress.XtraReports.Parameters.ParametersRequestEventArgs e)
        {
            var term = BusinessRule.CurrentTerm(DbContext);
            var terms = BusinessRule.SelectableTerms(DbContext).Where(x => x.Comparer >= term.Comparer-1).ToList();
            objectDataSource2.DataSource = terms;
            prmTerm.Value = term?.TermSummary;
            prmTerm.Value = term?.ID;

        }

        private void xrSubTitle_BeforePrint(object sender, CancelEventArgs e)
        {
            var term = DbContext.Term.Find((Guid)prmTerm.Value);
            xrSubTitle.Text = $"For {term?.TermSummary}";
        }
    }
}
