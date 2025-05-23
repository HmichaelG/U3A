﻿using DevExpress.Logify;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using U3A.Database;
using U3A.Model;

namespace U3A.Services
{
    public class LoginState
    {
        public bool IsAdminOnBehalfOfMember { get; set; }
        public LoginState(string AdminEmail, Person OnBehalfOfMember, IDbContextFactory<U3ADbContext> dbFactory)
        {
            IsAdminOnBehalfOfMember = true;
            this.AdminEmail = AdminEmail;
            if (!string.IsNullOrWhiteSpace(OnBehalfOfMember.Email))
            {
                GetLinkedMembers(OnBehalfOfMember.Email, OnBehalfOfMember, dbFactory);
            }
            else
            {
                LinkedPeople = new List<Person> { OnBehalfOfMember };
                IsNewMember = false;
                SelectedPerson = OnBehalfOfMember;
            }
        }

        public LoginState(string LoginEmail, IDbContextFactory<U3ADbContext> dbFactory)
        {
            IsAdminOnBehalfOfMember = false;
            AdminEmail = null;
            GetLinkedMembers(LoginEmail, dbFactory);
        }
        private void GetLinkedMembers(string LoginEmail,
                    IDbContextFactory<U3ADbContext> dbFactory)
        {
            GetLinkedMembers(LoginEmail, null, dbFactory);
        }
        private void GetLinkedMembers(string LoginEmail,
                Person OnBehalfOfMember,
                IDbContextFactory<U3ADbContext> dbFactory)
        {
            if (LoginEmail == null) { return; }
            this.LoginEmail = LoginEmail;
            using (var dbc = dbFactory.CreateDbContext())
            {
                LinkedPeople = dbc.Person.Where(x => x.Email == LoginEmail && !x.DateCeased.HasValue).ToList();
                IsNewMember = (LinkedPeople.Count <= 0) ? true : false;
                if (!IsNewMember)
                {
                    if (OnBehalfOfMember != null)
                    {
                        SelectedPerson = OnBehalfOfMember;
                    }
                    else
                    {
                        SelectedPerson = LinkedPeople.FirstOrDefault();
                    }
                }
            }
        }
        public List<Person> LinkedPeopleNotLoggedIn
        {
            get
            {
                if (SelectedPerson != null && LinkedPeople.Count > 0)
                {
                    return LinkedPeople.Where(x => x.ID != SelectedPerson.ID).ToList();
                }
                else { return new List<Person>(); }
            }
        }

        public List<Person> LinkedPeople { get; set; }
        public string? LoginEmail { get; set; }
        public Person? SelectedPerson { get; set; }

        public string? AdminEmail { get; set; }

        public bool IsNewMember { get; set; }


    }
}
