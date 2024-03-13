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
        public static async Task<List<OnlinePaymentStatus>> GetOnlinePaymentStatus(U3ADbContext dbc)
        {
            return await dbc.OnlinePaymentStatus.OrderByDescending(x => x.CreatedOn).ToListAsync();
        }
    }

}