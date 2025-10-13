using DevExpress.XtraRichEdit.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using U3A.Database;
using U3A.Model;

namespace U3A.BusinessRules;

public static partial class BusinessRule
{

    static readonly int gracePeriodDays = 30;
    public static async Task<int> TwoFactorGracePeriodRemainingAsync(U3ADbContext dbc, string? userName)
    {
        var user = await dbc.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserName == userName);
        if (user == null) throw new NullReferenceException("User is null");

        if (user.TwoFactorGracePeriodStart == null)
        {
            user.TwoFactorGracePeriodStart = DateOnly.FromDateTime(DateTime.UtcNow);
            dbc.Update(user);
            await dbc.SaveChangesAsync();
        }

        var userGracePeriodStart = user.TwoFactorGracePeriodStart.Value;
        var cutOffDate = userGracePeriodStart.AddDays(gracePeriodDays);
        var difference = cutOffDate.DayNumber - DateOnly.FromDateTime(DateTime.UtcNow).DayNumber;

        return difference;
    }

    public static async Task<bool> IsTwoFactorRequiredAsync(U3ADbContext dbContext,
        IConfiguration config,
        string? userName)
    {
        //Environment
        var exceptedUsers = config.GetValue<String>("TwoFactorExceptedUsers");
        if (exceptedUsers != null)
        {
            if (exceptedUsers.ToLower().Contains(userName.ToLower()))
                return false;
        }
        //Tenant
        var tenantInfo = dbContext.TenantInfo;
        if (tenantInfo == null) throw new NullReferenceException("TenantInfo is null");
        if (tenantInfo.IsTwoFactorNotRequired) return false;
        //User
        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserName == userName);
        if (user == null) throw new NullReferenceException("User is null");
        if (user.IsTwoFactorNotRequired) return false;
        if (user.TwoFactorEnabled) return false;
        return true;
    }
}
