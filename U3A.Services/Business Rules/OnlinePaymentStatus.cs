using DevExpress.XtraRichEdit.Commands.Internal;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text;
using U3A.Database;
using U3A.Model;

namespace U3A.BusinessRules
{
    public static partial class BusinessRule
    {
        public static async Task<bool> HasUnprocessedOnlinePayment(U3ADbContext dbc, Person person)
        {
            if (person == null) { return false; }
            return await dbc.OnlinePaymentStatus.AnyAsync(x => x.PersonID == person.ID &&
                                                                x.AccessCode != string.Empty &&
                                                                x.Status == String.Empty);
        }
        public static async Task<bool> HasUnprocessedOnlinePayment(U3ADbContext dbc)
        {
            return await dbc.OnlinePaymentStatus.AnyAsync(x => x.AccessCode != string.Empty
                                                                && x.Status == String.Empty);
        }
        public static async Task<List<OnlinePaymentStatus>> GetUnprocessedOnlinePayment(U3ADbContext dbc)
        {
            return await dbc.OnlinePaymentStatus
                .OrderBy(x => x.CreatedOn)
                .Where(x => x.AccessCode != string.Empty &&
                                            x.Status == String.Empty)
                .ToListAsync();
        }
        public static async Task<OnlinePaymentStatus> GetUnprocessedOnlinePayment(U3ADbContext dbc, Person person)
        {
            return await dbc.OnlinePaymentStatus.OrderBy(x => x.CreatedOn)
                .FirstOrDefaultAsync(x => x.PersonID == person.ID &&
                                            x.AccessCode != string.Empty &&
                                            x.Status == String.Empty);
        }
        public static async Task<List<OnlinePaymentStatus>> GetOnlinePaymentStatus(U3ADbContext dbc,
            DateTime fromDate, DateTime toDate)
        {
            var codes = new EwayResultCodes();
            var status = (await dbc.OnlinePaymentStatus
                            .Where(x => x.CreatedOn >= fromDate.AddDays(-2))
                            .OrderByDescending(x => x.CreatedOn).ToListAsync())
                            .Where(x => dbc.GetLocalDate(x.CreatedOn.Value) >= fromDate
                             && dbc.GetLocalDate(x.CreatedOn.Value) <= toDate).ToList();
            foreach (var s in status)
            {
                var code = codes.FirstOrDefault(x => x.Code == s.ResultCode);
                if (code != null)
                {
                    s.ResultDescription = code.CodeAndDescription;
                    s.ResultLongDescription = code.LongDescription;
                }
            }
            return status;
        }
    }

}