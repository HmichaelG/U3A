using DevExpress.XtraPrinting.Native;
using DevExpress.XtraRichEdit.Commands.Internal;
using Eway.Rapid.Abstractions.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text;
using U3A.Database;
using U3A.Model;

namespace U3A.BusinessRules
{
    public static partial class BusinessRule
    {
        // Result Code 06 
        //The customer’s card issuer has declined the transaction as there is a problem with the card number.
        //The customer should use an alternate credit card, or contact their bank.
        //Note: This response code can also be returned via the Rapid API if you run a transaction query prior to the transaction being completed.

        public static bool HasUnprocessedOnlinePayment(U3ADbContext dbc, Person person)
        {
            if (person == null) { return false; }
            return dbc.OnlinePaymentStatus.Where(x => x.PersonID == person.ID).AsEnumerable()
                                                                .Any(x =>
                                                                (x.AccessCode != string.Empty && x.Status == string.Empty)
                                                                ||
                                                                (x.ResultCode == "06" && (DateTime.Now - x.CreatedOn.Value).TotalHours <= 24)
                                                                );
        }
        public static bool HasUnprocessedOnlinePayment(U3ADbContext dbc, string AdminEmail)
        {
            return dbc.OnlinePaymentStatus.Where(x => x.AdminEmail == AdminEmail).AsEnumerable()
                                                                .Any(x =>
                                                                (x.AccessCode != string.Empty && x.Status == string.Empty)
                                                                ||
                                                                (x.ResultCode == "06" && (DateTime.Now - x.CreatedOn.Value).TotalHours <= 24)
                                                                );
        }
        public static List<OnlinePaymentStatus> GetUnprocessedOnlinePayment(U3ADbContext dbc)
        {
            return dbc.OnlinePaymentStatus.AsEnumerable()
                            .Where(x =>
                            (x.AccessCode != string.Empty && x.Status == string.Empty)
                            ||
                            (x.ResultCode == "06" && (DateTime.Now - x.CreatedOn.Value).TotalHours <= 24)
                            )
                        .OrderBy(x => x.CreatedOn)
                        .ToList();
        }
        
        public static OnlinePaymentStatus GetUnprocessedOnlinePayment(U3ADbContext dbc, Person person)
        {
            return dbc.OnlinePaymentStatus.Where(x => x.PersonID == person.ID).AsEnumerable()
                        .OrderBy(x => x.CreatedOn).FirstOrDefault(x =>
                            (x.AccessCode != string.Empty && x.Status == string.Empty)
                            ||
                            (x.ResultCode == "06" && (DateTime.Now - x.CreatedOn.Value).TotalHours <= 24)
                        );
        }
        public static List<OnlinePaymentStatus> GetUnprocessedOnlinePayment(U3ADbContext dbc, string AdminEmail)
        {
            return dbc.OnlinePaymentStatus.Where(x => x.AdminEmail == AdminEmail &&
                        x.WorkstationID == Workstation.ID).AsEnumerable()
                        .Where(x =>
                        (x.AccessCode != string.Empty && x.Status == string.Empty)
                        ||
                        (x.ResultCode == "06" && (DateTime.Now - x.CreatedOn.Value).TotalHours <= 24)
                        )
                    .OrderBy(x => x.CreatedOn)
                    .ToList();
        }
        public static async Task<List<OnlinePaymentStatus>> GetOnlinePaymentStatus(U3ADbContext dbc)
        {
            return await dbc.OnlinePaymentStatus.OrderByDescending(x => x.CreatedOn).ToListAsync();
        }
    }

}