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
            var settings = dbc.SystemSettings.OrderBy(x => x.ID).FirstOrDefault();
            var people = dbc.Person.IgnoreQueryFilters()
                            .Where(x => !x.IsDeleted && x.DateCeased == null).ToList();
            var result = new List<EnrolmentDetail>();
            EnrolmentDetail ed;
            var p = enrolment.Person
                        ?? BusinessRule.SelectPerson(dbc, enrolment.PersonID) ?? throw new ArgumentNullException(nameof(Person));
            var t = enrolment.Term
                        ?? dbc.Term.Find(enrolment.TermID) ?? throw new ArgumentNullException(nameof(Term));
            var cr = dbc.Course.Where(x => x.ID == enrolment.CourseID).Include(x => x.Enrolments).FirstOrDefault();
            var pt = dbc.CourseParticpationType.Find(cr.CourseParticipationTypeID);
            var ct = dbc.CourseType.Find(cr.CourseTypeID);
            var classes = new List<Class>();
            var IsPreRandomAllocationPeriod = BusinessRule.IsPreRandomAllocationEmailDay(t, settings, dbc.GetLocalTime());
            if (enrolment.ClassID != null)
            {
                classes.Add(dbc.Class
                                .Include(c => c.Enrolments)
                                .Include(x => x.Leader)
                                .Include(x => x.Leader2)
                                .Include(x => x.Leader3)
                                .Include(x => x.Course)
                                .Include(x => x.Occurrence)
                                .Where(x => x.ID == enrolment.ClassID).FirstOrDefault());
            }
            else
            {
                classes.AddRange(dbc.Class
                                .Include(x => x.Leader)
                                .Include(x => x.Leader2)
                                .Include(x => x.Leader3)
                                .Include(x => x.Course).ThenInclude(c => c.Enrolments)
                                .Include(x => x.Occurrence)
                                .Where(x => x.CourseID == cr.ID).ToList());
            }
            foreach (Class c in classes)
            {
                foreach (var e in c.Enrolments)
                {
                    e.Person = people.FirstOrDefault(x => x.ID == e.PersonID);
                }
                c.Enrolments = c.Enrolments.Where(x => x.Person != null).ToList();
                BusinessRule.AssignClassContacts(c, t, settings);
            }
            var termEnrolments = dbc.Enrolment.AsNoTracking().Where(x => x.TermID == t.ID);
            foreach (var c in classes)
            {
                SetCourseParticipationDetails(dbc, c, termEnrolments);
                var od = dbc.WeekDay.Find(c.OnDayID);
                var v = dbc.Venue.Find(c.VenueID);
                var l = dbc.Person.Find(c.LeaderID);
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
                        if (!cr.AllowAutoEnrol) reason = " because it has been CLOSED by the course leader.";
                        ed.WaitlistMessage =
                            $@"Thank you for your interest in our program.
We are currently unable to place you in this course{reason}. 
Your request has been <b>Waitlisted</b> meaning you will be notified should a place become available in the future.
<b>Please do not attend classes in which you are waitlisted.</b>";
                    }
                }
                else { ed.WaitlistMessage = string.Empty; }
                GetOrganisationPersonDetail(dbc, ed, t, p);
                if (ed.EnrolmentIsLeader) { ed.PersonFullName += " (Leader)"; }
                if (ed.EnrolmentIsClerk) { ed.PersonFullName += " (Clerk)"; }
                result.Add(ed);
            }
            return result;
        }

    }
}