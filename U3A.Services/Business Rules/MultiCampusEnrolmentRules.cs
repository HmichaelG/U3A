using DevExpress.Blazor;
using DevExpress.Pdf.ContentGeneration.Interop;
using DevExpress.XtraRichEdit.Import.Rtf;
using Eway.Rapid.Abstractions.Response;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using U3A.Database;
using U3A.Model;
using U3A.Services;

namespace U3A.BusinessRules
{
    public static partial class BusinessRule
    {

        public static async Task<Enrolment> GetMultiCampusEnrolmentAsync(
                                                            U3ADbContext dbc,
                                                            TenantDbContext dbcT,
                                                            Guid EnrolmentID,
                                                            string Tenant)
        {
            Enrolment result = null;
            var mcEnrolment = await dbcT.MultiCampusEnrolment.FirstOrDefaultAsync(x => x.ID == EnrolmentID);
            if (mcEnrolment != null)
            {
                var mcTerm = await dbcT.MultiCampusTerm.FindAsync(mcEnrolment.TermID);
                var mcPerson = await dbcT.MultiCampusPerson.FindAsync(mcEnrolment.PersonID);
                result = BusinessRule.GetEnrolmentFromMCEnrolment(mcEnrolment, mcPerson, mcTerm);
                result.Course = await dbc.Course.FindAsync(mcEnrolment.CourseID);
            }
            return result;
        }
        public static async Task<List<Enrolment>> GetMultiCampusEnrolmentsAsync(
                                                            U3ADbContext dbc,
                                                            TenantDbContext dbcT,
                                                            string Tenant,
                                                            Guid? CourseID = null,
                                                            Guid? ClassID = null)
        {
            ConcurrentBag<Enrolment> result = new();
            var mcEnrolment = await dbcT.MultiCampusEnrolment.Where(x => x.TenantIdentifier == Tenant).ToListAsync();
            if (mcEnrolment != null && mcEnrolment.Count > 0)
            {
                if (CourseID != null && ClassID == null)
                {
                    mcEnrolment = mcEnrolment.Where(x => x.CourseID == CourseID && x.ClassID == null).ToList();
                }
                if (CourseID != null && ClassID != null)
                {
                    mcEnrolment = mcEnrolment
                                    .Where(x => x.CourseID == CourseID
                                                && x.ClassID == ClassID).ToList();
                }
                foreach (var e in mcEnrolment)
                {
                    var mcTerm = await dbcT.MultiCampusTerm.FindAsync(e.TermID);
                    if (mcTerm != null)
                    {
                        var mcPerson = await dbcT.MultiCampusPerson.FindAsync(e.PersonID);
                        if (mcPerson != null)
                        {
                            if (e.ClassID == null)
                            {
                                var classes = await dbc.Class.Where(x => x.CourseID == e.CourseID).ToListAsync();
                                foreach (var c in classes)
                                {
                                    result.Add(BusinessRule.GetEnrolmentFromMCEnrolment(e, mcPerson, c, mcTerm));
                                }
                            }
                            else
                            {
                                var c = await dbc.Class.FindAsync(e.ClassID);
                                result.Add(BusinessRule.GetEnrolmentFromMCEnrolment(e, mcPerson, c, mcTerm));
                            }
                        }
                    }
                }
            }
            return result.ToList();
        }


        public static bool AllowMultiCampusForCourse(Course course, SystemSettings settings)
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
        /// <param name="RequestedClass"></param>
        /// <param name="person"></param>
        /// <param name="term"></param>
        /// <returns></returns>
        public static async Task<Enrolment> AddMultiCampusEnrolmentRequests(TenantDbContext dbT,
                                            Class RequestedClass,
                                            Person person,
                                            Term term, Term prevTerm)
        {
            Enrolment result = default;
            bool isFutureTerm = false;
            int thisYear;
            int thisTermNo;
            MultiCampusTerm thisTerm;
            var c = RequestedClass;
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
                    var e = GetEnrolmentFromMCEnrolment(mcE, person, thisTerm, c);
                    result = e;
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
                    var e = GetEnrolmentFromMCEnrolment(mcE, person, thisTerm, c);
                    result = e;
                    c.Course.Enrolments.Add(e);
                    await dbT.AddAsync<MultiCampusEnrolment>(mcE);
                }
            }
            return result;
        }

    }

}
