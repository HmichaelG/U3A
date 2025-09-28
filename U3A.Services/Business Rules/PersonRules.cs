using DevExpress.Blazor.RichEdit;
using Eway.Rapid.Abstractions.Response;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Twilio.TwiML.Fax;
using U3A.Database;
using U3A.Model;
using U3A.Services;

namespace U3A.BusinessRules;

public static partial class BusinessRule
{

    public static async Task<bool> CanDelete(U3ADbContext dbc, Person person)
    {
        // return false if person has any transactions
        bool result = true;
        if (await dbc.Receipt.AnyAsync(x => x.PersonID == person.ID)) { result = false; }
        if (await dbc.Fee.AnyAsync(x => x.PersonID == person.ID)) { result = false; }
        if (await dbc.Enrolment.AnyAsync(x => x.PersonID == person.ID)) { result = false; }
        if (await dbc.AttendClass.AnyAsync(x => x.PersonID == person.ID)) { result = false; }
        if (await dbc.Committee.AnyAsync(x => x.PersonID == person.ID)) { result = false; }
        if (await dbc.Volunteer.AnyAsync(x => x.PersonID == person.ID)) { result = false; }
        return result;
    }
    public static async Task<List<Person>> EditablePersonAsync(U3ADbContext dbc)
    {
        var people = dbc.Person
                        .AsEnumerable()
                        .OrderBy(x => x.LastName)
                        .ThenBy(x => x.FirstName)
                        .ThenBy(x => x.Email).ToList();
        await ApplyGroupsAsync(dbc, people);
        return people;
    }

    public static async Task<List<Person>> EditableDeletedPersonsAsync(U3ADbContext dbc, int CurrentYear)
    {
        var people = dbc.Person.IgnoreQueryFilters()
                        .Include(x => x.Enrolments)
                        .Include(x => x.Receipts)
                        .Where(x => x.IsDeleted && EF.Property<string>(x, "Discriminator") == "Person" &&
                                        (x.FinancialTo >= CurrentYear - 1 || x.FinancialTo == constants.START_OF_TIME))
                        .OrderBy(x => x.LastName)
                        .ThenBy(x => x.FirstName)
                        .ThenBy(x => x.Email).ToList();
        return people;
    }

    public static Person SelectPerson(U3ADbContext dbc, Guid ID)
    {
        var person = dbc.Person
                        .IgnoreQueryFilters()
                        .FirstOrDefault(x => x.ID == ID) ?? throw new ArgumentNullException(nameof(Person));
        ApplyGroups(dbc, person);
        return person;
    }
    public static async Task<Person> SelectPersonAsync(U3ADbContext dbc, Guid ID)
    {
        var person = await dbc.Person
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(x => x.ID == ID) ?? throw new ArgumentNullException(nameof(Person));
        await ApplyGroupsAsync(dbc, person);
        return person;
    }

    public static async Task<List<Person>> SelectablePersonsAsync(U3ADbContext dbc, Term term)
    {
        var people = await dbc.Person.AsNoTracking()
                        .Where(x => x.DateCeased == null)
                        .OrderBy(x => x.LastName)
                        .ThenBy(x => x.FirstName)
                        .ThenBy(x => x.Email).ToListAsync();
        await ApplyGroupsAsync(dbc, people);
        return people.Where(x => x.FinancialTo >= term.Year - 1).ToList();
    }
    public static async Task<List<Person>> SelectableFinancialPeopleAsync(U3ADbContext dbc)
    {
        var currentTerm = await dbc.Term.AsNoTracking()
                            .OrderByDescending(x => x.IsDefaultTerm)
                            .FirstOrDefaultAsync();
        if (currentTerm == null) { return null; }
        var people = await dbc.Person.AsNoTracking()
                        .Where(x => x.DateCeased == null
                                    && x.FinancialTo >= currentTerm.Year)
                        .OrderBy(x => x.LastName)
                        .ThenBy(x => x.FirstName)
                        .ThenBy(x => x.Email).ToListAsync();
        await ApplyGroupsAsync(dbc, people);
        return people;
    }
    public static async Task<List<Person>> SelectableFinancialPersonAsync(U3ADbContext dbc, Guid PersonID)
    {
        var currentTerm = await dbc.Term.AsNoTracking()
                            .OrderByDescending(x => x.IsDefaultTerm)
                            .FirstOrDefaultAsync();
        if (currentTerm == null) { return null; }
        var people = await dbc.Person.AsNoTracking()
                        .Where(x => x.ID == PersonID
                                    && x.FinancialTo >= currentTerm.Year)
                        .OrderBy(x => x.LastName)
                        .ThenBy(x => x.FirstName)
                        .ThenBy(x => x.Email).ToListAsync();
        await ApplyGroupsAsync(dbc, people);
        return people;
    }
    public static async Task<List<Person>> SelectablePersonsNotEnrolledAsync(U3ADbContext dbc,
                                            Term CurrentTerm,
                                            DateTime FromDate,
                                            DateTime ToDate)
    {
        return await dbc.Person.AsNoTracking()
                        .Include(x => x.Enrolments)
                        .Where(x => x.DateCeased == null
                                        && (x.DateJoined <= FromDate
                                        && x.DateJoined >= ToDate)
                                        && x.FinancialTo >= CurrentTerm.Year
                                        && !x.Enrolments.Any())
                        .OrderBy(x => x.LastName)
                        .ThenBy(x => x.FirstName)
                        .ThenBy(x => x.Email).ToListAsync();
    }
    public static async Task<List<Person>> SelectableNewPersonsWaitlistedOnlyAsync(U3ADbContext dbc,
                                            Term CurrentTerm,
                                            DateTime FromDate,
                                            DateTime ToDate)
    {
        return await dbc.Person.AsNoTracking()
                        .Include(x => x.Enrolments)
                        .Where(x => x.DateCeased == null
                                        && (x.DateJoined <= FromDate
                                        && x.DateJoined >= ToDate)
                                        && x.FinancialTo >= CurrentTerm.Year
                                        && x.Enrolments.Count() == x.Enrolments.Count(e => e.IsWaitlisted))
                        .OrderBy(x => x.LastName)
                        .ThenBy(x => x.FirstName)
                        .ThenBy(x => x.Email).ToListAsync();
    }
    public static List<Person> GetPostalPersons(U3ADbContext dbc, Term term, bool includeUnfinancial)
    {
        var finToYear = (includeUnfinancial) ? term.Year - 1 : term.Year;
        var people = dbc.Person.AsNoTracking()
                        .AsEnumerable()
                        .Where(x => x.DateCeased == null && x.Communication != "Email")
                        .OrderBy(x => x.LastName)
                        .ThenBy(x => x.FirstName);
        foreach (var p in people) ApplyGroups(dbc, p);
        return people.Where(x => x.FinancialTo >= finToYear).ToList();
    }
    public static async Task<List<Person>> SelectablePersonsIncludeUnfinancialAsync(U3ADbContext dbc)
    {
        var people = await dbc.Person
                        .AsNoTracking()
                        .Where(x => x.DateCeased == null)
                        .OrderBy(x => x.LastName)
                        .ThenBy(x => x.FirstName)
                        .ThenBy(x => x.Email).ToListAsync();
        await ApplyGroupsAsync(dbc, people);
        return people;
    }
    public static List<Person> SelectableLeaders(U3ADbContext dbc, Guid TermID)
    {
        var term = dbc.Term.Find(TermID);
        if (term == null) { return new List<Person> { }; }
        var people = dbc.Person.AsNoTracking().IgnoreQueryFilters()
                        .Where(x => !x.IsDeleted && x.DateCeased == null
                                && (x.LeaderOf.Any() || x.Leader2Of.Any() || x.Leader3Of.Any())).ToList();
        foreach (var person in people)
        {
            person.LeaderOf = dbc.Class.IgnoreQueryFilters()
                        .Include(x => x.Course).ThenInclude(x => x.CourseParticipationType)
                        .Include(x => x.Course).ThenInclude(x => x.Classes)
                        .Include(x => x.Venue)
                        .Include(x => x.OnDay)
                        .Include(x => x.Leader)
                        .Include(x => x.Leader2)
                        .Include(x => x.Leader3)
                        .Include(x => x.Occurrence)
                        .Where(x => !x.IsDeleted && (x.LeaderID == person.ID || x.Leader2ID == person.ID || x.Leader3ID == person.ID) &&
                                        x.Course.Year == term.Year)
                                        .AsEnumerable()
                                        .Where(x => IsClassInTerm(x, term.TermNumber))
                        .AsEnumerable().Where(x => IsCourseInTerm(x.Course, term)).ToList();
            foreach (var c in person.LeaderOf)
            {
                c.Enrolments = BusinessRule.SelectableEnrolmentsByClass(dbc, c, term, c.Course);
            }
        }
        return people.Where(x => x.LeaderOf.Any()).ToList();
    }

    public static List<Person> SelectableClerks(U3ADbContext dbc, Guid TermID)
    {
        var term = dbc.Term.Find(TermID);
        if (term == null) { return new List<Person> { }; }
        var clerkEnrolments = dbc.Enrolment.AsNoTracking()
                        .Include(x => x.Course)
                        .Include(x => x.Course).ThenInclude(x => x.CourseParticipationType)
                        .Include(x => x.Course).ThenInclude(c => c.Classes)
                        .Include(x => x.Class)
                        .Include(x => x.Person)
                        .Where(x => x.TermID == TermID && x.IsCourseClerk)
                        .AsEnumerable().Where(x => IsCourseInTerm(x.Course, term)).ToList();
        foreach (var e in clerkEnrolments)
        {
            var person = e.Person;
            if ((ParticipationType)e.Course.CourseParticipationTypeID == ParticipationType.SameParticipantsInAllClasses)
            {
                person.LeaderOf.AddRange(e.Course.Classes);
            }
            else
            {
                person.LeaderOf.Add(e.Class);
            }
        }
        var venues = dbc.Venue.AsNoTracking();
        var day = dbc.WeekDay.AsNoTracking();
        var occurrence = dbc.Occurrence.AsNoTracking();
        foreach (var e in clerkEnrolments)
        {
            foreach (var c in e.Person.LeaderOf)
            {
                if (c != null)
                {
                    c.Occurrence = occurrence.FirstOrDefault(x => x.ID == c.OccurrenceID);
                    c.Venue = venues.FirstOrDefault(x => x.ID == c.VenueID);
                    c.OnDay = day.FirstOrDefault(x => x.ID == c.OnDayID);
                }
            }
        }
        foreach (var e in clerkEnrolments)
        {
            foreach (var c in e.Person.LeaderOf)
            {
                if (c != null)
                {
                    c.Course = dbc.Course.Find(c.CourseID);
                    c.Enrolments.AddRange(BusinessRule.SelectableEnrolmentsByClass(dbc, c, term, c.Course));
                }
            }
        }
        return clerkEnrolments.Select(x => x.Person).ToList();
    }

    public static async Task<bool> IsLeaderOrClerk(U3ADbContext dbc, Person person, Term term)
    {
        bool isLeader = IsCourseLeader(dbc, person, term);
        bool isClerk = await IsCourseClerkAsync(dbc, person, term);
        return isLeader || isClerk;
    }

    public static bool IsCourseLeader(U3ADbContext dbc, Person person, Term term)
    {
        return dbc.Class
                        .Include(x => x.Course).AsEnumerable()
                        .Any(x => (x.LeaderID == person.ID ||
                                        x.Leader2ID == person.ID ||
                                        x.Leader3ID == person.ID) &&
                                        x.Course.Year == term.Year && IsClassInYear(dbc, x));
    }
    public static bool IsCourseLeader(U3ADbContext dbc, Person person, DateTime LocalNow)
    {
        var result = false;
        Term term = BusinessRule.CurrentEnrolmentTerm(dbc, LocalNow);
        var defaultTerm = dbc.Term.AsNoTracking().FirstOrDefault(x => x.IsDefaultTerm);
        return dbc.Class
                        .Include(x => x.Course).AsEnumerable()
                        .Any(x => (x.LeaderID == person.ID ||
                                        x.Leader2ID == person.ID ||
                                        x.Leader3ID == person.ID) &&
                                        x.Course.Year == term.Year && IsClassInRemainingYear(dbc, x, term, defaultTerm));
    }
    public static async Task<bool> IsCourseClerkAsync(U3ADbContext dbc, Person person, Term term)
    {
        return await dbc.Enrolment
                        .Include(x => x.Term)
                        .AnyAsync(x => x.Term.Year == term.Year && x.Term.TermNumber >= term.TermNumber &&
                                        x.IsCourseClerk &&
                                        x.PersonID == person.ID &&
                                        !x.IsWaitlisted);

    }

    public static List<Person> SelectablePersonsWithEnrolments(U3ADbContext dbc, Guid TermID, bool? WaitlistStatus = null)
    {
        var term = dbc.Term.Find(TermID);
        if (term == null) { return new List<Person> { }; }
        var Classes = dbc.Class.AsNoTracking().AsSplitQuery()
                    .Include(x => x.Course).ThenInclude(x => x.CourseParticipationType)
                    .Include(x => x.Venue)
                    .Include(x => x.OnDay)
                    .Include(x => x.Leader)
                    .Include(x => x.Occurrence)
                    .Where(x => x.Course.Year == term.Year).ToList();
        foreach (var c in Classes)
        {
            BusinessRule.AssignClassCounts(term, c);
        }
        var people = dbc.Person.IgnoreQueryFilters()
                        .Where(x => !x.IsDeleted && x.DateCeased == null).AsNoTracking().ToList();
        var enrolments = dbc.Enrolment.IgnoreQueryFilters().AsSplitQuery()
            .Include(x => x.Term)
            .Include(x => x.Course).ThenInclude(x => x.CourseParticipationType)
            .Include(x => x.Course).ThenInclude(x => x.Classes)
            .Where(x => !x.IsDeleted && x.TermID == TermID
                        && (WaitlistStatus == null || x.IsWaitlisted == WaitlistStatus))
                        .AsEnumerable().Where(x => IsCourseInTerm(x.Course, x.Term)).ToList();


        foreach (var e in enrolments)
        {
            e.Person = people.FirstOrDefault(x => x.ID == e.PersonID);
            if (e.ClassID != null)
            {
                e.Class = Classes.FirstOrDefault(x => x.ID == e.ClassID);
            }
        }

        foreach (var person in people)
        {
            person.Enrolments = enrolments.Where(e => e.PersonID == person.ID).ToList();
            person.EnrolledClasses.Clear();
            foreach (var e in person.Enrolments)
            {
                if (e.Class == null)
                {
                    foreach (var c in Classes.Where(x => e.CourseID == x.CourseID))
                    {
                        var newClass = new Class();
                        c.CopyTo(newClass);
                        newClass.Course = c.Course;
                        newClass.Venue = c.Venue;
                        newClass.OnDay = c.OnDay;
                        newClass.Occurrence = c.Occurrence;
                        newClass.Leader = c.Leader;
                        newClass.IsSelected = !e.IsWaitlisted;
                        newClass.IsSelectedByEnrolment = (newClass.IsSelected) ? e : null;
                        person.EnrolledClasses.Add(newClass);
                    }
                }
                else
                {
                    var newClass = new Class();
                    e.Class.CopyTo(newClass);
                    newClass.Course = e.Class.Course;
                    newClass.Venue = e.Class.Venue;
                    newClass.OnDay = e.Class.OnDay;
                    newClass.Occurrence = e.Class.Occurrence;
                    newClass.Leader = e.Class.Leader;
                    newClass.IsSelected = !e.IsWaitlisted;
                    newClass.IsSelectedByEnrolment = (newClass.IsSelected) ? e : null;
                    person.EnrolledClasses.Add(newClass);
                }
            }
        }
        return people.Where(x => x.EnrolledClasses.Any()).ToList();
    }

    async static Task ApplyGroupsAsync(U3ADbContext dbc, List<Person> people)
    {
        Term? term = await BusinessRule.CurrentEnrolmentTermAsync(dbc);
        if (term == null) term = await BusinessRule.CurrentTermAsync(dbc);
        if (term != null)
        {
            foreach (var e in await dbc.Enrolment.AsNoTracking()
                                        .Where(x => x.Term.Year == term.Year &&
                                                x.IsCourseClerk && !x.IsWaitlisted).ToListAsync())
            {
                var p = people.Find(x => x.ID == e.PersonID);
                if (p != null) p.IsCourseClerk = true;
            }
            foreach (var l in await dbc.Class.AsNoTracking()
                .Where(x => x.LeaderID != null && x.Course.Year == term.Year)
                .Select(x => x.LeaderID).ToListAsync())
            {
                var p = people.Find(x => x.ID == l);
                if (p != null)
                {
                    p.IsCourseLeader = true;
                    if (p.FinancialTo < term.Year) p.FinancialTo = term.Year;
                }
            }
            ;
            foreach (var l in await dbc.Class.AsNoTracking()
                .Where(x => x.Leader2ID != null && x.Course.Year == term.Year)
                .Select(x => x.Leader2ID).ToListAsync())
            {
                var p = people.Find(x => x.ID == l);
                if (p != null)
                {
                    p.IsCourseLeader = true;
                    if (p.FinancialTo < term.Year) p.FinancialTo = term.Year;
                }
            }
            ;
            foreach (var l in await dbc.Class.AsNoTracking()
                .Where(x => x.Leader3ID != null && x.Course.Year == term.Year)
                .Select(x => x.Leader3ID).ToListAsync())
            {
                var p = people.Find(x => x.ID == l);
                if (p != null)
                {
                    p.IsCourseLeader = true;
                    if (p.FinancialTo < term.Year) p.FinancialTo = term.Year;
                }
            }
            ;
        }
        foreach (var v in await dbc.Volunteer.Select(x => x.PersonID).ToListAsync())
        {
            var p = people.Find(x => x.ID == v);
            if (p != null) p.IsVolunteer = true;
        }
        ;
        foreach (var c in await dbc.Committee.Select(x => x.PersonID).ToListAsync())
        {
            var p = people.Find(x => x.ID == c);
            if (p != null)
            {
                p.IsCommitteeMember = true;
                if (p.FinancialTo < term.Year) { p.FinancialTo = term.Year; }
            }
        }
        ;
        foreach (var p in people.Where(x => x.IsLifeMember))
        {
            if (p.FinancialTo < term.Year) { p.FinancialTo = term.Year; }
        }
    }
    static void ApplyGroups(U3ADbContext dbc, Person person)
    {
        Term? term = dbc.Term.Where(x => x.IsDefaultTerm).FirstOrDefault();
        if (term != null)
        {
            if (dbc.Class.Include(x => x.Course)
                .Where(x => x.Course.Year == term.Year
                    && (x.LeaderID == person.ID
                        || x.Leader2ID == person.ID
                        || x.Leader3ID == person.ID)).Any()) person.IsCourseLeader = true;
        }
        if (dbc.Volunteer.Where(x => x.PersonID == person.ID).Any()) { person.IsVolunteer = true; }
        if (dbc.Committee.Where(x => x.PersonID == person.ID).Any()) { person.IsCommitteeMember = true; }
        if (person.IsCourseLeader || person.IsLifeMember || person.IsCommitteeMember)
        {
            if (person.FinancialTo < term.Year) person.FinancialTo += term.Year;
        }
    }
    static async Task ApplyGroupsAsync(U3ADbContext dbc, Person person)
    {
        Term? term = await dbc.Term.Where(x => x.IsDefaultTerm).FirstOrDefaultAsync();
        if (term != null)
        {
            if (await dbc.Class.Include(x => x.Course)
                .Where(x => x.Course.Year == term.Year
                    && (x.LeaderID == person.ID
                        || x.Leader2ID == person.ID
                        || x.Leader3ID == person.ID)).AnyAsync()) person.IsCourseLeader = true;
        }
        if (await dbc.Volunteer.Where(x => x.PersonID == person.ID).AnyAsync()) { person.IsVolunteer = true; }
        if (await dbc.Committee.Where(x => x.PersonID == person.ID).AnyAsync()) { person.IsCommitteeMember = true; }
        if (person.IsCourseLeader || person.IsLifeMember || person.IsCommitteeMember)
        {
            if (person.FinancialTo < term.Year) person.FinancialTo += term.Year;
        }
    }

    public static Person ConvertPersonImportToPerson(PersonImport ImportData)
    {
        Person person = new Person()
        {
            DataImportTimestamp = ImportData.Timestamp,
            FirstName = ImportData.FirstName,
            LastName = ImportData.LastName,
            Address = ImportData.Address,
            City = ImportData.City,
            State = ImportData.State,
            Postcode = ImportData.Postcode,
            Gender = ImportData.Gender,
            BirthDate = ImportData.BirthDate,
            Email = ImportData.Email,
            HomePhone = ImportData.HomePhone,
            Mobile = ImportData.Mobile,
            SMSOptOut = ImportData.SMSOptOut,
            ICEContact = ImportData.ICEContact,
            ICEPhone = ImportData.ICEPhone,
            VaxCertificateViewed = ImportData.VaxCertificateViewed,
            Occupation = ImportData.Occupation,
            Communication = ImportData.Communication,
        };
        if (ImportData.IsNewPerson) person.DateJoined = DateTime.UtcNow.Date;
        return person;
    }
    public static Person ConvertPersonImportToPerson(Person person, PersonImport ImportData)
    {
        person.DataImportTimestamp = ImportData.Timestamp;
        person.FirstName = ImportData.FirstName;
        person.LastName = ImportData.LastName;
        person.Address = ImportData.Address;
        person.City = ImportData.City;
        person.State = ImportData.State;
        person.Postcode = ImportData.Postcode;
        person.Gender = ImportData.Gender;
        person.BirthDate = ImportData.BirthDate;
        person.Email = ImportData.Email;
        person.HomePhone = ImportData.HomePhone;
        person.Mobile = ImportData.Mobile;
        person.SMSOptOut = ImportData.SMSOptOut;
        person.ICEContact = ImportData.ICEContact;
        person.ICEPhone = ImportData.ICEPhone;
        person.VaxCertificateViewed = ImportData.VaxCertificateViewed;
        person.Occupation = ImportData.Occupation;
        person.Communication = ImportData.Communication;
        if (ImportData.IsNewPerson) person.DateJoined = DateTime.UtcNow.Date;
        return person;
    }
    public static async Task<Person?> FindPersonByImportDataID(U3ADbContext dbc, string Timestamp)
    {
        return await dbc.Person.Where(x => x.DataImportTimestamp.Trim() == Timestamp.Trim()).FirstOrDefaultAsync();
    }

    public static bool IsFinancial(Person person, Term currentTerm)
    {
        if (person.DateCeased != null) return false;
        if (person.FinancialTo < currentTerm.Year) return false;
        if (person.FinancialTo > currentTerm.Year) return true;
        // financialTo == current year
        if (person.FinancialToTerm == null) return true;
        if (person.FinancialToTerm >= currentTerm.TermNumber) return true;
        return false;
    }
    public static async Task<List<LeaderHistory>> GetLeaderHistoryForPersonAsync(U3ADbContext dbc, Guid PersonID)
    {
        return await dbc.LeaderHistory
                    .Where(x => x.PersonID == PersonID)
                    .OrderByDescending(x => x.Year).ThenBy(x => x.Course).ThenBy(x => x.Term)
                    .ToListAsync();
    }

    public static async Task<List<Note>> GetNotesForPersonAsync(U3ADbContext dbc, Guid PersonID)
    {
        return await dbc.Note.AsNoTracking()
                    .Where(x => x.PersonID == PersonID)
                    .OrderByDescending(x => x.UpdatedOn)
                    .ToListAsync();
    }
    public static async Task<List<Note>> GetNotesAsync(U3ADbContext dbc)
    {
        var notes = await dbc.Note.AsNoTracking()
                    .Include(x => x.Person)
                    .ToListAsync();
        return notes.OrderBy(x => x.Person.FullNameAlpha)
                    .ThenByDescending(x => x.UpdatedOn).ToList();

    }

    public static async Task<List<(Guid PersonID, int NoteCount)>> GetPersonNoteCountsAsync(U3ADbContext dbc)
    {
        var grouped = await dbc.Note
                .GroupBy(x => x.PersonID)
                .Select(g => new { PersonID = g.Key, NoteCount = g.Count() })
                .OrderByDescending(x => x.NoteCount)
                .ToListAsync();

        return grouped.Select(x => (x.PersonID, x.NoteCount)).ToList();
    }
}
