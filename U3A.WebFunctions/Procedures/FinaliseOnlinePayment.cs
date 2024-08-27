using Microsoft.Extensions.Logging;
using U3A.BusinessRules;
using U3A.Database;
using U3A.Model;
using U3A.Services;

namespace U3A.WebFunctions.Procedures
{

    public static class FinaliseOnlinePayment
    {
        public static async Task Process(TenantInfo tenant, ILogger logger)
        {
            _ = new MemberFeeCalculationService();
            using (var dbc = new U3ADbContext(tenant))
            {
                dbc.UtcOffset = await Common.GetUtcOffsetAsync(dbc);
                var term = await BusinessRule.CurrentEnrolmentTermAsync(dbc);
                if (term == null) term = await BusinessRule.CurrentTermAsync(dbc);
                if (term == null) { return; }
                var paymentService = new EwayPaymentService(dbc);
                foreach (var payment in await BusinessRule.GetUnprocessedOnlinePayment(dbc))
                {
                    var person = await dbc.Person.FindAsync(payment.PersonID);
                    if (person == null) { continue; }
                    try
                    {
                        await paymentService.FinaliseEwayPyamentAsync(dbc, payment, term);
                        logger.LogInformation($"Online payment for {person.FullName} finalised.");
                    }
                    catch (Exception ex)
                    {
                        logger.LogInformation($"Error processing online payment for {person.FullName}. {ex.Message}");
                    }
                }
            }
        }
    }
}
