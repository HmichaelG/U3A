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

namespace U3A.BusinessRules
{
    public static partial class BusinessRule
    {
        public static async Task<List<Enrolment>> EditableEnrolmentsAsync(U3ADbContext dbc, Term SelectedTerm,
                              Course SelectedCourse, Class SelectedClass)
        {
            if (SelectedClass == null || SelectedCourse.CourseParticipationTypeID == (int?)ParticipationType.SameParticipantsInAllClasses)
            {
                return dbc.Enrolment
                                    .Include(x => x.Term)
                                    .Include(x => x.Course)
                                    .Include(x => x.Person)
                                    .Where(x => x.CourseID == SelectedCourse.ID
                                                        && x.TermID == SelectedTerm.ID
                                                        && x.Person.DateCeased == null)
                                    .AsEnumerable()
                                    .OrderBy(x => x.IsWaitlisted)
                                                .ThenBy(x => x.WaitlistSort)
                                                .ThenBy(x => x.Person.LastName)
                                                .ThenBy(x => x.Person.FirstName)
                                    .ToList();
            }
            else
            {
                return dbc.Enrolment
                                    .Include(x => x.Term)
                                    .Include(x => x.Course)
                                    .Include(x => x.Person)
                                    .Where(x => x.ClassID == SelectedClass.ID
                                                    && x.TermID == SelectedTerm.ID
                                                    && x.Person.DateCeased == null)
                                    .AsEnumerable()
                                    .OrderBy(x => x.IsWaitlisted)
                                                .ThenBy(x => x.WaitlistSort)
                                                .ThenBy(x => x.Person.LastName)
                                                .ThenBy(x => x.Person.FirstName)
                                    .ToList();
            }
        }

        public static async Task<List<Enrolment>> GetEnrolmentIncludeLeadersAsync(U3ADbContext dbc,
                                                  Course thisCourse,  Class thisClass,Term thisTerm)
        {
            var enrolments = new List<Enrolment>();
            if (await dbc.Enrolment.AnyAsync(x => x.ClassID == thisClass.ID && x.TermID == thisTerm.ID))
            {
                enrolments = await dbc.Enrolment.AsNoTracking()
                                          .Include(x => x.Person)
                                          .Include(x => x.Course)
                                          .Where(x => x.ClassID == thisClass.ID
                                                    && x.TermID == thisTerm.ID).ToListAsync();
            }
            else
            {
                enrolments = await dbc.Enrolment.AsNoTracking()
                                            .Include(x => x.Person)
                                            .Include(x => x.Course)
                                            .Where(x => x.CourseID == thisCourse.ID
                                                            && x.TermID == thisTerm.ID).ToListAsync();
            };
            Enrolment? dummy;
            if (enrolments.Count > 0)
            {
                var template = enrolments[0];
                dummy = await CreateDummyLeaderEnrolment(dbc,template, thisClass.LeaderID);
                if (dummy != null) enrolments.Add(dummy);
                dummy = await CreateDummyLeaderEnrolment(dbc,template, thisClass.Leader2ID);
                if (dummy != null) enrolments.Add(dummy);
                dummy = await CreateDummyLeaderEnrolment(dbc,template, thisClass.Leader3ID);
                if (dummy != null) enrolments.Add(dummy);
            }
            return enrolments.OrderBy(x => x.Person.FullNameAlpha).ToList();
        }

        private static async Task<Enrolment?> CreateDummyLeaderEnrolment(U3ADbContext dbc, Enrolment template, Guid? LeaderID)
        {
            if (LeaderID == null) return null;
            var person = await dbc.Person.FindAsync(LeaderID);
            if (person == null) return null;
            var result = new Enrolment();
            template.CopyTo(result);
            result.PersonID = person.ID;
            result.Person = person;
            result.isLeader = true;
            result.IsWaitlisted = false;
            result.Course = await dbc.Course.FindAsync(result.CourseID);
            return result;
        }
        public static async Task<List<Dropout>> EditableDropoutsAsync(U3ADbContext dbc, Term SelectedTerm)
        {
            return await dbc.Dropout
                                .Include(x => x.Term)
                                .Include(x => x.Course)
                                .Include(x => x.Person)
                                .Where(x => x.TermID == SelectedTerm.ID)
                                .OrderBy(x => x.IsWaitlisted)
                                            .ThenBy(x => x.Person.LastName)
                                            .ThenBy(x => x.Person.FirstName)
                                .ToListAsync();
        }

        public static async Task<List<Course>> GetStudentCoursesEnrolled(U3ADbContext dbc,
                                                    Person SelectedPerson, Term SelectedTerm)
        {
            var courseID = dbc.Enrolment.Where(x => x.TermID == SelectedTerm.ID &&
                                                        x.PersonID == SelectedPerson.ID)
                                                .Select(x => x.CourseID).Distinct().ToArray();
            return await dbc.Course
                            .Where(x => courseID.Contains(x.ID))
                                    .ToListAsync();
        }

        public static bool SetWaitlistStatus(Term selectedTerm, Term currentTerm)
        {
            bool result = false;
            if (selectedTerm.Comparer > currentTerm.Comparer)
            {
                // Any future enrolments must be waitlisted.
                result = true;
            }
            else
            {
                // set Enrolled if class allocation finalised
                // otherwise, set waitlisted.
                result = !currentTerm.IsClassAllocationFinalised;
            }
            return result;
        }
        public static async Task<bool> AnyEnrolmentsInYear(U3ADbContext dbc, Course course, int Year)
        {
            bool result = await dbc.Enrolment.AsNoTracking()
                                .Include(x => x.Course)
                                .Include(x => x.Term)
                                .AnyAsync(x => x.CourseID == course.ID && x.Term.Year == Year);
            return result;
        }
        public static List<Enrolment> SelectableEnrolments(U3ADbContext dbc, Term SelectedTerm)
        {
            var enrolments = dbc.Enrolment
                                .Include(x => x.Term)
                                .Include(x => x.Course)
                                .Include(x => x.Class).ThenInclude(x => x.OnDay)
                                .Include(x => x.Person)
                                .Where(x => x.TermID == SelectedTerm.ID
                                                    && x.Person.DateCeased == null)
                                .OrderBy(x => x.IsWaitlisted)
                                            .ThenBy(x => x.Person.LastName)
                                            .ThenBy(x => x.Person.FirstName)
                                .ToList();
            Parallel.ForEach(enrolments, e =>
            {
                if (e.Class != null)
                {
                    SetCourseParticipationDetails(dbc, e.Class, enrolments);
                }
                else
                {
                    Parallel.ForEach(e.Course.Classes, c =>
                    {
                        SetCourseParticipationDetails(dbc, c, enrolments);
                    });
                }
            });
            return enrolments;
        }

        public static List<Enrolment> SelectableEnrolmentsByClass(U3ADbContext dbc, Class SelectedClass, Term SelectedTerm,
                              Course SelectedCourse)
        {
            if (SelectedClass == null || SelectedCourse.CourseParticipationTypeID == (int?)ParticipationType.SameParticipantsInAllClasses)
            {
                return dbc.Enrolment
                                    .Include(x => x.Term)
                                    .Include(x => x.Course)
                                    .Include(x => x.Person)
                                    .Where(x => x.CourseID == SelectedCourse.ID
                                                        && x.TermID == SelectedTerm.ID
                                                        && x.Person.DateCeased == null)
                                    .OrderBy(x => x.IsWaitlisted)
                                                .ThenBy(x => x.Person.LastName)
                                                .ThenBy(x => x.Person.FirstName)
                                    .ToList();
            }
            else
            {
                return dbc.Enrolment
                                    .Include(x => x.Term)
                                    .Include(x => x.Course)
                                    .Include(x => x.Person)
                                    .Where(x => x.ClassID == SelectedClass.ID
                                                    && x.TermID == SelectedTerm.ID
                                                    && x.Person.DateCeased == null)
                                    .OrderBy(x => x.IsWaitlisted)
                                                .ThenBy(x => x.Person.LastName)
                                                .ThenBy(x => x.Person.FirstName)
                                    .ToList();
            }
        }
        public static async Task<List<Dropout>> SelectableDropoutsByClassAsync(U3ADbContext dbc, Term SelectedTerm, Course SelectedCourse,
                                                                    Class SelectedClass)
        {
            if (SelectedClass == null || SelectedCourse.CourseParticipationTypeID == (int?)ParticipationType.SameParticipantsInAllClasses)
            {
                return await dbc.Dropout
                                    .Include(x => x.Term)
                                    .Include(x => x.Course)
                                    .Include(x => x.Person)
                                    .Where(x => x.CourseID == SelectedCourse.ID
                                                        && x.TermID == SelectedTerm.ID)
                                    .OrderBy(x => x.Person.LastName)
                                                .ThenBy(x => x.Person.FirstName)
                                    .ToListAsync();
            }
            else
            {
                return await dbc.Dropout
                                    .Include(x => x.Term)
                                    .Include(x => x.Course)
                                    .Include(x => x.Person)
                                    .Where(x => x.ClassID == SelectedClass.ID
                                                    && x.TermID == SelectedTerm.ID)
                                    .OrderBy(x => x.Person.LastName)
                                                .ThenBy(x => x.Person.FirstName)
                                    .ToListAsync();
            }
        }

        public static async Task<List<Enrolment>> GetPersonEnrolments(U3ADbContext dbc,
                                    List<Person> people,
                                    Term term)
        {
            var peopleID = new List<Guid>();
            foreach (var p in people) peopleID.Add(p.ID);
            var enrolments = await dbc.Enrolment
                                .Include(x => x.Term)
                                .Include(x => x.Course).ThenInclude(x => x.CourseType)
                                .Include(x => x.Course).ThenInclude(x => x.Classes).ThenInclude(x => x.Leader)
                                .Include(x => x.Course).ThenInclude(x => x.Classes).ThenInclude(x => x.Occurrence)
                                .Include(x => x.Course).ThenInclude(x => x.Classes).ThenInclude(x => x.OnDay)
                                .Include(x => x.Course).ThenInclude(x => x.Classes).ThenInclude(x => x.Venue)
                                .Include(x => x.Person)
                                .Include(x => x.Class)
                                .Where(x => peopleID.Contains(x.PersonID)
                                                && x.Person.DateCeased == null
                                                && x.Person.FinancialTo >= term.Year
                                                && x.TermID == term.ID && x.Class == null)
                                .ToListAsync();
            enrolments.AddRange(await dbc.Enrolment
                                .Include(x => x.Term)
                                .Include(x => x.Person)
                                .Include(x => x.Class)
                                .Include(x => x.Class).ThenInclude(x => x.Leader)
                                .Include(x => x.Class).ThenInclude(x => x.Occurrence)
                                .Include(x => x.Class).ThenInclude(x => x.OnDay)
                                .Include(x => x.Class).ThenInclude(x => x.Venue)
                                .Where(x => peopleID.Contains(x.PersonID)
                                                    && x.Person.DateCeased == null
                                                    && x.Person.FinancialTo >= term.Year
                                                    && x.TermID == term.ID && x.Class != null)
                                .ToListAsync());
            return enrolments;
        }


        public static List<EnrolmentExportData> GetEnrolmentExportDataByPerson(U3ADbContext dbc, Guid PersonID)
        {
            var result = new List<EnrolmentExportData>();
            var settings = dbc.SystemSettings.OrderBy(x => x.ID).FirstOrDefault();
            Term? term = dbc.Term.AsNoTracking().Where(x => x.IsDefaultTerm).FirstOrDefault();
            if (term == null) throw new Exception("The current/default Term has not been set");
            var enrolments = dbc.Enrolment
                                .Include(x => x.Term)
                                .Include(x => x.Course).ThenInclude(x => x.CourseType)
                                .Include(x => x.Course).ThenInclude(x => x.Classes).ThenInclude(x => x.Leader)
                                .Include(x => x.Course).ThenInclude(x => x.Classes).ThenInclude(x => x.Occurrence)
                                .Include(x => x.Course).ThenInclude(x => x.Classes).ThenInclude(x => x.OnDay)
                                .Include(x => x.Course).ThenInclude(x => x.Classes).ThenInclude(x => x.Venue)
                                .Include(x => x.Person)
                                .Include(x => x.Class)
                                .Where(x => x.PersonID == PersonID
                                                && x.Person.DateCeased == null
                                                && x.Person.FinancialTo >= term.Year
                                                && x.TermID == term.ID && x.Class == null)
                                .ToList();
            enrolments.AddRange(dbc.Enrolment
                                .Include(x => x.Term)
                                .Include(x => x.Person)
                                .Include(x => x.Class)
                                .Include(x => x.Class).ThenInclude(x => x.Leader)
                                .Include(x => x.Class).ThenInclude(x => x.Occurrence)
                                .Include(x => x.Class).ThenInclude(x => x.OnDay)
                                .Include(x => x.Class).ThenInclude(x => x.Venue)
                                .Where(x => x.PersonID == PersonID
                                                    && x.Person.DateCeased == null
                                                    && x.Person.FinancialTo >= term.Year
                                                    && x.TermID == term.ID && x.Class != null)
                                .ToList());
            foreach (var e in enrolments)
            {
                if (e.Course.CourseParticipationTypeID == (int?)ParticipationType.SameParticipantsInAllClasses)
                {
                    foreach (var c in e.Course.Classes)
                    {
                        result.Add(new EnrolmentExportData()
                        {
                            CourseType = e.Course.CourseType.Name,
                            CourseName = e.Course.Name,
                            CourseDescription = e.Course.Description,
                            CourseParticipationType = "Enrolled in all classes",
                            CourseFeePerYear = e.Course.CourseFeePerYear.ToString("c2"),
                            CourseFeePerYearDescription = e.Course.CourseFeePerYearDescription,
                            CourseFeePerTerm = e.Course.CourseFeePerTerm.ToString("c2"),
                            CourseFeePerTermDescription = e.Course.CourseFeePerTermDescription,
                            ClassHeld = c.ClassSummary,
                            Leader = c.LeaderSummary,
                            Venue = c.Venue.Name,
                            VenueLocation = c.Venue.Address,
                            EnrolmentStatus = GetEnrolmentStatus(e, term, settings)
                        });
                    }
                }
                else
                {
                    result.Add(new EnrolmentExportData()
                    {
                        CourseType = e.Course.CourseType.Name,
                        CourseName = e.Course.Name,
                        CourseDescription = e.Course.Description,
                        CourseParticipationType = "Enrolled in all classes",
                        CourseFeePerYear = e.Course.CourseFeePerYear.ToString("c2"),
                        CourseFeePerYearDescription = e.Course.CourseFeePerYearDescription,
                        CourseFeePerTerm = e.Course.CourseFeePerTerm.ToString("c2"),
                        CourseFeePerTermDescription = e.Course.CourseFeePerTermDescription,
                        ClassHeld = e.Class?.ClassSummary,
                        Leader = e.Class?.LeaderSummary,
                        Venue = e.Class?.Venue.Name,
                        VenueLocation = e.Class?.Venue.Address,
                        EnrolmentStatus = GetEnrolmentStatus(e, term, settings)
                    }); ;
                }
            }
            return result;
        }

        public static string GetEnrolmentStatus(Enrolment? enrolment, Term term, SystemSettings settings)
        {
            var result = "Pending";
            if (enrolment != null)
            {
                result = (enrolment.IsWaitlisted) ? "Waitlisted" : "Enrolled";
                if (enrolment.IsWaitlisted && BusinessRule.IsPreRandomAllocationEmailDay(term, settings, TimezoneAdjustment.GetLocalTime()))
                {
                    result += " (Awaiting Random Allocation)";
                }
            }
            return result;
        }
        public static string GetMemberPortalEnrolmentStatus(Enrolment? enrolment, Term term, SystemSettings settings)
        {
            var result = "Pending";
            if (enrolment != null)
            {
                // Display if prior to allocation date irrespective of status
                if (BusinessRule.IsPreRandomAllocationEmailDay(term, settings, TimezoneAdjustment.GetLocalTime()))
                {
                    result = "Waitlisted: (Awaiting Random Allocation)";
                }
                else
                {
                    result = (enrolment.IsWaitlisted) ? "Waitlisted" : "Enrolled";
                }
            }
            return result;
        }

        public static string GetCourseEnrolmentStatus(Course course, List<Enrolment> enrolments)
        {
            string result = "N/A";
            if (course != null && enrolments != null)
            {
                int EnrolledParticipants = enrolments.Where(x => !x.IsWaitlisted).Count();
                int WaitlistedParticipants = enrolments.Where(x => x.IsWaitlisted).Count();
                if (EnrolledParticipants < course.RequiredStudents)
                {
                    result = "Undersubscribed";
                }
                if (EnrolledParticipants >= course.RequiredStudents
                        && EnrolledParticipants <= course.MaximumStudents)
                {
                    result = "Good To Go";
                }
                if (EnrolledParticipants > course.MaximumStudents)
                {
                    result = "Oversubscribed";
                }
                if (EnrolledParticipants < course.MaximumStudents
                        && WaitlistedParticipants > 0)
                {
                    result = "Places Available - Assign Waitlisted";
                }
            }
            return result;
        }
        public static ButtonRenderStyle GetEnrolmentButtonRenderStyle(Course course, List<Enrolment> enrolments)
        {
            ButtonRenderStyle result = ButtonRenderStyle.Light;
            if (course != null && enrolments != null)
            {
                int EnrolledParticipants = enrolments.Where(x => !x.IsWaitlisted).Count();
                int WaitlistedParticipants = enrolments.Where(x => x.IsWaitlisted).Count();
                if (EnrolledParticipants < course.RequiredStudents)
                {
                    result = ButtonRenderStyle.Warning;
                }
                if (EnrolledParticipants >= course.RequiredStudents
                        && EnrolledParticipants <= course.MaximumStudents)
                {
                    result = ButtonRenderStyle.Success;
                }
                if (EnrolledParticipants > course.MaximumStudents)
                {
                    result = ButtonRenderStyle.Danger;
                }
                if (EnrolledParticipants < course.MaximumStudents
                        && WaitlistedParticipants > 0)
                {
                    result = ButtonRenderStyle.Info;
                }
            }
            return result;
        }

        public static async Task<string> DuplicateMarkUpAsync(U3ADbContext dbc,
                                            Enrolment enrolment, Term selectedTerm,
                                            Course selectedCourse, Class selectedClass)
        {
            StringBuilder result = new StringBuilder();
            Enrolment? duplicate = await DuplicateEnrolment(dbc, enrolment, selectedTerm, selectedCourse, selectedClass);
            if (duplicate != null)
            {
                result.AppendLine($"<p>{duplicate.Person.FullName} is already enrolled in this course.<br/>");
                if (duplicate.IsWaitlisted) { result.AppendLine("They are registered as <strong>Waitlisted</strong>.<br/>"); }
                if (duplicate.Course.CourseParticipationTypeID == (int?)ParticipationType.DifferentParticipantsInEachClass &&
                   duplicate.Class != null)
                {
                    result.AppendLine($"They are registered in class <strong>{duplicate.Class.ClassSummary}</strong>.<br/>");
                }
                result.AppendLine("</p>");
            }
            return result.ToString();
        }
        static async Task<Enrolment?> DuplicateEnrolment(U3ADbContext dbc,
                                        Enrolment enrolment, Term selectedTerm,
                                        Course selectedCourse, Class selectedClass)
        {
            if (selectedCourse.CourseParticipationTypeID == (int?)ParticipationType.DifferentParticipantsInEachClass &&
                selectedClass != null)
            {
                return await dbc.Enrolment.AsNoTracking()
                            .Include(x => x.Course)
                            .Include(x => x.Class)
                            .Include(x => x.Term)
                            .Include(x => x.Person)
                            .Where(x => x.ID != enrolment.ID &&
                                        x.PersonID == enrolment.Person.ID &&
                                        x.TermID == selectedTerm.ID &&
                                        x.CourseID == selectedCourse.ID &&
                                        x.ClassID == selectedClass.ID).FirstOrDefaultAsync();
            }
            else
            {
                return await dbc.Enrolment.AsNoTracking()
                            .Include(x => x.Course)
                            .Include(x => x.Term)
                            .Include(x => x.Person)
                            .Where(x => x.ID != enrolment.ID &&
                                        x.PersonID == enrolment.Person.ID &&
                                        x.TermID == selectedTerm.ID &&
                                        x.CourseID == selectedCourse.ID).FirstOrDefaultAsync();
            }
        }
        public static async Task<Enrolment?> DuplicateEnrolment(U3ADbContext dbc,
                                        Person selectedPerson, Term selectedTerm,
                                        Course? selectedCourse, Class selectedClass)
        {
            if (selectedCourse.CourseParticipationTypeID == (int?)ParticipationType.DifferentParticipantsInEachClass &&
                selectedClass != null)
            {
                return await dbc.Enrolment
                            .Include(x => x.Course)
                            .Include(x => x.Class)
                            .Include(x => x.Term)
                            .Include(x => x.Person)
                            .Where(x => x.PersonID == selectedPerson.ID &&
                                        x.TermID == selectedTerm.ID &&
                                        x.CourseID == selectedCourse.ID &&
                                        x.ClassID == selectedClass.ID).FirstOrDefaultAsync();
            }
            else
            {
                return await dbc.Enrolment
                            .Include(x => x.Course)
                            .Include(x => x.Term)
                            .Include(x => x.Person)
                            .Where(x => x.PersonID == selectedPerson.ID &&
                                        x.TermID == selectedTerm.ID &&
                                        x.CourseID == selectedCourse.ID).FirstOrDefaultAsync();
            }
        }

        public static async Task DeleteEnrolmentByClassID(U3ADbContext dbc, Guid ClassID)
        {
            var query = await dbc.Enrolment
                        .Include(x => x.Class)
                        .Where(x => x.ClassID == ClassID).ToArrayAsync();
            dbc.RemoveRange(query);
        }

        /// <summary>
        /// Delete enrolments no longer required by a member.
        /// </summary>
        /// <param name="dbc"></param>
        /// <param name="person"></param>
        /// <param name="term"></param>
        public static async Task<List<Enrolment>> DeleteEnrolmentsRescinded(U3ADbContext dbc,
                                            IEnumerable<Class> DeletedClasses,
                                            Person person,
                                            Term term, Term prevTerm)
        {
            var result = new List<Enrolment>();
            Term thisTerm = term;
            foreach (var c in DeletedClasses)
            {
                if (c.DoNotAllowEdit) continue;
                var termNumber = c.TermNumber;
                thisTerm = (term.TermNumber == termNumber) ? term : prevTerm;
                if (thisTerm.TermNumber != termNumber) { thisTerm = await dbc.Term
                                                                .Where(x => x.Year == term.Year && x.TermNumber == termNumber)
                                                                .FirstOrDefaultAsync(); }
                var course = await dbc.Course.FindAsync(c.CourseID);
                if ((ParticipationType)c.Course.CourseParticipationTypeID == ParticipationType.SameParticipantsInAllClasses)
                {
                    var e = dbc.Enrolment.Where(x =>
                                        x.PersonID == person.ID &&
                                        x.Term.Year == thisTerm.Year && x.Term.TermNumber >= termNumber &&
                                        x.CourseID == c.CourseID).ToImmutableList();
                    if (e.Count > 0)
                    {
                        dbc.RemoveRange(e);
                        result.AddRange(e);
                    }
                }
                else
                {
                    var e = dbc.Enrolment.Where(x =>
                                        x.PersonID == person.ID &&
                                        x.Term.Year == term.Year && x.Term.TermNumber >= termNumber &&
                                        x.CourseID == c.CourseID &&
                                        x.ClassID == c.ID).ToImmutableList();
                    if (e.Count > 0)
                    {
                        dbc.RemoveRange(e);
                        result.AddRange(e);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Add enrolments requested by a member
        /// </summary>
        /// <param name="dbc"></param>
        /// <param name="RequestedClasses"></param>
        /// <param name="person"></param>
        /// <param name="term"></param>
        /// <returns></returns>
        public static async Task<List<Enrolment>> AddEnrolmentRequests(U3ADbContext dbc,
                                            IEnumerable<Class> RequestedClasses,
                                            Person person,
                                            Term term, Term prevTerm)
        {
            var result = new List<Enrolment>();
            foreach (var c in RequestedClasses)
            {
                if (c.DoNotAllowEdit) continue;
                int thisYear;
                int thisTermNo;
                Term thisTerm = term;
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
                    thisTermNo = term.TermNumber;
                    thisTerm = await dbc.Term.AsNoTracking().FirstOrDefaultAsync(x => x.Year == term.Year && x.TermNumber == thisTermNo);
                }
                var course = await dbc.Course.FindAsync(c.CourseID);
                if ((ParticipationType)c.Course.CourseParticipationTypeID == ParticipationType.SameParticipantsInAllClasses)
                {
                    if (!await dbc.Enrolment.AnyAsync(x =>
                                        x.PersonID == person.ID &&
                                        x.Term.Year == thisYear && x.Term.TermNumber == thisTermNo &&
                                        x.CourseID == c.CourseID))
                    {
                        var e = new Enrolment()
                        {
                            Created = DateTime.Now,
                            IsWaitlisted = await BusinessRule.SetWaitlistStatusAsync(dbc, course.ID, thisTerm, person)
                        };
                        if (!BusinessRule.IsClassInTerm(c, thisTermNo)) { e.IsWaitlisted = true; }
                        e.Person = await dbc.Person.FindAsync(person.ID);
                        e.Term = await dbc.Term.FirstOrDefaultAsync(x => x.Year == thisYear && x.TermNumber == thisTermNo);
                        e.Course = await dbc.Course.FindAsync(c.Course.ID);
                        if (c.Course.CourseParticipationTypeID == 1)
                        {
                            e.Class = await dbc.Class.FindAsync(c.ID);
                        }
                        result.Add(e);
                        await dbc.AddAsync<Enrolment>(e);
                    }
                }
                else
                {
                    if (!await dbc.Enrolment.AnyAsync(x =>
                                        x.PersonID == person.ID &&
                                        x.Term.Year == thisYear && x.Term.TermNumber == thisTermNo &&
                                        x.CourseID == c.CourseID &&
                                        x.ClassID == c.ID))
                    {
                        var e = new Enrolment()
                        {
                            Created = DateTime.Now,
                            IsWaitlisted = await BusinessRule.SetWaitlistStatusAsync(dbc, course.ID, c.ID, thisTerm, person)
                        };
                        e.Person = await dbc.Person.FindAsync(person.ID);
                        e.Term = await dbc.Term.FirstOrDefaultAsync(x => x.Year == thisYear && x.TermNumber == thisTermNo);
                        e.Course = await dbc.Course.FindAsync(c.Course.ID);
                        e.Class = await dbc.Class.FindAsync(c.ID);
                        result.Add(e);
                        await dbc.AddAsync<Enrolment>(e);
                    }
                }
            }
            return result;
        }

        // Same participants in all classes
        public static async Task<bool> SetWaitlistStatusAsync(U3ADbContext dbc,
                                    Guid CourseID,
                                    Term CurrentTerm,
                                    Person person)
        {
            bool result = false;
            if (person.FinancialTo < CurrentTerm.Year) return true;
            if (!CurrentTerm.IsClassAllocationFinalised) return true;

            // Otherwise, set enrolled if enrolled count less than Max count
            var course = await dbc.Course.FindAsync(CourseID);
            if (!course.AllowAutoEnrol) return true;

            int enrolments = await dbc.Enrolment
                                .Where(x => x.CourseID == CourseID &&
                                            x.TermID == CurrentTerm.ID &&
                                            !x.IsWaitlisted).CountAsync();
            result = (enrolments >= course.MaximumStudents);
            return result;
        }

        // Different participants in each class
        public static async Task<bool> SetWaitlistStatusAsync(U3ADbContext dbc,
                                    Guid CourseID,
                                    Guid ClassID,
                                    Term CurrentTerm,
                                    Person person)
        {
            bool result = false;
            if (person.FinancialTo < CurrentTerm.Year) return true;
            if (!CurrentTerm.IsClassAllocationFinalised) return true;

            // Otherwise, set enrolled if enrolled count less than Max count
            var course = await dbc.Course.FindAsync(CourseID);
            int enrolments = await dbc.Enrolment
                                .Where(x => x.CourseID == CourseID && x.ClassID == ClassID &&
                                                x.TermID == CurrentTerm.ID &&
                                                !x.IsWaitlisted).CountAsync();
            result = (enrolments >= course.MaximumStudents);
            return result;
        }

        static int? GetFirstTermNumber(Term currentTerm, Class requestedClass)
        {
            int? result = null;
            var termNumber = currentTerm.TermNumber;
            var numbers = new List<int>();
            if (requestedClass.OfferedTerm1 && termNumber == 1) numbers.Add(1);
            if (requestedClass.OfferedTerm2 && termNumber <= 2) numbers.Add(2);
            if (requestedClass.OfferedTerm3 && termNumber <= 3) numbers.Add(3);
            if (requestedClass.OfferedTerm4 && termNumber <= 4) numbers.Add(4);
            if (numbers.Any()) result = numbers.Min();
            return result;
        }

        public static async Task<string> GetEnrolmentStatusMarkup(U3ADbContext dbc,
                IEnumerable<Enrolment> enrolments, IEnumerable<Enrolment> deletions, Term term, SystemSettings settings)
        {
            var result = new StringBuilder();
            if (enrolments.Count() > 0)
            {
                createEnrolmentSummaryTable(result, "Course Requested");
                foreach (var e in enrolments)
                {
                    var status = GetEnrolmentStatus(e, term, settings);
                    result.AppendLine($"<tr><td>{e.Course.Name}</td><td>{status}</td></tr>");
                }
                result.AppendLine("</tbody></table>");
            }
            if (deletions.Count() > 0)
            {
                createEnrolmentSummaryTable(result, "Course Removed");
                foreach (var e in deletions)
                {
                    result.AppendLine($"<tr><td>{e.Course.Name}</td><td>Removed</td></tr>");
                }
                result.AppendLine("</tbody></table>");
            }
            return result.ToString();

            static void createEnrolmentSummaryTable(StringBuilder result, string header)
            {
                result.AppendLine("<table class='table'>");
                result.AppendLine("<thead><tr>");
                result.AppendLine($"<th scope='col'>{header}</th>");
                result.AppendLine("<th scope='col'>Status</th>");
                result.AppendLine("</tr></thead>");
                result.AppendLine("<tbody>");
            }
        }

        public static async Task<DateTime?> IsEnrolmentDayLockoutPeriod(U3ADbContext dbc, DateTime Today)
        {
            DateTime? result = null;
            var term = BusinessRule.CurrentEnrolmentTerm(dbc);
            if (term == null)
            {
                term = await BusinessRule.CurrentTermAsync(dbc);
            }

            //Special case: enrolment day
            if (term.TermNumber == 4)
            {
                Term? nextTerm = default;
                nextTerm = await BusinessRule.GetFirstTermNextYearAsync(dbc, term);
                if (nextTerm != null)
                {
                    var timeDiff = nextTerm.EnrolmentStartDate.Date - Today.Date;
                    if (nextTerm.TermNumber == 1 && timeDiff.TotalHours < 12 && timeDiff.TotalHours > 0)
                    {
                        result = Today.Date;
                    }
                }
            }
            return result;
        }

    }

}
