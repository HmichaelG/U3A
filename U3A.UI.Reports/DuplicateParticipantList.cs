using DevExpress.XtraReports.UI;
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using U3A.BusinessRules;
using U3A.Database;

namespace U3A.UI.Reports
{
    public partial class DuplicateParticipantList : DevExpress.XtraReports.UI.XtraReport, IXtraReportWithDbContext

    {
        public DuplicateParticipantList()
        {
            InitializeComponent();
        }

        public U3ADbContext DbContext { get; set; }

        private void DuplicateParticipantList_DataSourceDemanded(object sender, EventArgs e)
        {
            DataSource = BusinessRule.ReportableDuplicatePersons(DbContext);
        }
    }
}
