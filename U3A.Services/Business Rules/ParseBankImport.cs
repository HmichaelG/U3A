using DevExpress.Data.Linq.Helpers;
using Eway.Rapid.Abstractions.Response;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using Twilio.TwiML.Fax;
using U3A.Database;
using U3A.Model;

namespace U3A.BusinessRules
{
    public static partial class BusinessRule
    {
        public static async Task<List<Person>> ParseBankDescription(U3ADbContext dbc, string BankDescription)
        {
            List<Person> people = new List<Person>();

            // removing the identifier stop matches against the group name (eastlakes, maitland etc)
            var info = dbc.TenantInfo;
            var bankDescription = BankDescription;
            bankDescription = bankDescription.Replace(info.Identifier, String.Empty,StringComparison.InvariantCultureIgnoreCase);

            // split the description into space-delimited tokens
            var tokens = GetTokens(BankDescription);

            // find all people where LastName & (FirstName OR FirstName Initials) is contained in tokens
            string tokenLastName = "";
            string tokenFirstName = "";
            string tokenFirstInitial = "";
            foreach (var token in tokens)
            {
                tokenLastName = (await dbc.Person.AnyAsync(x => x.LastName == token)) ? token : "";
                if (tokenLastName != "") break;
            }
            if (tokenLastName != "")
            {
                foreach (var token in tokens)
                {
                    if (token != tokenLastName)
                    {
                        tokenFirstName = (await dbc.Person.AnyAsync(x => x.FirstName == token)) ? token : "";
                    }
                    if (tokenFirstName != "") break;
                }
            }
            if (tokenLastName != "" && tokenFirstName == "")
            {
                foreach (var token in tokens)
                {
                    if (tokenFirstName == "" && !string.Equals(token,tokenLastName,StringComparison.InvariantCultureIgnoreCase) && token.Length == 1)
                    {
                        tokenFirstInitial =
                            (await dbc.Person.AnyAsync(x => x.FirstName.Substring(0, 1) == token))
                                ? token.Substring(0, 1) : "";
                    }
                    if (tokenFirstInitial != "") break;
                }
            }

            if (tokenLastName != "" && tokenFirstName != "")
            {
                people = await dbc.Person.Where(x => x.LastName == tokenLastName &&
                                            x.FirstName == tokenFirstName).ToListAsync();
            }
            if (tokenLastName != "" && tokenFirstInitial != "")
            {
                people = await dbc.Person.Where(x => x.LastName == tokenLastName &&
                                            x.FirstName.Substring(0, 1) == tokenFirstInitial).ToListAsync();
            }
            if (people.Count <= 0 && tokenLastName != "")
            {
                people = await dbc.Person.Where(x => x.LastName == tokenLastName).ToListAsync();
            }
            return people;
        }

        public static async Task<bool> IsReceiptOnFileAsync(U3ADbContext dbc,
                                        DateTime Date, String Description, DateTime StartTime)
        {
            return dbc.Receipt.Any(x => x.Date == Date &&
                                        x.Description == Description &&
                                        x.CreatedOn < StartTime);
        }
        public static async Task<Receipt?> GetReceiptOnFileAsync(U3ADbContext dbc,
                                        DateTime Date, String Description, DateTime StartTime)
        {
            return await dbc.Receipt.FirstOrDefaultAsync(x => x.Date == Date &&
                                        x.Description == Description &&
                                        x.CreatedOn < StartTime);
        }
        public static async Task DeleteReceiptOnFileAsync(U3ADbContext dbc,
                                            DateTime Date, String Description, DateTime StartTime)
        {
            dbc.RemoveRange(await dbc.Receipt.Where(x => x.Date == Date &&
                                        x.Description == Description &&
                                        x.CreatedOn < StartTime).ToArrayAsync());
        }
        public static async Task<decimal> GetPreviouslyPaidAsync(U3ADbContext dbc,
                                                    Guid? PersonID,
                                                    int ProcessingYear,
                                                    DateTime StartTime)
        {
            return await dbc.Receipt.Where(x => x.PersonID == PersonID &&
                                            x.ProcessingYear == ProcessingYear &&
                                            x.CreatedOn < StartTime)
                                            .SumAsync(x => x.Amount);
        }
    }
}