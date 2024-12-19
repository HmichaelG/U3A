using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using U3A.Database;
using U3A.Model;
using U3A.Services;

namespace U3A.BusinessRules;

public static partial class BusinessRule
{

    public static async Task<List<Contact>> EditableContactAsync(U3ADbContext dbc)
    {
        var contacts = dbc.Contact
                        .IgnoreQueryFilters()
                        .Include(x => x.Tags)
                        .Where(x => !x.IsDeleted)
                        .OrderBy(x => x.LastName)
                        .ThenBy(x => x.FirstName)
                        .ThenBy(x => x.Email).ToList();
        return contacts;
    }
    public static async Task<List<Tag>> SelectableTagAsync(U3ADbContext dbc)
    {
        var tags = await dbc.Tag
                        .OrderBy(x => x.Name).ToListAsync();
        return tags;
    }
    public static async Task<List<TaggedContact>> SelectableTaggedContactsAsync(U3ADbContext dbc)
    {
        var contacts = await EditableContactAsync(dbc);
        var emprtyTag = new Tag() { Id = Guid.Empty, Name = " []" };
        List<TaggedContact> result = new();
        foreach (var contact in contacts)
        {
            if (contact.Tags.Any())
            {
                foreach (var tag in contact.Tags)
                {
                    result.Add(new TaggedContact() { Tag = tag, Contact = contact });
                }
            }
            else { result.Add(new TaggedContact() { Tag = emprtyTag, Contact = contact }); }
        }
        return result;
    }

    public static async Task<Tag?> DuplicateTagAsync(U3ADbContext dbc, Tag tag)
    {
        return await dbc.Tag.AsNoTracking()
                        .Where(x => x.Id != tag.Id &&
                                    x.Name.Trim().ToUpper() == tag.Name.Trim().ToUpper()).FirstOrDefaultAsync();
    }

    public static async Task<List<Contact>> EditableDeletedContactsAsync(U3ADbContext dbc)
    {
        var people = await dbc.Contact.IgnoreQueryFilters()
                        .Include(x => x.Enrolments)
                        .Where(x => x.IsDeleted)
                        .OrderBy(x => x.LastName)
                        .ThenBy(x => x.FirstName)
                        .ThenBy(x => x.Email).ToListAsync();
        return people;
    }
    public static async Task<List<Person>> EnrollablePeopleAsync(U3ADbContext dbc, Term selectedTerm)
    {
        var people = await BusinessRule.SelectablePersonsAsync(dbc, selectedTerm);
        var contacts = await dbc.Contact.IgnoreQueryFilters()
                        .Include(x => x.Tags)
                        .Where(x => !x.IsDeleted)
                        .ToListAsync();
        foreach (var c in contacts)
        {
            foreach (var t in c.Tags)
            {
                if (t.CanEnrol) { people.Add(c as Person); break; }
            }
        }
        return people.OrderBy(x => x.FullNameAlphaKey).ToList();
    }
    public static async Task<List<Person>> AllPeopleIncludingContactsAsync(U3ADbContext dbc)
    {
        var people = await BusinessRule.SelectablePersonsIncludeUnfinancialAsync(dbc);
        var contacts = await dbc.Contact.IgnoreQueryFilters()
                        .Include(x => x.Tags)
                        .Where(x => !x.IsDeleted)
                        .ToListAsync();
        foreach (var c in contacts)
        {
                people.Add(c as Person); 
        }
        return people.OrderBy(x => x.FullNameAlphaKey).ToList();
    }
    public static async Task<List<Person>> LeadablePeopleAsync(U3ADbContext dbc)
    {
        var people = await BusinessRule.SelectablePersonsIncludeUnfinancialAsync(dbc);
        var contacts = await dbc.Contact.IgnoreQueryFilters()
                        .Include(x => x.Tags)
                        .Where(x => !x.IsDeleted)
                        .ToListAsync();
        foreach (var c in contacts)
        {
            foreach (var t in c.Tags)
            {
                if (t.CanLead) { people.Add(c as Person); break; }
            }
        }
        return people.OrderBy(x => x.FullNameAlphaKey).ToList();
    }


    public static List<Person> SelectablePeopleIncludeUnfinancial(U3ADbContext dbc)
    {
        var result = Task.Run(() =>
        {
            return SelectablePeopleIncludeUnfinancialAsync(dbc);
        }).Result;
        return result;
    }
    public static async Task<List<Person>> SelectablePeopleIncludeUnfinancialAsync(U3ADbContext dbc)
    {
        var people = await BusinessRule.SelectablePersonsIncludeUnfinancialAsync(dbc);
        var contacts = await dbc.Contact.IgnoreQueryFilters()
                        .Include(x => x.Tags)
                        .Where(x => !x.IsDeleted)
                        .OrderBy(x => x.LastName)
                        .ToListAsync();
        foreach (var c in contacts)
        {
                people.Add(c as Person);
        }
        return people.OrderBy(x => x.FullNameAlphaKey).ToList();
    }

}