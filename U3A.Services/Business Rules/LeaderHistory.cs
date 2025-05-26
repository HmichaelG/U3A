using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using U3A.Database;
using U3A.Model;
using Serilog;

namespace U3A.BusinessRules;

public static partial class BusinessRule
{
    public async static Task CreateUpdateLeaderHistoryAsync(U3ADbContext dbc)
    {
        var term = await CurrentTermAsync(dbc);
        if (term == null) return;
        List<LeaderHistory> currentHistory = await GetCurrentLeaderHistoryForTerm(dbc, term);
        List<LeaderHistory> persistedHistory = await dbc.LeaderHistory
                                                    .Where(x => x.TermID == term.ID)
                                                    .ToListAsync();
        //Merge persisted history with current history and update the database
        //Remove any persisted history that is not in the current history
        foreach (var ph in persistedHistory)
        {
            if (!currentHistory.Any(x => x.ID == ph.ID))
            {
                dbc.LeaderHistory.Remove(ph);
            }
        }
        //Add any current history that is not in the persisted history
        foreach (var ch in currentHistory)
        {
            if (!persistedHistory.Any(x => x.ID == ch.ID))
            {
                dbc.LeaderHistory.Add(ch);
            }
        }
        //Update any persisted history that is in the current history
        foreach (var ch in currentHistory)
        {
            var ph = persistedHistory.FirstOrDefault(x => x.ID == ch.ID);
            if (ph != null)
            {
                ph.PersonID = ch.PersonID;
                ph.ClassID = ch.ClassID;
                ph.Year = ch.Year;
                ph.Term = ch.Term;
                ph.Type = ch.Type;
                ph.Course = ch.Course;
                ph.Class = ch.Class;
            }
        }
        //Save changes to the database
        try
        {
            await dbc.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // log the error
            Log.Error($"Error saving LeaderHistory: {ex.Message}");
        }
    }

    static async Task<List<LeaderHistory>> GetCurrentLeaderHistoryForTerm(U3ADbContext dbc, Term term)
    {
        List<LeaderHistory> result = new();
        List<string> keys = new();

        List<Class> classes = await dbc.Class
                            .Include(x => x.OnDay)
                            .Include(x => x.Occurrence)
                            .Include(x => x.Course)
                            .Include(x => x.Leader)
                            .Include(x => x.Leader2)
                            .Include(x => x.Leader3)
                            .Where(x => !x.Course.IsOffScheduleActivity && x.Course.Year == term.Year)
                            .ToListAsync();
        foreach (var c in classes)
        {
            if (c.OfferedTerm1 && term.TermNumber == 1)
            {
                if (c.Leader != null) { await AddLeaderHistory(result, dbc, c, c.Leader, term, keys); }
                if (c.Leader2 != null) { await AddLeaderHistory(result, dbc, c, c.Leader2, term, keys); }
                if (c.Leader3 != null) { await AddLeaderHistory(result, dbc, c, c.Leader3, term, keys); }
            }
            if (c.OfferedTerm2 && term.TermNumber == 2)
            {
                if (c.Leader != null) { await AddLeaderHistory(result, dbc, c, c.Leader, term, keys); }
                if (c.Leader2 != null) { await AddLeaderHistory(result, dbc, c, c.Leader2, term, keys); }
                if (c.Leader3 != null) { await AddLeaderHistory(result, dbc, c, c.Leader3, term, keys); }
            }   
            if (c.OfferedTerm3 && term.TermNumber == 3)
            {
                if (c.Leader != null) { await AddLeaderHistory(result, dbc, c, c.Leader, term, keys); }
                if (c.Leader2 != null) { await AddLeaderHistory(result, dbc, c, c.Leader2, term, keys); }
                if (c.Leader3 != null) { await AddLeaderHistory(result, dbc, c, c.Leader3, term, keys); }
            }
            if (c.OfferedTerm4 && term.TermNumber == 4)
            {
                if (c.Leader != null) { await AddLeaderHistory(result, dbc, c, c.Leader, term, keys); }
                if (c.Leader2 != null) { await AddLeaderHistory(result, dbc, c, c.Leader2, term, keys); }
                if (c.Leader3 != null) { await AddLeaderHistory(result, dbc, c, c.Leader3, term, keys); }
            }
        }
        //Clerks History
        var enrolments = await dbc.Enrolment
            .Include(x => x.Course)
            .Include(x => x.Class).ThenInclude(x => x.OnDay)
            .Include(x => x.Class).ThenInclude(x => x.Occurrence)
            .Include(x => x.Person)
            .Where(x => x.IsCourseClerk && !x.Course.IsOffScheduleActivity && x.TermID == term.ID)
            .ToListAsync();
        foreach (var e in enrolments)
        {
            if (e.Class == null)
            {
                var CourseClasses = classes.Where(x => x.CourseID == e.CourseID).ToList();
                if (CourseClasses.Count == 1) { e.Class = CourseClasses.First(); e.ClassID = e.Class.ID; }
            }
            var key = $"{e.PersonID}_{e.TermID}_{(e.ClassID == null ? Guid.Empty : e.ClassID)}";
            if (keys.Contains(key)) { continue; }
            keys.Add(key);
            LeaderHistory history = new()
            {
                ID = Guid.NewGuid(),
                PersonID = e.PersonID,
                Person = e.Person,
                Year = e.Term.Year,
                Term = e.Term.TermNumber,
                ClassID = (e.ClassID.HasValue) ? e.ClassID.Value : Guid.Empty,
                Course = e.Course.Name,
                Class = (e.Class is not null) ? e.Class.OccurrenceTextBrief : "All classes",
                TermID = e.TermID,
                Type = LeaderType.Clerk,
            };
            result.Add(history);
        }
        return result; ;
    }

    static async Task AddLeaderHistory(List<LeaderHistory> result,
                                        U3ADbContext dbc, Class c, Person person, Term term, List<string> keys)
    {
        var key = $"{person.ID}_{term.ID}_{c.ID}";
        if (keys.Contains(key)) { return; }
        keys.Add(key);
        LeaderHistory history = new()
        {
            ID = Guid.NewGuid(),
            PersonID = person.ID,
            Person = person,
            Year = term.Year,
            Term = term.TermNumber,
            ClassID = c.ID,
            Course = c.Course.Name,
            Class = c.OccurrenceTextBrief,
            TermID = term.ID,
            Type = LeaderType.Leader,
        };
        result.Add(history);
    }

}
