using DevExpress.DirectX.NativeInterop.Direct2D;
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
        public static async Task<int> CountOfTermEnrolments(U3ADbContext dbc, Term? targetTerm)
        {
            return await dbc.Enrolment.Include(x => x.Person)
                                .Where(enrolment => enrolment.Term == targetTerm &&
                                                        enrolment.Person.DateCeased == null).CountAsync();
        }
        public static async Task<Term?> GetNextTermInYear(U3ADbContext dbc, Term? sourceTerm)
        {
            return await dbc.Term
                            .Where(x => x.Year == sourceTerm.Year && x.TermNumber == sourceTerm.TermNumber + 1).FirstOrDefaultAsync();
        }
        public static async Task<Term?> GetNextTermAsync(U3ADbContext dbc, Term? sourceTerm)
        {
            int year = sourceTerm.Year;
            int termNumber = sourceTerm.TermNumber + 1;
            if (termNumber > 4) { year++; termNumber = 1; }
            return await dbc.Term
                            .Where(x => x.Year == year && x.TermNumber == termNumber).FirstOrDefaultAsync();
        }
        public static async Task<Term?> GetNextTermAsync(U3ADbContext dbc)
        {
            var term = await CurrentTermAsync(dbc);
            return await GetNextTermAsync(dbc,term);
        }
        public static async Task<Term?> GetFirstTermNextYearAsync(U3ADbContext dbc, Term? sourceTerm)
        {
            return await dbc.Term
                            .Where(x => x.Year == sourceTerm.Year + 1 && x.TermNumber == 1).FirstOrDefaultAsync();
        }

        public static async Task BringForwardEnrolmentsAsync(U3ADbContext dbc,
                    Term? sourceTerm, Term? targetTerm, bool SetCurrentTerm)
        {
            var terms = await dbc.Term.Where(x => x.Year == sourceTerm.Year
                                                && x.TermNumber <= sourceTerm.TermNumber)
                                        .OrderByDescending(x => x.TermNumber)
                                        .ToListAsync();
            foreach (var term in terms)
            {
                await BringForwardEnrolmentsAsync(dbc, term, targetTerm);
            }
            if (SetCurrentTerm)
            {
                foreach (var t in dbc.Term.Where(x => x.IsDefaultTerm))
                {
                    t.IsDefaultTerm = false;
                }
                var trm = await dbc.Term.FindAsync(targetTerm.ID);
                trm.IsDefaultTerm = true;
            }

            await dbc.SaveChangesAsync();
        }

        static async Task BringForwardEnrolmentsAsync(U3ADbContext dbc,
                                    Term sourceTerm, Term targetTerm)
        {

            var enrolments = await dbc.Enrolment
                            .Include(x => x.Class)
                            .Include(x => x.Course)
                            .Include(x => x.Person)
                            .Where(enrolment => enrolment.Term == sourceTerm
                                                    && enrolment.Person.DateCeased == null
                                                    && enrolment.Person.FinancialTo >= sourceTerm.Year)
                            .ToListAsync();
            foreach (var e in enrolments)
            {
                if (ShouldBringForwardPrevTerm(dbc, sourceTerm, targetTerm, e.Class))
                {
                    await CreateEnrolment(dbc, e, targetTerm);
                }
                else
                {
                    foreach (var course in dbc.Course
                                        .Include(x => x.Classes)
                                        .Where(x => x.ID == e.Course.ID).ToList())
                    {
                        foreach (var clss in course.Classes)
                        {
                            if (ShouldBringForwardPrevTerm(dbc, sourceTerm, targetTerm, clss))
                            {
                                await CreateEnrolment(dbc, e, targetTerm);
                                break;  // we only need one new enrolment
                            }
                        }
                    }
                }
            }

        }

        static async Task CreateEnrolment(U3ADbContext dbc, Enrolment currentEnrolment, Term? targetTerm)
        {
            var newEnrolment = new Enrolment();
            currentEnrolment.CopyTo(newEnrolment);
            newEnrolment.Term = await dbc.Term.FindAsync(targetTerm.ID);
            newEnrolment.Course = await dbc.Course.FindAsync(currentEnrolment.CourseID);
            if (newEnrolment.Course == null) return;
            newEnrolment.Person = await dbc.Person.FindAsync(currentEnrolment.PersonID);
            if (newEnrolment.Person == null) return;
            if (currentEnrolment.Class != null)
            {
                newEnrolment.Class = await dbc.Class.FindAsync(currentEnrolment.ClassID);
                if (newEnrolment.Class == null) return;
            }
            newEnrolment.ID = Guid.Empty;
            if (!await IsAlreadyEnrolled(dbc, newEnrolment)) { await dbc.Enrolment.AddAsync(newEnrolment); }
        }
        static async Task<bool> IsAlreadyEnrolled(U3ADbContext dbc, Enrolment e)
        {
            bool result;
            if (e.ClassID == null)
            {
                result = await dbc.Enrolment.AnyAsync(x => x.TermID == e.Term.ID &&
                                                x.CourseID == e.Course.ID &&
                                                x.PersonID == e.Person.ID);
            }
            else
            {
                result = await dbc.Enrolment.AnyAsync(x => x.TermID == e.Term.ID &&
                                x.CourseID == e.Course.ID &&
                                x.PersonID == e.Person.ID &&
                                x.ClassID == e.Class.ID);
            }
            return result;
        }
        public static bool IsClassInTerm(U3ADbContext dbc, Term? targetTerm, Class? c)
        {
            bool result = false;
            if (c == null) { return result; }
            if (c.StartDate == null && c.Recurrence == null)
            {
                if (targetTerm.TermNumber == 1 && c.OfferedTerm1) { result = true; }
                if (targetTerm.TermNumber == 2 && c.OfferedTerm2) { result = true; }
                if (targetTerm.TermNumber == 3 && c.OfferedTerm3) { result = true; }
                if (targetTerm.TermNumber == 4 && c.OfferedTerm4) { result = true; }
            }
            else
            {
                var endDate = BusinessRule.GetClassEndDate(dbc, c, targetTerm);
                if (endDate != null && endDate >= targetTerm.StartDate) { result = true; }
            }
            return result;
        }
        static bool ShouldBringForwardPrevTerm(U3ADbContext dbc,
                                            Term sourceTerm,
                                            Term targetTerm, Class? c)
        {
            bool result = false;
            if (c == null) { return result; }
            switch (targetTerm.TermNumber - sourceTerm.TermNumber)
            {
                case 2: // term 3 & term 1, term 4 & term 2
                    if (sourceTerm.TermNumber == 3 && !c.OfferedTerm2) result = true;
                    if (sourceTerm.TermNumber == 4 && !c.OfferedTerm3) result = true;
                    break;
                case 3: // term 4 and term 1 only
                    if (!c.OfferedTerm2 && !c.OfferedTerm3) result = true;
                    break;
                default:
                    result = true; break;
            }
            if (result) result = IsClassInTerm(dbc, targetTerm, c);
            return result;
        }
    }
}
