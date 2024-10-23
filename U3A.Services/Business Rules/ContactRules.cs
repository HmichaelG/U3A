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
        var tags = await dbc.Tag.IgnoreQueryFilters()
                        .Where(x => x.Contacts.Any(x => !x.IsDeleted))
                        .Include(x => x.Contacts)
                        .OrderBy(x => x.Name).ToListAsync();
        List<TaggedContact> result = new();
        foreach (var tag in tags)
        {
            foreach (var contact in tag.Contacts)
            {
                result.Add(new TaggedContact() { Tag = tag, Contact = contact });
            }
        }
        return result;
    }

    public static async Task<Tag?> DuplicateTagAsync(U3ADbContext dbc, Tag tag)
    {
        return await dbc.Tag.AsNoTracking()
                        .Where(x => x.Id != tag.Id &&
                                    x.Name.Trim().ToUpper() == tag.Name.Trim().ToUpper()).FirstOrDefaultAsync();
    }


}