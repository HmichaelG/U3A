using Blazored.LocalStorage;
using DevExpress.Logify;
using DevExpress.Utils.Serializing;
using Eway.Rapid;
using Eway.Rapid.Abstractions;
using Eway.Rapid.Abstractions.Interfaces;
using Eway.Rapid.Abstractions.Models;
using Eway.Rapid.Abstractions.Request;
using Eway.Rapid.Abstractions.Response;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Twilio.TwiML.Fax;
using U3A.BusinessRules;
using U3A.Database;
using U3A.Model;

namespace U3A.Services
{
    public class EwayPaymentService
    {

        private IRapidClient ewayClient;

        public EwayPaymentService() { }
        public EwayPaymentService(U3ADbContext dbc)
        {
            GetClient(dbc);
        }
        void GetClient(U3ADbContext dbc)
        {
            var info = dbc.TenantInfo;
            HttpClient httpClient = new HttpClient();
            RapidOptions options = new RapidOptions
            {
                ApiKey = info.EwayAPIKey,
                Password = info.EwayPassword,
                RapidEndPoint = info.UseEwayTestEnviroment ? RapidEndpoints.SANDBOX : RapidEndpoints.PRODUCTION
            };
            options.ConfigureHttpClient(httpClient);
            ewayClient = new RapidClient(httpClient);
        }

        public async Task<string?> CreatePayment(U3ADbContext dbc,
                            string? AdminEmail,
                            string BaseUri,
                            Person person,
                            string PayerEmail,
                            string InvoiceNumber,
                            string InvoiceDescription,
                            string InvoiceReference,
                            decimal TotalFee,
                            int? TermsPaid)
        {
            var settings = await dbc.SystemSettings.OrderBy(x => x.ID).FirstOrDefaultAsync();
            string? result = null;
            CreateResponsiveSharedRequest request = new CreateResponsiveSharedRequest();
            request.HeaderText = "U3Admin.org.au";
            request.VerifyCustomerEmail = true;
            request.Capture = true;
            request.AllowedCards = AllowedCards.Visa | AllowedCards.Mastercard;

            request.RedirectUrl = BaseUri + "?Eway=Success";
            request.CancelUrl = BaseUri + "?Eway=Fail";
            request.TransactionType = TransactionTypes.Purchase;
            request.TrackingID = Guid.NewGuid().ToString();

            var Customer = new DirectTokenCustomer()
            {
                Reference = person.PersonIdentity,
                FirstName = person.FirstName,
                LastName = person.LastName,
                Email = (!string.IsNullOrWhiteSpace(person.Email)) ? PayerEmail.Trim() : string.Empty
            };
            request.Customer = Customer;
            request.CustomerReadOnly = true;

            int U3AFee = (int)(TotalFee * 100M);
            request.Options = new();
            request.Options.Add(new Option() { Value = U3AFee.ToString("0") });

            if (request.TransactionType == TransactionTypes.Purchase)
            {
                request.Payment = new Payment()
                {
                    TotalAmount = U3AFee,
                    InvoiceDescription = InvoiceDescription,
                    InvoiceNumber = InvoiceNumber,
                    InvoiceReference = InvoiceReference
                };
            }

            // Use the RapidClient to process the request.
            var response = await ewayClient.CreateTransaction(request);
            if (response.Errors == null)
            {
                var pay = new OnlinePaymentStatus()
                {
                    AdminEmail = AdminEmail,
                    AccessCode = response.AccessCode,
                    PersonID = person.ID,
                    Status = String.Empty,
                    TermsPaid = TermsPaid,
                };
                await dbc.AddAsync(pay);
                await dbc.SaveChangesAsync();
                result = response.SharedPaymentUrl;
            }
            else
            {
                var msg = $"Error processing CreateResponsiveSharedRequest; Error code(s): {response.Errors}";
                EwayRequestException ex = new(msg);
                throw ex;
            }
            return result;
        }

        static SemaphoreSlim? paymentSemaphore = new(1);
        public async Task FinaliseEwayPyamentAsync(IDbContextFactory<U3ADbContext> U3Adbfactory, OnlinePaymentStatus paymentStatus, Term term)
        {
            await paymentSemaphore.WaitAsync();
            try
            {
                using (var dbc = await U3Adbfactory.CreateDbContextAsync())
                {
                    GetClient(dbc);
                    await FinaliseEwayPyamentAsync(dbc, paymentStatus, term);
                }
            }
            finally
            {
                paymentSemaphore.Release();
            }
        }

        public async Task FinaliseEwayPyamentAsync(U3ADbContext dbc, OnlinePaymentStatus paymentStatus, Term term)
        {
            if (paymentStatus == null) { return; } // cancelled by user
            GetClient(dbc);
            paymentStatus = await dbc.OnlinePaymentStatus.FindAsync(paymentStatus.ID);
            var person = await dbc.Person.FindAsync(paymentStatus.PersonID);
            if (person == null)
            {
                throw new Exception("The participant for this payment no longer exists.");
            }
            PaymentResult? result = null;
            var eWayResponse = await ewayClient.QueryAccessCode(paymentStatus.AccessCode);
            if (eWayResponse.Errors == null)
            {
                if (eWayResponse.AccessCode == null || eWayResponse.AccessCode != paymentStatus.AccessCode)
                {
                    throw new Exception("Payment details no longer exist at Eway.");
                }
                result = new PaymentResult()
                {
                    Date = dbc.GetLocalDate(paymentStatus.CreatedOn.Value),
                    TermsPaid = paymentStatus.TermsPaid,
                    AccessCode = eWayResponse.AccessCode,
                    AuthorizationCode = eWayResponse.AuthorisationCode,
                    TransactionID = eWayResponse.TransactionID.GetValueOrDefault(),
                    TotalPaid = (decimal)(eWayResponse.TotalAmount.GetValueOrDefault() / 100.00),
                    ResponseCode = eWayResponse.ResponseCode ?? "",
                    ResponseMessage = eWayResponse.ResponseMessage ?? ""
                };
                result.OriginalFee = (decimal.Parse(eWayResponse.Options.First().Value) / 100.00M);
                if (!CanSetPaymentStatusProcessed(result.ResponseCode, result.Date))
                {
                    throw new EwayResponseException(@"The processing of your payment is incomplete.
                            This may be due to a delay in processing by your bank or other system issue. 
                            Please wait as we’ll keep checking for 24 hours. Or, if you know the reason simply make your payment now.
                            Otherwise, if the problem persists, contact your U3A.", result);
                }
            }
            else
            {
                throw new Exception("Internet transmission errors are preventing successful completion of your transaction. Please try again later.");
            }
            if (result != null)
            {
                if (await DoesReceiptExist(dbc, person.ID, result))
                {
                    throw new Exception("A cash receipt already exists for this transaction.");
                }
                else
                {
                    await CreateReceipt(dbc, result, person, term);
                }
                await SetPaymentStatusProcessed(dbc, result, person);
                await dbc.SaveChangesAsync();
            }
        }

        private async Task<bool> DoesReceiptExist(U3ADbContext dbc, Guid PersonID, PaymentResult paymentResult)
        {
            if (string.IsNullOrWhiteSpace(paymentResult.AuthorizationCode)) { return false; }
            var receipts = await dbc.Receipt.Where(x => x.PersonID == PersonID).ToListAsync();
            return receipts.Where(x => x.PersonID == PersonID
                            && x.Description.EndsWith(paymentResult.AuthorizationCode)
                            && x.Identifier.EndsWith(paymentResult.TransactionID.ToString())).Any();
        }

        private async Task CreateReceipt(U3ADbContext dbc, PaymentResult result, Person person, Term term)
        {
            var feeService = await MemberFeeCalculationService.CreateAsync(dbc,term,person);
            if (result.AccessCode != null && (result.ResponseCode == "00" || result.ResponseCode == "08"))
            {
                var receipt = new Receipt()
                {
                    Date = result.Date,
                    Description = $"Eway online payment Auth: {result.AuthorizationCode}",
                    Identifier = $"TransID: {result.TransactionID}",
                    Person = person
                };
                receipt.MerchantFee = result.MerchantFee;
                receipt.Amount = result.OriginalFee;
                var processingYear = term.Year;
                var minMembershipFee = feeService.CalculateMinimumFeePayable(person);

                // Special Rule: set Financial To if amount paid greater than minimum amount
                var previouslyPaid = await BusinessRule.GetPreviouslyPaidAsync(dbc, person.ID, processingYear, DateTime.UtcNow);
                if (receipt.Amount + previouslyPaid >= minMembershipFee)
                {
                    receipt.FinancialTo = (person.FinancialTo >= processingYear) ? person.FinancialTo : processingYear;
                }
                else { receipt.FinancialTo = person.FinancialTo; }

                if (receipt.Person.DateJoined == null)
                {
                    receipt.DateJoined = receipt.Date;
                }
                else
                {
                    receipt.DateJoined = receipt.Person.DateJoined.Value;
                }

                receipt.ProcessingYear = processingYear;
                person.PreviousFinancialTo = person.FinancialTo;
                person.PreviousDateJoined = person.DateJoined;
                person.FinancialTo = receipt.FinancialTo;
                person.FinancialToTerm = result.TermsPaid;
                person.DateJoined = receipt.DateJoined;
                await dbc.AddAsync(receipt);
                // send email
                if (string.IsNullOrWhiteSpace(person.Email))
                {
                    await BusinessRule.CreateReceiptSendMailAsync(dbc);
                }
                dbc.Update(person);
            }
        }
        public async Task SetPaymentStatusProcessed(U3ADbContext dbc, PaymentResult result, Person person)
        {
            var details = await dbc.OnlinePaymentStatus.FirstOrDefaultAsync(x => x.AccessCode == result.AccessCode);
            if (details != null)
            {
                details.Status = (CanSetPaymentStatusProcessed(result.ResponseCode, details.CreatedOn)) ? "Processed" : string.Empty;
                details.ResultCode = result.ResponseCode;
                details.ResultMessage = result.ResponseMessage;
                dbc.Update(details);
            }
        }

        static SemaphoreSlim? cancelSemaphore = new(1);
        public async Task CancelPaymentAsync(U3ADbContext dbc, string AccessCode)
        {
            await cancelSemaphore.WaitAsync();
            try
            {
                await CancelEwayPaymentAsync(dbc, AccessCode);
            }
            finally
            {
                cancelSemaphore.Release();
            }
        }

        private async Task CancelEwayPaymentAsync(U3ADbContext dbc, string AccessCode)
        {
            var request = await dbc.OnlinePaymentStatus.FirstOrDefaultAsync(x => x.AccessCode == AccessCode);
            if (request != null)
            {
                request.Status = "Processed";
                request.ResultCode = "--";
                request.ResultMessage = "Payment Cancelled";
                dbc.SaveChangesAsync();
            }
        }

        private bool CanSetPaymentStatusProcessed(string ResponseCode, DateTime? paymentDate)
        {
            return (ResponseCode == "06" && (DateTime.UtcNow - paymentDate.Value).TotalHours <= 6) ? false : true;
        }
        public async Task<Tuple<string, string>> FixResultCodesAsync(U3ADbContext dbc, OnlinePaymentStatus PayStatus)
        {
            var response = await ewayClient.QueryAccessCode(PayStatus.AccessCode);
            if (response.Errors == null)
            {
                return Tuple.Create(response.ResponseCode ?? "", response.ResponseMessage ?? "");
            }
            return null;
        }

    }

    public record PaymentResult
    {
        public DateTime Date { get; set; }
        public string AccessCode { get; set; }
        public string AuthorizationCode { get; set; }
        public int TransactionID { get; set; }
        public decimal TotalPaid { get; set; } // total paid including merchant surcharge
        public decimal OriginalFee { get; set; } // membership fee without surcharge

        public decimal MerchantFee
        {
            get
            {
                return TotalPaid - OriginalFee;
            }
        }
        public int? TermsPaid { get; set; }
        public string ResponseCode { get; set; }
        public string ResponseMessage { get; set; }

    }

    public class EwayRequestException : Exception
    {
        public string Message { get; set; }
        public EwayRequestException(string Message) { this.Message = Message; }
    }
    public class EwayResponseException : Exception
    {
        public string Message { get; set; }
        public PaymentResult PaymentResult { get; set; }
        public EwayResponseException(string Message, PaymentResult PaymentResult)
        {
            this.Message = Message;
            this.PaymentResult = PaymentResult;
        }
    }
}


