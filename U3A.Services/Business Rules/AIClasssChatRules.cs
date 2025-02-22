using DevExpress.Blazor;
using DevExpress.XtraRichEdit.Commands.Internal;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text;
using U3A.Database;
using U3A.Model;
using U3A.Services;
using static Azure.Core.HttpHeader;

namespace U3A.BusinessRules
{
    public static partial class BusinessRule
    {
        public static async Task<AIChatClassData> GetAIChatClassDataAsync(U3ADbContext dbc,
                                                        TenantDbContext dbcT,
                                                        TenantInfoService tenantService)
        {
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
               // classes = classes.Take(102).ToList();
                await getAppointmentsForAiClass(dbc, term, classes);
                // Transpose to ScheduledClasses
                data.Classes = classes.Select(x => new ScheduledClass()
                {
                    ID = x.ID,

                    // From Course

                    Name = x.Course.Name,
                    CourseParticipationType = (x.Course.CourseParticipationTypeID == 0)
                                                ? "Same students in all classes"
                                                : "Different students in each class",
                    IsFeaturedCourse = x.Course.IsFeaturedCourse,
                    EnforceOneClassPerStudent = x.Course.EnforceOneStudentPerClass,
                    FeePerYear = x.Course.CourseFeePerYear,
                    FeePerYearDescription = x.Course.CourseFeePerYearDescription,
                    FeePerTerm = x.Course.CourseFeePerTerm,
                    FeePerTermDescription = x.Course.CourseFeePerTermDescription,
                    Duration = x.Course.Duration,
                    RequiredStudents = x.Course.RequiredStudents,
                    MaximumStudents = x.Course.MaximumStudents,
                    AllowAutoEnroll = x.Course.AllowAutoEnrol,
                    CourseType = x.Course.CourseType.Name,
                    OfferedBy = x.Course.OfferedBy,

                    // From Class

                    OfferedTerm1 = x.OfferedTerm1,
                    OfferedTerm2 = x.OfferedTerm2,
                    OfferedTerm3 = x.OfferedTerm3,
                    OfferedTerm4 = x.OfferedTerm4,
                    StartDate = (x.StartDate != null)
                                    ? DateOnly.FromDateTime(x.StartDate.Value)
                                    : null,
                    StartTime = TimeOnly.FromDateTime(x.StartTime),
                    EndTime = (x.EndTime != null) ? TimeOnly.FromDateTime(x.EndTime.Value)
                                                  : null,
                    Occurrence = x.Occurrence.Name,
                    Recurrence = x.Recurrence,
                    OnDay = x.OnDay.Day,
                    OccurrenceTextBrief = x.OccurrenceTextBrief,
                    OccurrenceText = x.OccurrenceText,
                    Venue = x.Venue.Name,
                    VenueAddress = x.Venue.Address,
                    TotalActiveStudents = x.TotalActiveStudents,
                    TotalWaitlistedStudents = x.TotalWaitlistedStudents,
                    ParticipationRate = x.ParticipationRate

                }).ToList();

                classes.ForEach(delegate (Class c)
                {
                    var sc = data.Classes.Find(x => x.ID == c.ID);
                    if (sc != null)
                    {
                        if (!string.IsNullOrWhiteSpace(c.GuestLeader)) sc.Leader.Add(new ScheduledPerson()
                        {
                            Name = c.GuestLeader,
                            IsGuestLeader = true
                        });
                        if (c.Leader != null) sc.Leader.Add(new ScheduledPerson()
                        {
                            Name = c.Leader.FullNameWithPostNominals,
                            Email = c.Leader.Email,
                            Phone = c.Leader.AdjustedHomePhone,
                            Mobile = c.Leader.AdjustedMobile
                        });
                        if (c.Leader2 != null) sc.Leader.Add(new ScheduledPerson()
                        {
                            Name = c.Leader2.FullNameWithPostNominals,
                            Email = c.Leader2.Email,
                            Phone = c.Leader2.AdjustedHomePhone,
                            Mobile = c.Leader2.AdjustedMobile
                        });
                        if (c.Leader3 != null) sc.Leader.Add(new ScheduledPerson()
                        {
                            Name = c.Leader3.FullNameWithPostNominals,
                            Email = c.Leader3.Email,
                            Phone = c.Leader3.AdjustedHomePhone,
                            Mobile = c.Leader3.AdjustedMobile
                        });
                        foreach (var clerk in c.Clerks)
                        {
                            sc.Clerk.Add(new ScheduledPerson()
                            {
                                Name = clerk.FullNameWithPostNominals,
                                Email = clerk.Email,
                                Phone = clerk.AdjustedHomePhone,
                                Mobile = clerk.AdjustedMobile
                            });
                        }
                        foreach (var d in c.ClassDates)
                        {
                            sc.ClassDates.Add(d);
                        }
                        if (sc.StartDate == null)
                        {
                            sc.StartDate = DateOnly.FromDateTime(sc.ClassDates.OrderBy(x => x).FirstOrDefault());
                        }
                    }
                });
            }
            return data;
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
