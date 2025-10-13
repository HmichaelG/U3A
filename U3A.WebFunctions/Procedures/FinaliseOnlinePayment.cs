using Serilog;
using U3A.BusinessRules;
using U3A.Database;
using U3A.Model;
using U3A.Services;

namespace U3A.WebFunctions.Procedures
{

    public static class FinaliseOnlinePayment
    {
        public static async Task Process(TenantInfo tenant)
        {
            using U3ADbContext dbc = new(tenant);
            dbc.UtcOffset = await Common.GetUtcOffsetAsync(dbc);
            Term? term = await BusinessRule.CurrentEnrolmentTermAsync(dbc);
            term ??= await BusinessRule.CurrentTermAsync(dbc);
            if (term == null) { return; }
            EwayPaymentService paymentService = new(dbc);
            foreach (OnlinePaymentStatus payment in await BusinessRule.GetUnprocessedOnlinePayment(dbc))
            {
                Person? person = await dbc.Person.FindAsync(payment.PersonID);
                if (person == null) { continue; }
                try
                {
                    await paymentService.FinaliseEwayPyamentAsync(dbc, payment, term);
                    Log.Information($"Online payment for {person.FullName} finalised.");
                }
                catch (EwayResponseException ex)
                {
                    string response = (!string.IsNullOrWhiteSpace(ex.PaymentResult.ResponseCode))
                        ? $"{ex.PaymentResult.ResponseCode} {ex.PaymentResult.ResponseMessage}"
                        : "No response received.";
                    Log.Information(ex, $"Payment for {person.FullName} not processed: EWAY reason: {response}");
                }
                catch (Exception ex)
                {
                    Log.Error($"Error processing online payment for {person.FullName}. {ex.Message}");
                }
            }
        }
    }
}
