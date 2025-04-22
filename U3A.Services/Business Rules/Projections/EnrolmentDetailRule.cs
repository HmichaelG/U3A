using Microsoft.EntityFrameworkCore;
using Twilio.TwiML.Fax;
using U3A.Database;
using U3A.Model;

namespace U3A.BusinessRules
{
    public static partial class BusinessRule
    {
        public static List<EnrolmentDetail> GetEnrolmentSample(U3ADbContext dbc)
        {
            // Grab a random Enrolment
            return GetEnrolmentDetail(dbc,
                dbc.Enrolment.OrderBy(x => Guid.NewGuid()).Take(1).FirstOrDefault());
        }

        public static List<EnrolmentDetail> GetEnrolmentDetail(U3ADbContext dbc, Enrolment enrolment)
        {
            Task<List<EnrolmentDetail>> syncTask = Task.Run(async () =>
            {
                return await GetEnrolmentDetailAsync(dbc, enrolment);
            });
            syncTask.Wait();
            return syncTask.Result;
        }
        public static async Task<List<EnrolmentDetail>> GetEnrolmentDetailAsync(U3ADbContext dbc, Enrolment enrolment)
        {
            var settings = await dbc.SystemSettings.OrderBy(x => x.ID).FirstOrDefaultAsync();
            var result = new List<EnrolmentDetail>();
            EnrolmentDetail ed;
            var p = enrolment.Person
                        ?? await BusinessRule.SelectPersonAsync(dbc, enrolment.PersonID) ?? throw new ArgumentNullException(nameof(Person));
            var t = enrolment.Term
                        ?? await dbc.Term.FindAsync(enrolment.TermID) ?? throw new ArgumentNullException(nameof(Term));
            var enrolmentTerm = await CurrentEnrolmentTermAsync(dbc)
                        ?? await CurrentTermAsync(dbc)
                        ?? throw new ArgumentNullException(nameof(Term));

            var cr = await dbc.Course.Where(x => x.ID == enrolment.CourseID).Include(x => x.Enrolments).FirstOrDefaultAsync();
            var pt = await dbc.CourseParticpationType.FindAsync(cr.CourseParticipationTypeID);
            var ct = await dbc.CourseType.FindAsync(cr.CourseTypeID);
            var classes = new List<Class>();
            var IsPreRandomAllocationPeriod = BusinessRule.IsPreRandomAllocationEmailDay(t, settings, dbc.GetLocalTime());
            if (enrolment.ClassID != null)
            {
                classes.Add(await dbc.Class
                                .Include(c => c.Enrolments)
                                .Include(x => x.Leader)
                                .Include(x => x.Leader2)
                                .Include(x => x.Leader3)
                                .Include(x => x.Course)
                                .Include(x => x.Occurrence)
                                .Where(x => x.ID == enrolment.ClassID).FirstOrDefaultAsync());
            }
            else
            {
                classes.AddRange(await dbc.Class
                                .Include(x => x.Leader)
                                .Include(x => x.Leader2)
                                .Include(x => x.Leader3)
                                .Include(x => x.Course).ThenInclude(c => c.Enrolments)
                                .Include(x => x.Occurrence)
                                .Where(x => x.CourseID == cr.ID).ToListAsync());
            }
            foreach (Class c in classes)
            {
                foreach (var e in c.Enrolments)
                {
                    if (e.Person == null) { e.Person = await dbc.Person.FindAsync(e.PersonID); }
                }
                c.Enrolments = c.Enrolments.Where(x => x.Person != null).ToList();
                BusinessRule.AssignClassContacts(c, t, settings);
            }
            var termEnrolments = await dbc.Enrolment.AsNoTracking().Where(x => x.CourseID == cr.ID && x.TermID == t.ID).ToListAsync();
            foreach (var c in classes)
            {
                SetCourseParticipationDetails(c, termEnrolments);
                var od = await dbc.WeekDay.FindAsync(c.OnDayID);
                var v = await dbc.Venue.FindAsync(c.VenueID);
                var l = await dbc.Person.FindAsync(c.LeaderID);
                ed = new EnrolmentDetail()
                {
                    // Course
                    CourseLegacyID = cr.ConversionID,
                    CourseName = cr.Name,
                    CourseDescription = cr.DisplayDescription ?? String.Empty,
                    CourseParticipationType = pt.Name,
                    CourseFeePerYear = cr.CourseFeePerYear,
                    CourseFeePerYearDescription = cr.CourseFeePerYearDescription ?? String.Empty,
                    CourseFeePerTerm = cr.CourseFeePerTerm,
                    CourseFeePerTermDescription = cr.CourseFeePerTermDescription ?? String.Empty,
                    CourseDuration = cr.Duration,
                    CourseRequiredStudents = cr.RequiredStudents,
                    CourseMaximumStudents = cr.MaximumStudents,
                    CourseTotalActiveStudents = c.TotalActiveStudents,
                    CourseTotalWaitlistedStudents = c.TotalWaitlistedStudents,
                    CourseParticipationRate = c.ParticipationRate,
                    CourseType = ct.Name,
                    // Class
                    ClassOfferedTerm1 = c.OfferedTerm1,
                    ClassOfferedTerm2 = c.OfferedTerm2,
                    ClassOfferedTerm3 = c.OfferedTerm3,
                    ClassOfferedTerm4 = c.OfferedTerm4,
                    ClassOfferedSummary = c.OfferedSummary,
                    ClassStartDate = c.StartDate,
                    ClassOnDay = od.Day,
                    ClassStartTime = c.StartTime,
                    ClassOccurrence = c.OccurrenceText,
                    ClassEndTime = c.EndTime,
                    ClassStrEndTime = c.StrEndTime,
                    ClassSummary = c.ClassSummary,
                    ClassDetail = c.ClassDetail,
                    ClassDetailWithoutVenue = c.ClassDetailWithoutVenue,
                    ClassVenue = v.Name,
                    ClassVenueAddress = v.Address,
                    // Enrolment
                    EnrolmentDateReceived = enrolment.Created,
                    EnrolmentDateEnrolled = enrolment.DateEnrolled,
                    EnrolmentIsWaitlisted = enrolment.IsWaitlisted,
                    EnrolmentIsClerk = enrolment.IsCourseClerk,
                    EnrolmentIsLeader = enrolment.isLeader,
                    EnrolmentStatus = GetEnrolmentStatus(enrolment, t, settings, dbc.GetLocalTime()),
                    WaitlistSort = enrolment.WaitlistSort
                };
                ed.ClassLeader = c.LeaderNamesOnly;
                ed.ClassContact = c.CourseContactDetails;
                if (enrolment.IsWaitlisted)
                {
                    if (IsPreRandomAllocationPeriod)
                    {
                        var allocationMailoutDay = GetThisTermAllocationMailoutDay(t, settings);
                        ed.WaitlistMessage =
                            @$"Thank you for your interest in our program.{Environment.NewLine}Your enrolment request has been Waitlisted pending random course enrolment allocation.{Environment.NewLine}We anticipate the next course allocation will complete on or about <b>{allocationMailoutDay.ToString("dddd, dd MMMM yyyy")}</b>";
                    }
                    else
                    {
                        var reason = string.Empty;
                        if (c.TotalActiveStudents >= cr.MaximumStudents) reason = " because it is FULL";
                        if (!cr.AllowAutoEnrol)
                        {
                            reason = (cr.AutoEnrolDisabledReason == null)
                                        ? " because it is CLOSED for new enrolments"
                                        : $". It is CLOSED because...{Environment.NewLine}{cr.AutoEnrolDisabledReason}";
                        }
                        ed.WaitlistMessage =
                            $@"Thank you for your interest in our program.
We are currently unable to place you in this course{reason}. 
Your request has been <b>Waitlisted</b> meaning you will be notified should a place become available in the future.
<b>Please do not attend classes in which you are waitlisted.</b>";
                        if (!IsClassInTerm(c, enrolmentTerm.TermNumber))
                        {
                            ed.WaitlistMessage =
                                $@"The class you have requested will be held in Term {t.TermNumber}.
Class allocation for <b>{cr.Name}</b> will occur shortly before the commencement of Term {t.TermNumber}. 
Your request remains <b>Waitlisted</b> until allocation occurs on or before {t.StartDate.ToString(constants.FULL_DAY_DATE_FORMAT)}.";
                        }
                    }
                }
                else { ed.WaitlistMessage = string.Empty; }
                GetOrganisationPersonDetail(settings, ed, t, p);
                if (ed.EnrolmentIsLeader) { ed.PersonFullName += " (Leader)"; }
                if (ed.EnrolmentIsClerk) { ed.PersonFullName += " (Clerk)"; }
                result.Add(ed);
            }
            return result;
        }

    }
}