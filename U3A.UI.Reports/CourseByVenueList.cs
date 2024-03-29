﻿using DevExpress.XtraReports.UI;
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using U3A.BusinessRules;
using U3A.Database;

namespace U3A.UI.Reports
{
    public partial class CourseByVenueList : DevExpress.XtraReports.UI.XtraReport, IXtraReportWithDbContext
    {
        public CourseByVenueList()
        {
            InitializeComponent();
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
            DataSource = BusinessRule.SelectableVenues(DbContext, term.ID);
        }
    }
}
