using DevExpress.DataAccess.ObjectBinding;
using DevExpress.XtraReports.UI;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using U3A.BusinessRules;
using U3A.Database;
using U3A.Database.Migrations.U3ADbContextSeedMigrations;
using U3A.Model;
using U3A.Services;

namespace U3A.UI.Reports
{
    public partial class AttendanceByMemberRpt
        : DevExpress.XtraReports.UI.XtraReport, IXtraReportWithDbContextFactory
    {
        public AttendanceByMemberRpt()
        {
            InitializeComponent();
        }

        public IDbContextFactory<U3ADbContext> U3Adbfactory { get; set; }
        private void AttendanceByMemberRpt_ParametersRequestBeforeShow(object sender, DevExpress.XtraReports.Parameters.ParametersRequestEventArgs e)
        {
            List<AttendClass> data = new List<AttendClass>();
            // Poor man's task dispatcher
            Parallel.For(0, 1, (i, state) =>
            {
                using (var dbc = U3Adbfactory.CreateDbContext())
                {
                    prmStartDate.Value = dbc.AttendClass
                        .OrderBy(x => x.Date)
                        .Select(x => x.Date).FirstOrDefault();
                    prmEndDate.Value = dbc.AttendClass
                        .OrderByDescending(x => x.Date)
                        .Select(x => x.Date).FirstOrDefault();
                    objectDataSource2.DataSource = dbc.AttendClassStatus.ToList();
                    objectDataSource3.DataSource = dbc.Person.IgnoreQueryFilters()
                                                    .Where(x => !x.IsDeleted).ToList();
                }
                ;
            });
            objectDataSource1.DataSource = data.ToList();
        }

        private void AttendanceByMemberRpt_ParametersRequestSubmit(object sender, DevExpress.XtraReports.Parameters.ParametersRequestEventArgs e)
        {
            if (prmMembers.Value == null)
            {
                prmMembers.Value = (objectDataSource1.DataSource as List<AttendClass>).Select(x => x.PersonName).Distinct();
            }
            switch ((int)prmStatus.Value)
            {
                case 0:
                    xrReportTitle.Text = "Classes Attended Report";
                    break;
                case 1:
                    xrReportTitle.Text = "Classes Absent Without Apology Report";
                    break;
                case 2:
                    xrReportTitle.Text = "Classes Absent With Apology Report";
                    break;
            }
        }

        private void AttendanceByMemberRpt_DataSourceDemanded(object sender, EventArgs e)
        {
            using (var dbc = U3Adbfactory.CreateDbContext())
            {
                var peopleID = prmMembers.Value as IEnumerable<Guid>;
                objectDataSource1.DataSource = BusinessRule.GetAttendance(dbc, peopleID).ToList();
            }
        }

    }
}
