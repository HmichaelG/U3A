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
        public static async Task<List<Enrolment>> EditableEnrolmentsAsync(U3ADbContext dbc, Term SelectedTerm,
                              Course SelectedCourse, Class SelectedClass)
        {
            List<Enrolment> enrolments = null;
            if (SelectedClass == null || SelectedCourse.CourseParticipationTypeID == (int?)ParticipationType.SameParticipantsInAllClasses)
            {
                enrolments = await dbc.Enrolment.IgnoreQueryFilters()
                                    .Include(x => x.Term)
                                    .Include(x => x.Course)
                                    .Include(x => x.Person)
                                    .Where(x => !x.IsDeleted && x.CourseID == SelectedCourse.ID
                                                        && x.TermID == SelectedTerm.ID
                                                        && x.Person.DateCeased == null).ToListAsync();
            }
            else
            {
                enrolments = await dbc.Enrolment.IgnoreQueryFilters()
                                    .Include(x => x.Term)
                                    .Include(x => x.Course)
                                    .Include(x => x.Person)
                                    .Where(x => !x.IsDeleted && x.ClassID == SelectedClass.ID
                                                    && x.TermID == SelectedTerm.ID
                                                    && x.Person.DateCeased == null).ToListAsync();
            }
            return enrolments.OrderBy(x => x.IsWaitlisted && !x.IsDeleted)
                                                .ThenBy(x => x.WaitlistSort)
                                                .ThenBy(x => x.Person.LastName)
                                                .ThenBy(x => x.Person.FirstName)
                                    .ToList();

        }

        public static async Task<List<Enrolment>> GetEnrolmentIncludeLeadersAsync(U3ADbContext dbc,
                                                  Course thisCourse, Class thisClass, Term thisTerm)
        {
            var enrolments = new List<Enrolment>();
            var testTerm = thisTerm;
            var termNumber = GetNextTermOffered(thisClass, thisTerm.TermNumber);
            if (testTerm.TermNumber != termNumber)
            {
                testTerm = await dbc.Term.FirstOrDefaultAsync(x => x.Year == thisTerm.Year
                                            && x.TermNumber == termNumber);
            }
            if (testTerm == null) { return enrolments; }
            if (await dbc.Enrolment.AnyAsync(x => x.ClassID == thisClass.ID && x.TermID == testTerm.ID))
            {
                enrolments = await dbc.Enrolment.AsNoTracking().IgnoreQueryFilters()
                                          .Include(x => x.Person)
                                          .Include(x => x.Course)
                                          .Where(x => !x.IsDeleted && x.ClassID == thisClass.ID
                                                    && x.TermID == testTerm.ID).ToListAsync();
            }
            else
            {
                enrolments = await dbc.Enrolment.AsNoTracking().IgnoreQueryFilters()
                                            .Include(x => x.Person)
                                            .Include(x => x.Course)
                                            .Where(x => !x.IsDeleted && x.CourseID == thisCourse.ID
                                                            && x.TermID == testTerm.ID).ToListAsync();
            };
            Enrolment? dummy;
            if (enrolments.Count > 0)
            {
                var template = enrolments[0];
                dummy = await CreateDummyLeaderEnrolment(dbc, template, thisClass.LeaderID);
                if (dummy != null) enrolments.Add(dummy);
                dummy = await CreateDummyLeaderEnrolment(dbc, template, thisClass.Leader2ID);
                if (dummy != null) enrolments.Add(dummy);
                dummy = await CreateDummyLeaderEnrolment(dbc, template, thisClass.Leader3ID);
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
            return await dbc.Dropout.IgnoreQueryFilters()
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

        public static async Task<bool> AnyEnrolmentsInYear(U3ADbContext dbc, Guid courseID)
        {
            bool result = await dbc.Enrolment.AsNoTracking()
                                .Include(x => x.Course)
                                .Include(x => x.Term)
                                .AnyAsync(x => x.CourseID == courseID);
            return result;
        }

        public static List<Enrolment> SelectableEnrolments(U3ADbContext dbc, Term SelectedTerm)
        {
            var enrolments = dbc.Enrolment.IgnoreQueryFilters()
                                .Include(x => x.Term)
                                .Include(x => x.Course).ThenInclude(x => x.Classes)
                                .Include(x => x.Class).ThenInclude(x => x.OnDay)
                                .Include(x => x.Person)
                                .Where(x => !x.IsDeleted && !x.Person.IsDeleted && x.TermID == SelectedTerm.ID
                                                    && x.Person.DateCeased == null)
                                .OrderBy(x => x.IsWaitlisted)
                                            .ThenBy(x => x.Person.LastName)
                                            .ThenBy(x => x.Person.FirstName)
                                .AsEnumerable().Where(x => IsCourseInTerm(x.Course, SelectedTerm)).ToList();
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
            List<Enrolment> result;
            if (SelectedClass == null || SelectedCourse.CourseParticipationTypeID == (int?)ParticipationType.SameParticipantsInAllClasses)
            {
                result = dbc.Enrolment.IgnoreQueryFilters().AsSplitQuery()
                                    .Include(x => x.Term)
                                    .Include(x => x.Course)
                                    .Include(x => x.Person)
                                    .Where(x => !x.IsDeleted && !x.Person.IsDeleted 
                                                        && x.ClassID == null
                                                        && x.CourseID == SelectedCourse.ID
                                                        && x.TermID == SelectedTerm.ID
                                                        && x.Person.DateCeased == null)
                                    .OrderBy(x => x.IsWaitlisted)
                                                .ThenBy(x => x.Person.LastName)
                                                .ThenBy(x => x.Person.FirstName)
                                    .ToList();
            }
            else
            {
                result = dbc.Enrolment.IgnoreQueryFilters()
                                    .Include(x => x.Term)
                                    .Include(x => x.Course)
                                    .Include(x => x.Person)
                                    .Where(x => !x.IsDeleted && !x.Person.IsDeleted 
                                                    && x.ClassID == SelectedClass.ID
                                                    && x.TermID == SelectedTerm.ID
                                                    && x.Person.DateCeased == null)
                                    .OrderBy(x => x.IsWaitlisted)
                                                .ThenBy(x => x.Person.LastName)
                                                .ThenBy(x => x.Person.FirstName)
                                    .ToList();
            }
            return result;
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
                            EnrolmentStatus = GetEnrolmentStatus(e, term, settings, dbc.GetLocalTime())
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
                        EnrolmentStatus = GetEnrolmentStatus(e, term, settings, dbc.GetLocalTime())
                    }); ;
                }
            }
            return result;
        }

        public static string GetEnrolmentStatus(Enrolment? enrolment, Term term, SystemSettings settings, DateTime localTime)
        {
            var result = "Pending";
            if (enrolment != null)
            {
                result = (enrolment.IsWaitlisted) ? "Waitlisted" : "Enrolled";
                if (enrolment.IsWaitlisted && BusinessRule.IsPreRandomAllocationEmailDay(term, settings, localTime))
                {
                    result += " (Awaiting Random Allocation)";
                }
                else if (enrolment.IsWaitlisted)
                {
                    if (!IsAllocationDone(enrolment.Created))
                    {
                        result = "Waitlisted: (Pending Allocation)";
                    }
                }
            }
            return result;
        }
        public static string GetMemberPortalEnrolmentStatus(Class Class, Enrolment? enrolment, Term term, SystemSettings settings, DateTime localTime)
        {
            var result = "Pending";
            if (enrolment != null)
            {
                // Display if prior to allocation date irrespective of status
                if (BusinessRule.IsPreRandomAllocationEmailDay(term, settings, localTime))
                {
                    if (Class.TermNumber >= term.TermNumber)
                    {
                        result = "Waitlisted: (Awaiting Random Allocation)";
                    }
                    else
                    {
                        result = (enrolment.IsWaitlisted) ? "Waitlisted" : "Enrolled";
                    }
                }
                else
                {
                    result = (enrolment.IsWaitlisted) ? "Waitlisted" : "Enrolled";
                    if (enrolment.IsWaitlisted)
                    {
                        if (!IsAllocationDone(enrolment.Created))
                        {
                            result = "Waitlisted: (Pending Allocation)";
                        }
                    }
                }
            }
            return result;
        }

        private static bool IsAllocationDone(DateTime EnrolmentCreated)
        {
            // backgorund processing to occur every hour on the hour
            var now = DateTime.UtcNow;
            var minute = now.Minute;
            var second = now.Second;
            var lastProcessed = now.AddMinutes(-minute).AddSeconds(-second);
            // do the test
            return (EnrolmentCreated <= lastProcessed) ? true : false;
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
                if (course.IsOffScheduleActivity)
                {
                    result = "Off-Schedule Activity";
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
                if (course.IsOffScheduleActivity)
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
            var deletions = await dbc.Enrolment
                        .Include(x => x.Class)
                        .Where(x => x.ClassID == ClassID).ToArrayAsync();
            foreach (var d in deletions) { DeleteEnrolment(dbc, d); }
        }
        public static void DeleteEnrolment(U3ADbContext dbc, Enrolment enrolment)
        {
            // delete the enrolments
            var query = dbc.Enrolment
                        .Include(x => x.Term)
                        .Where(x => x.CourseID == enrolment.CourseID
                                    && (x.ClassID == null || x.ClassID == enrolment.ClassID)
                                    && x.PersonID == enrolment.PersonID).AsEnumerable()
                                    .Where(x => x.Term.Year >= enrolment.Term.Year).ToList();
            dbc.RemoveRange(query);
            // delete future attendance records
            var query2 = dbc.AttendClass
                        .Where(x => x.ClassID == enrolment.ClassID
                                    && x.PersonID == enrolment.PersonID).AsEnumerable()
                                    .Where(x => x.Date >= dbc.GetLocalTime()).ToList();
            dbc.RemoveRange(query2);
        }

        public static async Task DeleteEnrolmentsRescinded(U3ADbContext dbc,
                                            TenantDbContext dbT,
                                            IEnumerable<Class> DeletedClasses,
                                            Person person,
                                            Term term, Term prevTerm)
        {
            List<Enrolment> result = new();
            List<Class> thisCampus = new();
            List<Class> multiCampus = new();
            foreach (var c in DeletedClasses)
            {
                if (await dbc.Class.AnyAsync(x => x.ID == c.ID))
                {
                    thisCampus.Add(c);
                }
                else
                {
                    multiCampus.Add(c);
                }
            }
            if (thisCampus.Count > 0)
            {
                await DeleteEnrolmentsRescinded(dbc, thisCampus, person, term, prevTerm);
            }
            if (multiCampus.Count > 0)
            {
                await DeleteMultiCampusEnrolmentsRescinded(dbT, multiCampus, person, term, prevTerm);
            }
        }

        /// <summary>
        /// Delete enrolments no longer required by a member.
        /// </summary>
        /// <param name="dbc"></param>
        /// <param name="person"></param>
        /// <param name="term"></param>
        private static async Task DeleteEnrolmentsRescinded(U3ADbContext dbc,
                                            IEnumerable<Class> DeletedClasses,
                                            Person person,
                                            Term term, Term prevTerm)
        {
            foreach (var c in DeletedClasses)
            {
                if ((ParticipationType)c.Course.CourseParticipationTypeID == ParticipationType.SameParticipantsInAllClasses)
                {
                    var deletion = await dbc.Enrolment
                                        .Include(x => x.Term)
                                        .Where(x =>
                                            x.PersonID == person.ID &&
                                            x.Term.Year == term.Year &&
                                            x.CourseID == c.CourseID).FirstOrDefaultAsync();
                    if (deletion != null) { DeleteEnrolment(dbc, deletion); }
                }
                else
                {
                    var deletion = await dbc.Enrolment
                                    .Include(x => x.Term)
                                    .Where(x =>
                                        x.PersonID == person.ID &&
                                        x.Term.Year == term.Year &&
                                        x.CourseID == c.CourseID &&
                                        x.ClassID == c.ID).FirstOrDefaultAsync();
                    if (deletion != null) { DeleteEnrolment(dbc, deletion); }
                }
            }
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
                                            TenantDbContext dbT,
                                            IEnumerable<Class> RequestedClasses,
                                            Person person,
                                            Term term, Term prevTerm)
        {
            List<Enrolment> result = new();
            List<Class> thisCampus = new();
            List<Class> multiCampus = new();
            foreach (var c in RequestedClasses)
            {
                if (await dbc.Class.AnyAsync(x => x.ID == c.ID))
                {
                    thisCampus.Add(c);
                }
                else
                {
                    multiCampus.Add(c);
                }
            }
            if (thisCampus.Count > 0)
            {
                result = await AddEnrolmentRequests(dbc, thisCampus, person, term, prevTerm);
            }
            if (multiCampus.Count > 0)
            {
                result.AddRange(await AddMultiCampusEnrolmentRequests(dbT, multiCampus, person, term, prevTerm));
            }
            return result;
        }
        private static async Task<List<Enrolment>> AddEnrolmentRequests(U3ADbContext dbc,
                                            IEnumerable<Class> RequestedClasses,
                                            Person person,
                                            Term term, Term prevTerm)
        {
            var result = new List<Enrolment>();
            foreach (var c in RequestedClasses)
            {
                bool isFutureTerm = false;
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
                    thisTermNo = c.TermNumber;
                    isFutureTerm = true;
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
                            IsWaitlisted = true
                        };
                        e.Person = await dbc.Person.FindAsync(person.ID);
                        e.Term = await dbc.Term.FirstOrDefaultAsync(x => x.Year == thisYear && x.TermNumber == thisTermNo);
                        e.Course = await dbc.Course.FindAsync(c.Course.ID);
                        result.Add(e);
                        c.Course.Enrolments.Add(e);
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
                            IsWaitlisted = true
                        };
                        e.Person = await dbc.Person.FindAsync(person.ID);
                        e.Term = await dbc.Term.FirstOrDefaultAsync(x => x.Year == thisYear && x.TermNumber == thisTermNo);
                        e.Course = await dbc.Course.FindAsync(c.Course.ID);
                        e.Class = await dbc.Class.FindAsync(c.ID);
                        result.Add(e);
                        c.Course.Enrolments.Add(e);
                        await dbc.AddAsync<Enrolment>(e);
                    }
                }
            }
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
                IEnumerable<Enrolment> enrolments, Term term, SystemSettings settings)
        {
            var localTime = dbc.GetLocalTime();
            var result = new StringBuilder();
            if (enrolments.Count() > 0)
            {
                createEnrolmentSummaryTable(result, "Course Requested");
                foreach (var e in enrolments)
                {
                    var status = GetEnrolmentStatus(e, term, settings, localTime);
                    result.AppendLine($"<tr><td>{e.Course.Name}</td><td>{status}</td></tr>");
                }
                result.AppendLine("</tbody></table>");
                var processMsg = "Pending allocations are processed hourly";
                if (IsEnrolmentBlackoutPeriod(settings))
                {
                    var processDate = dbc.GetLocalTime(settings.EnrolmentBlackoutEndsUTC.Value).ToString(constants.STD_DATETIME_FORMAT);
                    processMsg = $"Pending allocations will be processed on or after<br/>{processDate}";
                }
                result.AppendLine($"<div class='alert alert-info text-center'>{processMsg}</div>");
            }
            return result.ToString();
        }
        static void createEnrolmentSummaryTable(StringBuilder result, string header)
        {
            result.AppendLine("<table class='table'>");
            result.AppendLine("<thead><tr>");
            result.AppendLine($"<th scope='col'>{header}</th>");
            result.AppendLine("<th scope='col'>Status</th>");
            result.AppendLine("</tr></thead>");
            result.AppendLine("<tbody>");
        }

        public static async Task<DateTime?> IsEnrolmentDayLockoutPeriod(U3ADbContext dbc, DateTime Today)
        {
            DateTime? result = null;
            var term = await BusinessRule.CurrentEnrolmentTermAsync(dbc);
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
