using Blazored.LocalStorage;
using DevExpress.Logify;
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
                            decimal TotalFee)
        {

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

            if (request.TransactionType == TransactionTypes.Purchase)
            {
                request.Payment = new Payment()
                {
                    TotalAmount = (int)(TotalFee * 100M),
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
                    Status = String.Empty
                };
                await dbc.AddAsync(pay);
                await dbc.SaveChangesAsync();
                result = response.SharedPaymentUrl;
            }
            else
            {
                var msg = $"Error processing CreateResponsiveSharedRequest; Error code(s): {response.Errors}";
                EwayResponseException ex = new(msg);
                throw ex;
            }
            return result;
        }

        static SemaphoreSlim? semaphore = new(1);
        public async Task DoFinaliseEwayPyamentAsync(IDbContextFactory<U3ADbContext> U3Adbfactory, Guid personID, Term term)
        {
            await semaphore.WaitAsync();
            try
            {
                await FinaliseEwayPyamentAsync(U3Adbfactory, personID, term);
            }
            finally
            {
                semaphore.Release();
            }
        }
        public async Task FinaliseEwayPyamentAsync(IDbContextFactory<U3ADbContext> U3Adbfactory, Guid personID, Term term)
        {
            using (var dbc = await U3Adbfactory.CreateDbContextAsync())
            {
                GetClient(dbc);
                await FinaliseEwayPyamentAsync(dbc, personID, term);
            }
        }

        public async Task FinaliseEwayPyamentAsync(U3ADbContext dbc, OnlinePaymentStatus payment, Term term)
        {
            GetClient(dbc);
            var feeService = new MemberFeeCalculationService();
            var person = await dbc.Person.FindAsync(payment.PersonID);
            var doneCodes = new List<string>();
            PaymentResult? result = null;
            var response = await ewayClient.QueryAccessCode(payment.AccessCode);
            if (response.Errors == null)
            {
                if (response.AccessCode == null ||  response.AccessCode != payment.AccessCode)
                        { throw new Exception("Payment details no longer exist at Eway."); }
                var receipts = await dbc.Receipt.Where(x => x.PersonID == person.ID).ToListAsync();
                var receipt = receipts.Where(x => x.PersonID == person.ID
                                && x.Description.EndsWith(response.AuthorisationCode)
                                && x.Identifier.EndsWith(response.TransactionID.ToString())).FirstOrDefault();
                if (receipt != null)
                        { throw new Exception("A cash receipt already exists for this transaction."); }
                result = new PaymentResult()
                {
                    Date = payment.CreatedOn.Value.Add(TimezoneAdjustment.TimezoneOffset),
                    AccessCode = response.AccessCode,
                    AuthorizationCode = response.AuthorisationCode,
                    TransactionID = response.TransactionID.GetValueOrDefault(),
                    Amount = (decimal)(response.TotalAmount.GetValueOrDefault() / 100.00),
                    ResponseCode = response.ResponseCode ?? "",
                    ResponseMessage = response.ResponseMessage ?? ""
                };
            }
            if (result != null)
            {
                await CreateReceipt(dbc, result, person, term);
            }
        }

        MemberFeeCalculationService feeService = new ();

        public async Task FinaliseEwayPyamentAsync(U3ADbContext dbc, Guid personID, Term term)
        {
            GetClient(dbc);
            feeService = new MemberFeeCalculationService();
            var person = await dbc.Person.FindAsync(personID);
            var doneCodes = new List<string>();
            while (true)
            {
                PaymentResult? result = await ProcessPaymentResponse(dbc, person);
                if (result == null) return;
                if (doneCodes.Contains(result.AccessCode)) { return; }
                doneCodes.Add(result.AccessCode);
                await CreateReceipt(dbc,result,person,term);
            }
        }

        private async Task CreateReceipt(U3ADbContext dbc,PaymentResult result, Person person, Term term)
        {
            if (result.AccessCode != null && !string.IsNullOrWhiteSpace(result.AuthorizationCode))
            {
                var receipt = new Receipt()
                {
                    Amount = result.Amount,
                    Date = (result.Date == null) 
                            ? TimezoneAdjustment.GetLocalTime(DateTime.Now).Date 
                            : result.Date.Value,
                    Description = $"Eway online payment Auth: {result.AuthorizationCode}",
                    Identifier = $"TransID: {result.TransactionID}",
                    Person = person
                };

                var processingYear = term.Year;
                var minMembershipFee = await feeService.CalculateMinimumFeePayableAsync(dbc, person);

                // Special Rule: set Financial To if amount paid greater than minimum amount
                var previouslyPaid = BusinessRule.GetPreviouslyPaidAsync(dbc, person.ID, processingYear, DateTime.Now);
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
                person.DateJoined = receipt.DateJoined;
                await dbc.AddAsync(receipt);
                dbc.Update(person);
            }
            await SetPaymentStatusProcessed(dbc, result, person);
            await dbc.SaveChangesAsync();
        }
        public async Task<PaymentResult?> ProcessPaymentResponse(U3ADbContext dbc, Person person)
        {
            PaymentResult? result = null;
            var reqDetails = await BusinessRule.GetUnprocessedOnlinePayment(dbc, person);
            if (reqDetails != null)
            {
                var response = await ewayClient.QueryAccessCode(reqDetails.AccessCode);
                if (response.Errors == null)
                {
                    result = new PaymentResult()
                    {
                        AccessCode = response.AccessCode,
                        AuthorizationCode = response.AuthorisationCode,
                        TransactionID = response.TransactionID.GetValueOrDefault(),
                        Amount = (decimal)(response.TotalAmount.GetValueOrDefault() / 100.00),
                        ResponseCode = response.ResponseCode ?? "",
                        ResponseMessage = response.ResponseMessage ?? ""
                    };
                }
            }
            return result;
        }
        public async Task SetPaymentStatusProcessed(U3ADbContext dbc, PaymentResult result, Person person)
        {
            var details = await dbc.OnlinePaymentStatus.FirstOrDefaultAsync(x => x.AccessCode == result.AccessCode);
            if (details != null)
            {
                details.Status = "Processed";
                details.ResultCode = result.ResponseCode;
                details.ResultMessage = result.ResponseMessage;
                dbc.Update(details);
                if (details.ResultCode == "00" && string.IsNullOrWhiteSpace(person.Email))
                {
                    await BusinessRule.CreateReceiptSendMailAsync(dbc);
                }
            }
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
        public DateTime? Date { get; set; }
        public string AccessCode { get; set; }
        public string AuthorizationCode { get; set; }
        public int TransactionID { get; set; }
        public decimal Amount { get; set; }
        public string ResponseCode { get; set; }
        public string ResponseMessage { get; set; }

    }

    public class EwayResponseException : Exception
    {
        public string Message { get; set; }
        public EwayResponseException(string Message) { this.Message = Message; }
    }
}


