﻿using DevExpress.Blazor;
using DevExpress.Blazor.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using U3A.Database;
using U3A.Model;

namespace U3A.BusinessRules
{
    public static partial class BusinessRule
    {
        public static async Task CreateReceiptSendMailAsync(U3ADbContext dbc)
        {
            var list = new List<Receipt>();
            var entries = dbc.ChangeTracker.Entries<Receipt>();
            foreach (var entry in entries)
                if (entry.Entity is Receipt r)
                {
                    if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
                    {
                        list.Add(r);
                    }
                }
            foreach (var r in list)
            {
                var mail = new SendMail()
                {
                    DocumentName = "Cash Receipt",
                    Person = await dbc.Person.FindAsync(r.PersonID),
                    RecordKey = r.ID
                };
                await dbc.AddAsync(mail);
            }
        }
        public static async Task CreateEnrolmentSendMailAsync(U3ADbContext dbc, DateTime? AsAt = null)
        {
            var list = new List<Enrolment>();
            var entries = dbc.ChangeTracker.Entries<Enrolment>();
            foreach (var entry in entries)
                if (entry.Entity is Enrolment r)
                {
                    if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
                    {
                        list.Add(r);
                    }
                }
            await DoParticipantEnrolmentAsync(dbc, list.ToArray(), AsAt);
            await DoLeaderEnrolmentAsync(dbc, list.ToArray());
        }

        private static async Task DoParticipantEnrolmentAsync(U3ADbContext dbc, Enrolment[] enrolments, DateTime? AsAt)
        {
            var settings = await dbc.SystemSettings.OrderBy(x => x.ID).FirstOrDefaultAsync();
            var reportName = "Participant Enrolment";
            foreach (var e in enrolments)
            {
                if (!(await dbc.SendMail.AnyAsync(x => x.RecordKey == e.ID
                            && x.Person.ID == e.Person.ID
                            && x.DocumentName == reportName
                            && string.IsNullOrWhiteSpace(x.Status))))
                {
                    var mail = new SendMail()
                    {
                        DocumentName = reportName,
                        Person = await dbc.Person.FindAsync(e.PersonID),
                        RecordKey = e.ID,
                        CreatedOn = AsAt,
                    };
                    await dbc.AddAsync(mail);
                }
            }
        }
        private static async Task DoLeaderEnrolmentAsync(U3ADbContext dbc, Enrolment[] enrolments)
        {
            var reportName = "Leader Report";
            var keys = new List<string>();
            var s = await dbc.SystemSettings.OrderBy(x => x.ID).FirstOrDefaultAsync();
            foreach (var e in enrolments.Where(x => !x.IsWaitlisted))
            {
                var t = await dbc.Term.FindAsync(e.TermID);
                if (e.ClassID != null)
                {
                    // Different participants in each class
                    var c = await dbc.Class.FindAsync(e.ClassID);
                    foreach (var p in await BusinessRule.GetLeaderReportRecipients(dbc, s, t, c))
                    {
                        if (!(await dbc.SendMail.AnyAsync(x => x.RecordKey == c.ID           // Record key is the classID
                                    && x.TermID == e.TermID
                                    && x.PersonID == p.ID
                                    && x.DocumentName == reportName
                                    && string.IsNullOrWhiteSpace(x.Status))))
                        {
                            var key = p.ID.ToString() + c.ID.ToString();
                            if (!keys.Contains(key))
                            {
                                var mail = new SendMail()
                                {
                                    DocumentName = reportName,
                                    Person = p,
                                    RecordKey = c.ID,
                                    TermID = e.TermID
                                };
                                await dbc.AddAsync(mail);
                                keys.Add(key);
                            }
                        }
                    }
                }
                else
                {
                    // Same participants in all classes.
                    // Each class can have a different leader, so make sure we get them all
                    var classes = dbc.Class.Where(x => x.CourseID == e.CourseID).ToList();
                    foreach (var c in classes)
                    {
                        foreach (var p in await BusinessRule.GetLeaderReportRecipients(dbc, s, t, c))
                        {
                            if (!(await dbc.SendMail.Include(x => x.Person)
                                    .Where(x => x.RecordKey == e.CourseID       //Record key is the CourseID
                                        && x.TermID == e.TermID
                                        && x.Person.ID == p.ID
                                        && x.DocumentName == reportName
                                        && string.IsNullOrWhiteSpace(x.Status)).AnyAsync()))
                            {
                                var key = p.ID.ToString() + e.CourseID.ToString();
                                if (!keys.Contains(key))
                                {
                                    var mail = new SendMail()
                                    {
                                        DocumentName = reportName,
                                        Person = p,
                                        RecordKey = e.CourseID,
                                        TermID = e.TermID
                                    };
                                    await dbc.AddAsync(mail);
                                    keys.Add(key);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}