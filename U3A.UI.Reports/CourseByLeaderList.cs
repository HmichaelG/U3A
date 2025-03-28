﻿using DevExpress.XtraReports.UI;
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using U3A.BusinessRules;
using U3A.Database;
using U3A.Model;

namespace U3A.UI.Reports
{
    public partial class CourseByLeaderList : DevExpress.XtraReports.UI.XtraReport, IXtraReportWithDbContext
    {
        public CourseByLeaderList()
        {
            InitializeComponent();
        }

        public U3ADbContext DbContext { get; set; }

        private void CourseByLeaderList_ParametersRequestBeforeShow(object sender, DevExpress.XtraReports.Parameters.ParametersRequestEventArgs e)
        {
            var settings = DbContext.SystemSettings.OrderBy(x => x.ID).FirstOrDefault();
            prmLeaderType.Value = (settings?.SendLeaderReportsTo == SendLeaderReportsTo.LeadersThenClerks)
                                        ? "Leader" : "Clerk";
            var terms = BusinessRule.SelectableTerms(DbContext);
            objectDataSource2.DataSource = terms;
            var term = BusinessRule.CurrentTerm(DbContext);
            paramTermSummary.Value = term?.TermSummary;
            paramTerm.Value = term?.ID;
        }

        private void CourseByLeaderList_ParametersRequestSubmit(object sender, DevExpress.XtraReports.Parameters.ParametersRequestEventArgs e)
        {
            var term = DbContext.Term.Find((Guid)paramTerm.Value);
            DataSource = (prmLeaderType.Value == "Leader")
                ? BusinessRule.SelectableLeaders(DbContext, term.ID)
                : BusinessRule.SelectableClerks(DbContext, term.ID);
            paramTermSummary.Value = term?.TermSummary;
        }
    }
}
