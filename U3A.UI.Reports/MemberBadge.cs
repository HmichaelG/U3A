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
using DevExpress.Data.Browsing.Design;
using Microsoft.EntityFrameworkCore;

namespace U3A.UI.Reports
{
    public partial class MemberBadge 
        : DevExpress.XtraReports.UI.XtraReport, IXtraReportWithDbContextFactory
    {
       
        public IDbContextFactory<U3ADbContext> U3Adbfactory { get; set; }

        Term _term { get; set; }
        Guid? _personID;
        List<Guid> _personsID;
        List<Person> _people;
        SystemSettings _settings;

        public MemberBadge()
        {
            InitializeComponent();
        }

        private void MemberBadge_BeforePrint(object sender, CancelEventArgs e)
        {
            if (DataSource == null || (DataSource as IEnumerable<Person>).Count() == 0)
            { e.Cancel = true; return; }
            else
            {
                if (_term == null)
                {
                    using (var dbc = U3Adbfactory.CreateDbContext())
                    {
                        _term = BusinessRule.CurrentEnrolmentTerm(dbc);
                    }
                }
            }
        }
        private void MemberBadge_ParametersRequestBeforeShow(object sender, DevExpress.XtraReports.Parameters.ParametersRequestEventArgs e)
        {
            GetData();
            objectDataSource1.DataSource = GetPeople();
        }

        public void SetParameters(Guid PersonID)
        {
            this.Parameters.Clear();
            _personID = PersonID;
            GetData();
        }
        public void SetParameters(List<Guid> PersonsID)
        {
            this.Parameters.Clear();
            _personsID = PersonsID;
            GetData();
        }
        public void SetParameters(List<Person> People, SystemSettings settings, Term CurrentTerm)
        {
            this.Parameters.Clear();
            _settings = settings;
            _people = People;
            _term = CurrentTerm;
        }
        private void GetData()
        {
            using (var dbc = U3Adbfactory.CreateDbContext())
            {
                _term = BusinessRule.CurrentEnrolmentTerm(dbc);
            }
        }

        private void MaillingLabels_DataSourceDemanded(object sender, EventArgs e)
        {
            if (_people is not null)
            {
                DataSource = _people;
                return;
            }
            using (var dbc = U3Adbfactory.CreateDbContext())
            {
                _settings = dbc.SystemSettings.FirstOrDefault();
            }
            List<Person> persons;
            if (_personID.HasValue)
            {
                persons = GetPerson();
            }
            else
            {
                persons = GetPeople(); ;
                if (prmPersonID.Value != null)
                {
                    persons = persons
                                .Where(x => (prmPersonID.Value as IEnumerable<Guid>).Contains(x.ID)).ToList();
                }
                if (prmStartDate.Value != null)
                {
                    persons = persons.Where(x => x.DateJoined >= (DateTime)prmStartDate.Value).ToList();
                }
            }
            DataSource = persons;
        }

        List<Person> GetPeople()
        {
            Task<List<Person>> syncTask = Task.Run(async () =>
            {
                using (var dbc = U3Adbfactory.CreateDbContext())
                {
                    var people = await BusinessRule.SelectableFinancialPeopleAsync(dbc);
                    if (_personsID is not null)
                    {
                        people = people.Where(x => _personsID.Contains(x.ID)).ToList();
                    }
                    return people;
                }
            });
            syncTask.Wait();
            return syncTask.Result;
        }
        List<Person> GetPerson()
        {
            Task<List<Person>> syncTask = Task.Run(async () =>
            {
                using (var dbc = U3Adbfactory.CreateDbContext())
                {
                    return await BusinessRule.SelectableFinancialPersonAsync(dbc, _personID.Value);
                }
            });
            syncTask.Wait();
            return syncTask.Result;
        }

        private void xrTitle_BeforePrint(object sender, CancelEventArgs e)
        {
            var person = (Person)this.GetCurrentRow();
            xrTitle.Text = "";
            if (person == null) { return; }
            if (person.FinancialTo == _term.Year) { xrTitle.Text = $"Member {_term.Year}"; }
            if (person.IsCourseClerk) { xrTitle.Text = $"Course Clerk {_term.Year}"; }
            if (person.IsCourseLeader) { xrTitle.Text = $"Course Leader {_term.Year}"; }
            if (person.IsCommitteeMember) { xrTitle.Text = $"Committee Member {_term.Year}"; }
            if (person.IsLifeMember) { xrTitle.Text = "Life Member"; }
        }

        private void xrU3AName_BeforePrint(object sender, CancelEventArgs e)
        {
            xrU3AName.Text = _settings.U3AGroup;
        }

    }
}
