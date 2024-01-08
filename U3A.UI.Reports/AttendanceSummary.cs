using DevExpress.XtraReports.UI;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using U3A.BusinessRules;
using U3A.Database;

namespace U3A.UI.Reports
{
    public partial class AttendanceSummary : DevExpress.XtraReports.UI.XtraReport, IXtraReportWithDbContextFactory
    {
        public AttendanceSummary()
        {
            InitializeComponent();
        }

        public IDbContextFactory<U3ADbContext> U3Adbfactory { get; set; }

        private void AttendanceSummary_DataSourceDemanded(object sender, EventArgs e)
        {
            using (var dbc = U3Adbfactory.CreateDbContext())
            {
                objectDataSource1.DataSource = BusinessRule.GetAttendanceSummary(dbc, (int)prmYear.Value).ToList();
            }
        }

        private void AttendanceSummary_ParametersRequestBeforeShow(object sender, DevExpress.XtraReports.Parameters.ParametersRequestEventArgs e)
        {
            using (var dbc = U3Adbfactory.CreateDbContext())
            {
                objectDataSource2.DataSource = BusinessRule.SelectableTerms(dbc).ToList();
                var currentTerm = BusinessRule.CurrentTerm(dbc);
                prmYear.Value = currentTerm.Year;
            }
        }

    }
}
