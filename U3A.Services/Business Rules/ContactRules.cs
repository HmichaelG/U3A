using DevExpress.XtraRichEdit.Model;
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
        var tags = dbc.Tag
                        .OrderBy(x => x.Name).ToList();
        return tags;
    }

}