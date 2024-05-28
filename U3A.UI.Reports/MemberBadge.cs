using DevExpress.Web.Internal;
using DevExpress.XtraReports.UI;
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using U3A.BusinessRules;
using U3A.Database;
using DevExpress.Drawing;
using U3A.Model;

namespace U3A.UI.Reports
{
    public partial class MemberBadge : DevExpress.XtraReports.UI.XtraReport, IXtraReportWithDbContext
    {
        Term term { get; set; }
        public U3ADbContext DbContext { get; set; }

        public MemberBadge()
        {
            InitializeComponent();
        }

        private void MemberBadge_BeforePrint(object sender, CancelEventArgs e)
        {
            if (term == null) { term = BusinessRule.CurrentEnrolmentTerm(DbContext); }
        }
        private void MemberBadge_ParametersRequestBeforeShow(object sender, DevExpress.XtraReports.Parameters.ParametersRequestEventArgs e)
        {
            term = BusinessRule.CurrentEnrolmentTerm(DbContext);
            objectDataSource1.DataSource = GetPeople();
        }

        private void MaillingLabels_DataSourceDemanded(object sender, EventArgs e)
        {
            var settings = DbContext.SystemSettings.FirstOrDefault();
            prmU3AName.Value = settings.U3AGroup;
            var persons = GetPeople();
            if (prmPersonID.Value != null)
            {
                persons = persons
                            .Where(x => (prmPersonID.Value as IEnumerable<Guid>).Contains(x.ID)).ToList();
            }
            if (prmStartDate.Value != null)
            {
                persons = persons.Where(x => x.DateJoined >= (DateTime)prmStartDate.Value).ToList();
            }
            DataSource = persons;
        }

        List<Person> GetPeople()
        {
            Task<List<Person>> syncTask = Task.Run(async () =>
            {
                return await BusinessRule.SelectableFinancialPeopleAsync(DbContext);
            });
            syncTask.Wait();
            return syncTask.Result;
        }

        private void xrTitle_BeforePrint(object sender, CancelEventArgs e)
        {
            var person = (Person)this.GetCurrentRow();
            xrTitle.Text = "";
            if (person == null) { return; }
            if (person.FinancialTo == term.Year) { xrTitle.Text = $"Member {term.Year}"; }
            if (person.IsCourseClerk) { xrTitle.Text = $"Course Clerk {term.Year}"; }
            if (person.IsCourseLeader) { xrTitle.Text = $"Course Leader {term.Year}"; }
            if (person.IsCommitteeMember) { xrTitle.Text = $"Committeee Member {term.Year}"; }
            if (person.IsLifeMember) { xrTitle.Text = "Life Member"; }
        }

    }
}
