using DevExpress.Blazor;
using DevExpress.Pdf.ContentGeneration.Interop;
using DevExpress.XtraRichEdit.Import.Rtf;
using Eway.Rapid.Abstractions.Response;
using Microsoft.EntityFrameworkCore;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Twilio.Rest.Trunking.V1;
using U3A.Database;
using U3A.Model;
using U3A.Services;

namespace U3A.BusinessRules
{
    public static partial class BusinessRule
    {

        public static bool AllowMultiCampusForCourse(Course course,SystemSettings settings)
        {
            bool result = false;
            if (settings.AllowMultiCampusExtensions)
            {
                if (course.CourseFeePerYear == 0 && course.CourseFeePerTerm == 0)
                {
                    if (course.CourseParticipationTypeID == (int)ParticipationType.SameParticipantsInAllClasses)
                    {
                        if (course.Classes.Count == 1)
                        {
                            var c = course.Classes.First();
                            var terms = 0;
                            if (c.OfferedTerm1) terms++;
                            if (c.OfferedTerm2) terms++;
                            if (c.OfferedTerm3) terms++;
                            if (c.OfferedTerm4) terms++;
                            if (terms == 1) result = true;
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Delete enrolments no longer required by a member.
        /// </summary>
        /// <param name="dbc"></param>
        /// <param name="person"></param>
        /// <param name="term"></param>
        public static async Task DeleteMultiCampusEnrolmentsRescinded(TenantDbContext dbc,
                                            IEnumerable<Class> DeletedClasses,
                                            Person person,
                                            Term term, Term prevTerm)
        {
            foreach (var c in DeletedClasses)
            {
                if ((ParticipationType)c.Course.CourseParticipationTypeID == ParticipationType.SameParticipantsInAllClasses)
                {
                    var deletion = await dbc.MultiCampusEnrolment
                                        .Where(x =>
                                            x.PersonID == person.ID &&
                                            x.CourseID == c.CourseID).ToListAsync();
                    dbc.RemoveRange(deletion);
                }
                else
                {
                    var deletion = await dbc.MultiCampusEnrolment
                                    .Where(x =>
                                        x.PersonID == person.ID &&
                                        x.CourseID == c.CourseID &&
                                        x.ClassID == c.ID).ToListAsync();
                    dbc.RemoveRange(deletion);
                }
            }
        }

        /// <summary>
        /// Add enrolments requested by a member
        /// </summary>
        /// <param name="dbT"></param>
        /// <param name="RequestedClasses"></param>
        /// <param name="person"></param>
        /// <param name="term"></param>
        /// <returns></returns>
        public static async Task<List<Enrolment>> AddMultiCampusEnrolmentRequests(TenantDbContext dbT,
                                            IEnumerable<Class> RequestedClasses,
                                            Person person,
                                            Term term, Term prevTerm)
        {
            var result = new List<Enrolment>();
            bool isFutureTerm = false;
            int thisYear;
            int thisTermNo;
            MultiCampusTerm thisTerm;
            foreach (var c in RequestedClasses)
            {
                if (c.TermNumber == term.TermNumber)
                {
                    //current term
                    thisYear = term.Year;
                    thisTermNo = term.TermNumber;
                }
                else if (c.TermNumber == prevTerm.TermNumber)
                {
                    //last term
                    thisYear = prevTerm.Year;
                    thisTermNo = prevTerm.TermNumber;
                }
                else
                {
                    //future term
                    thisYear = term.Year;
                    thisTermNo = c.TermNumber;
                    isFutureTerm = true;
                }
                thisTerm = await dbT.MultiCampusTerm
                                .AsNoTracking()
                                .FirstOrDefaultAsync(x => x.TenantIdentifier == c.TenantIdentifier
                                                            && x.Year == term.Year
                                                            && x.TermNumber == thisTermNo);
                var course = c.Course;
                if ((ParticipationType)c.Course.CourseParticipationTypeID == ParticipationType.SameParticipantsInAllClasses)
                {
                    if (!await dbT.MultiCampusEnrolment.AnyAsync(x =>
                                        x.PersonID == person.ID &&
                                        x.TermID == thisTerm.ID &&
                                        x.CourseID == c.CourseID))
                    {
                        var mcE = new MultiCampusEnrolment()
                        {
                            IsWaitlisted = true,
                            TenantIdentifier = c.TenantIdentifier,
                            PersonID = person.ID,
                            TermID = thisTerm.ID,
                            CourseID = c.Course.ID
                        };
                        var e = GetEnrolmentFromMultiCampusEnrolment(mcE, person, c, thisTerm);
                        result.Add(e);
                        c.Course.Enrolments.Add(e);
                        await dbT.AddAsync<MultiCampusEnrolment>(mcE);
                    }
                }
                else
                {
                    if (!await dbT.MultiCampusEnrolment.AnyAsync(x =>
                                        x.PersonID == person.ID &&
                                        x.TermID == thisTerm.ID &&
                                        x.CourseID == c.CourseID &&
                                        x.ClassID == c.ID))
                    {
                        var mcE = new MultiCampusEnrolment()
                        {
                            TenantIdentifier = c.TenantIdentifier,
                            IsWaitlisted = true,
                            PersonID = person.ID,
                            TermID = thisTerm.ID,
                            CourseID = c.Course.ID,
                            ClassID = c.ID
                        };
                        var e = GetEnrolmentFromMultiCampusEnrolment(mcE, person, c, thisTerm);
                        result.Add(e);
                        c.Course.Enrolments.Add(e);
                        await dbT.AddAsync<MultiCampusEnrolment>(mcE);
                    }
                }
            }
            return result;
        }

        public static Enrolment GetEnrolmentFromMultiCampusEnrolment(MultiCampusEnrolment e,
                                    MultiCampusPerson p,
                                    Class c,
                                    MultiCampusTerm t)
        {
            Person person = new()
            {
                ID = p.ID,
                Title = p.Title,
                PostNominals = p.PostNominals,
                FirstName = p.FirstName,
                LastName = p.LastName,
                Email = p.Email,
                HomePhone = p.HomePhone,
                Mobile = p.Mobile,
                SMSOptOut = p.SMSOptOut,
                ICEContact = p.ICEContact,
                ICEPhone = p.ICEPhone,
                VaxCertificateViewed = p.VaxCertificateViewed,
                Communication = p.Communication,
            };
            return GetEnrolmentFromMultiCampusEnrolment(e, person, c, t);
        }
        public static Enrolment GetEnrolmentFromMultiCampusEnrolment(MultiCampusEnrolment e,
                                    Person p,
                                    Class c,
                                    MultiCampusTerm t)
        {
            var term = new Term()
            {
                Duration = t.Duration,
                EnrolmentEnds = t.EnrolmentEnds,
                EnrolmentStarts = t.EnrolmentStarts,
                ID = t.ID,
                StartDate = t.StartDate,
                IsDefaultTerm = t.IsDefaultTerm,
                IsClassAllocationFinalised = t.IsClassAllocationFinalised,
            };
            return new Enrolment()
            {
                ID = e.ID,
                IsWaitlisted = e.IsWaitlisted,
                PersonID = e.PersonID,
                TermID = e.TermID,
                Term = term,
                CourseID = e.CourseID,
                Course = c.Course,
                ClassID = e.ClassID,
                Created = e.Created,
                DateEnrolled = e.DateEnrolled,
                Person = p,
            };
        }

    }

}
