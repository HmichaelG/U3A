using DevExpress.Blazor;
using DevExpress.XtraRichEdit.Commands.Internal;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text;
using U3A.Database;
using U3A.Model;
using U3A.Services;
using AngleSharp.Dom;
using AngleSharp.Html;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using static Azure.Core.HttpHeader;

namespace U3A.BusinessRules
{
    public static partial class BusinessRule
    {
        public static async Task<AIChatClassData> GetJsonAIChatClassDataAsync(U3ADbContext dbc,
                                                        TenantDbContext dbcT,
                                                        TenantInfoService tenantService)
        {
            var people = await dbc.Person.ToListAsync();    
            var data = new AIChatClassData();
            // settings
            var settings = await dbc.SystemSettings.OrderBy(x => x.ID).FirstOrDefaultAsync();

            // Terms
            data.Terms = await BusinessRule.SelectableTermsInCurrentYearAsync(dbc);

            // Classes
            var term = await BusinessRule.CurrentEnrolmentTermAsync(dbc);
            if (term == null)
            {
                var currentTerm = await BusinessRule.CurrentTermAsync(dbc);
            }
            if (term != null)
            {

                var prevTerm = await BusinessRule.GetPreviousTermAsync(dbc, term.Year, term.TermNumber) ?? term;

                // Fast lookup from Schedule cache
                var classes = await BusinessRule.RestoreClassesFromScheduleAsync(dbc, dbcT, tenantService, term, settings, true, true);
                // Get the schedule
                await getAppointmentsForAiClass(dbc, term, classes);
                // Transpose to ScheduledClasses
                data.Classes = classes.Select(x => new ScheduledClass()
                {
                    ID = x.ID,

                    // From Course

                    Name = x.Course.Name,
                    Description = RemoveHtmlTags( x.Course.Description),
                    CourseParticipationType = (x.Course.CourseParticipationTypeID == 0)
                                                ? "Same students in all classes"
                                                : "Different students in each class",
                    Featured = x.Course.IsFeaturedCourse,
                    EnforceOneClassPerStudent = x.Course.EnforceOneStudentPerClass,
                    FeePerYear = x.Course.CourseFeePerYear,
                    FeePerYearDescription = x.Course.CourseFeePerYearDescription,
                    FeePerTerm = x.Course.CourseFeePerTerm,
                    FeePerTermDescription = x.Course.CourseFeePerTermDescription,
                    Duration = x.Course.Duration,
                    RequiredStudents = x.Course.RequiredStudents,
                    MaximumStudents = x.Course.MaximumStudents,
                    AllowAutoEnroll = x.Course.AllowAutoEnrol,
                    Type = x.Course.CourseType.Name,
                    ProvidedBy = (x.Course.OfferedBy == null)
                                ? dbc.TenantInfo.Name : x.Course.OfferedBy,

                    // From Class

                    OfferedTerm1 = x.OfferedTerm1,
                    OfferedTerm2 = x.OfferedTerm2,
                    OfferedTerm3 = x.OfferedTerm3,
                    OfferedTerm4 = x.OfferedTerm4,
                    StartDate = (x.StartDate != null)
                                    ? DateOnly.FromDateTime(x.StartDate.Value)
                                    : DateOnly.FromDateTime(x.ClassDates.OrderBy(x => x).FirstOrDefault()),
                    StartTime = TimeOnly.FromDateTime(x.StartTime),
                    EndTime = (x.EndTime != null) ? TimeOnly.FromDateTime(x.EndTime.Value)
                                                  : null,
                    Occurs = x.Occurrence.Name,
                    Repeats = x.Recurrence  ?? term.Duration ,
                    Day = x.OnDay.Day,
                    Venue = x.Venue.Name,
                    VenueAddress = x.Venue.Address,
                    ClassSummary = x.OccurrenceText,
                    TotalActiveStudents = x.TotalActiveStudents,
                    TotalWaitlisted = x.TotalWaitlistedStudents,
                    ParticipationRate = x.ParticipationRate

                }).ToList();

                classes.ForEach(delegate (Class c)
                {
                    var sc = data.Classes.Find(x => x.ID == c.ID);
                    if (sc != null)
                    {
                        if (!string.IsNullOrWhiteSpace(c.GuestLeader)) sc.People.Add(new ScheduledPerson()
                        {
                            Class = c.Course.Name,
                            SortOrder = c.GuestLeader,
                            Name = c.GuestLeader,
                            Role = "Guest Leader"
                        });
                        if (c.Leader != null) sc.People.Add(new ScheduledPerson()
                        {
                            Class = c.Course.Name,
                            SortOrder = c.Leader.FullNameAlphaKey,
                            Name = c.Leader.FullNameWithPostNominals,
                            Email = c.Leader.Email,
                            Phone = c.Leader.AdjustedHomePhone,
                            Mobile = c.Leader.AdjustedMobile,
                            Role = "Leader"
                        });
                        if (c.Leader2 != null) sc.People.Add(new ScheduledPerson()
                        {
                            Class = c.Course.Name,
                            SortOrder = c.Leader2.FullNameAlphaKey,
                            Name = c.Leader2.FullNameWithPostNominals,
                            Email = c.Leader2.Email,
                            Phone = c.Leader2.AdjustedHomePhone,
                            Mobile = c.Leader2.AdjustedMobile,
                            Role = "Leader"
                        });
                        if (c.Leader3 != null) sc.People.Add(new ScheduledPerson()
                        {
                            Class = c.Course.Name,
                            SortOrder = c.Leader3.FullNameAlphaKey,
                            Name = c.Leader3.FullNameWithPostNominals,
                            Email = c.Leader3.Email,
                            Phone = c.Leader3.AdjustedHomePhone,
                            Mobile = c.Leader3.AdjustedMobile,
                            Role = "Leader"

                        });
                        foreach (var clerk in c.Clerks)
                        {
                            sc.People.Add(new ScheduledPerson()
                            {
                                Class = c.Course.Name,
                                SortOrder = clerk.FullNameAlphaKey,
                                Name = clerk.FullNameWithPostNominals,
                                Email = clerk.Email,
                                Phone = clerk.AdjustedHomePhone,
                                Mobile = clerk.AdjustedMobile,
                                Role = "Clerk"
                            });
                        }
                        foreach (var e in c.Course.Enrolments)
                        {
                            if (e.isLeader) continue;
                            if (e.Person == null) { e.Person = people.Find(x => x.ID == e.PersonID); }
                            sc.People.Add(new ScheduledPerson()
                            {
                                Class = c.Course.Name,
                                SortOrder = e.Person.FullNameAlphaKey,
                                Name = e.Person.FullNameWithPostNominals,
                                Email = e.Person.Email,
                                Phone = e.Person.AdjustedHomePhone,
                                Mobile = e.Person.AdjustedMobile,
                                Role = (e.IsWaitlisted) ? "Waitlisted" : "Student"
                            });
                        }
                        foreach (var e in c.Enrolments)
                        {
                            if (e.isLeader) continue;
                            if (e.Person == null) { e.Person = people.Find(x => x.ID == e.PersonID); }
                            sc.People.Add(new ScheduledPerson()
                            {
                                Class = c.Course.Name,
                                SortOrder = e.Person.FullNameAlphaKey,
                                Name = e.Person.FullNameWithPostNominals,
                                Email = e.Person.Email,
                                Phone = e.Person.AdjustedHomePhone,
                                Mobile = e.Person.AdjustedMobile,
                                Role = (e.IsWaitlisted) ? "Waitlisted" : "Student"
                            });
                        }
                        sc.People = sc.People.OrderBy(x => x.SortOrder).ToList();
                    }
                });
            }
            return data;
        }

        static IHtmlDocument? ParseHtml(string html)
        {
            IHtmlDocument result = null!;
            var parser = new HtmlParser();
            try
            {
                result = parser.ParseDocument(html);
            }
            catch (Exception e) { }
            return result;
        }

        static string RemoveHtmlTags(string html)
        {
            var result = string.Empty;
            using (var document = ParseHtml(html))
            {
                result = document.Body.TextContent;
            }
            return result;
        }
        static async Task getAppointmentsForAiClass(U3ADbContext dbc, Term term,
            IEnumerable<Class> classes)
        {
            var dataStorage = await BusinessRule.GetCalendarDataStorageAsync(dbc, term);
            var range = new DxSchedulerDateTimeRange(term.StartDate, new DateTime(term.Year, 12, 31));
            Dictionary<Guid, List<DxSchedulerAppointmentItem>> classAppointments = new();
            foreach (var a in dataStorage?.GetAppointments(range))
            {
                Class c = (Class)a.CustomFields["Source"];
                if (c != null && (int)a.LabelId != 9)
                {
                    if (!classAppointments.ContainsKey(c.ID))
                    {
                        classAppointments.Add(c.ID, new List<DxSchedulerAppointmentItem>());
                    }
                    classAppointments[c.ID].Add(a);
                }
            }
            foreach (Class c in classes)
            {
                if (classAppointments.ContainsKey(c.ID))
                {
                    foreach (var a in classAppointments[c.ID])
                    {
                        c.ClassDates.Add(a.Start);
                    }
                }
            }

        }


    }
}
