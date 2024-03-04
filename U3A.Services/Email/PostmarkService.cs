using DevExpress.Office.Utils;
using DevExpress.XtraRichEdit.Commands;
using Eway.Rapid.Abstractions.Response;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Postmark.Model.Suppressions;
using PostmarkDotNet;
using PostmarkDotNet.Model;
using Postmark.Model.MessageStreams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using U3A.Database;
using U3A.Model;

namespace U3A.Services.Email
{
    public class PostmarkService
    {

        readonly IDbContextFactory<U3ADbContext> U3AdbFactory;
        readonly PostmarkClient client;
        readonly bool UsePostmarkTestEnvironment;

        public PostmarkService(IDbContextFactory<U3ADbContext> contextFactory)
        {
            U3AdbFactory = contextFactory;
            using (var dbc = U3AdbFactory.CreateDbContext())
            {
                var tenantInfo = dbc.TenantInfo;
                UsePostmarkTestEnvironment = tenantInfo.UsePostmarkTestEnviroment;
                if (UsePostmarkTestEnvironment && tenantInfo.PostmarkSandboxAPIKey != null)
                {
                    client = new PostmarkClient(tenantInfo.PostmarkSandboxAPIKey);
                }
                else if (tenantInfo.PostmarkAPIKey != null) { client = new PostmarkClient(tenantInfo.PostmarkAPIKey); }
            }
        }

        // special constructor for overnight membership email
        public PostmarkService(TenantInfo tenantInfo)
        {
            UsePostmarkTestEnvironment = tenantInfo.UsePostmarkTestEnviroment;
            if (UsePostmarkTestEnvironment && tenantInfo.PostmarkSandboxAPIKey != null)
            {
                client = new PostmarkClient(tenantInfo.PostmarkSandboxAPIKey);
            }
            else if (tenantInfo.PostmarkAPIKey != null) { client = new PostmarkClient(tenantInfo.PostmarkAPIKey); }
        }

        public async Task<IEnumerable<EmailOutboundOverviewStats>> GetOverviewStatisticsAsync(TimeSpan offset)
        {
            var result = new List<EmailOutboundOverviewStats>();
            PostmarkOutboundOverviewStats stats;
            DateTime To = (DateTime.Now + offset).Date;
            foreach (var day in new int[] { -1, -5, -6, -6, -6 })
            {
                DateTime From = To.AddDays(day);
                stats = await client.GetOutboundOverviewStatsAsync(null, From, To);
                var emailStats = new EmailOutboundOverviewStats()
                {
                    Bounced = stats.Bounced,
                    BounceRate = stats.BounceRate,
                    Opens = stats.Opens,
                    Sent = stats.Sent,
                    SpamComplaints = stats.SpamComplaints,
                    SpamComplaintsRate = stats.SpamComplaintsRate,
                    StartDateUTC = From - offset,
                    EndDateUTC = To - offset,
                    StartDate = From,
                    EndDate = To,
                    UniqueOpens = stats.UniqueOpens,
                };
                switch (day + 1)
                {
                    case 0: emailStats.Period = "Last 24 Hours"; break;
                    default: emailStats.Period = $"Previous {-(day) + 1} Days "; break;
                }
                result.Add(emailStats);
                To = From.AddDays(-1);
            }
            return result.OrderByDescending(x => x.StartDateUTC);
        }

        public async Task<List<EmailBounce>> GetBounceData(TimeSpan offset, DateTime FromUTC, DateTime ToUTC)
        {
            var result = new List<EmailBounce>();
            var from = (FromUTC + offset).ToString("yyyy-MM-ddT00:00:00");
            var to = (ToUTC + offset).ToString("yyyy-MM-ddThh:mm:ss");
            PostmarkBounces list = await client.GetBouncesAsync(fromDate: from, toDate: to);
            using (var dbc = await U3AdbFactory.CreateDbContextAsync())
            {
                foreach (var b in list.Bounces)
                {
                    var eb = new EmailBounce()
                    {
                        BouncedAt = b.BouncedAt + offset,
                        CanActivate = b.CanActivate,
                        Description = b.Description,
                        Details = b.Details,
                        DumpAvailable = b.DumpAvailable,
                        Email = b.Email,
                        Person = await dbc.Person.FirstOrDefaultAsync(p => p.Email == b.Email),
                        From = b.From,
                        ID = b.ID,
                        Inactive = b.Inactive,
                        MessageID = b.MessageID,
                        Name = b.Name,
                        ServerID = b.ServerID,
                        Subject = b.Subject,
                        Tag = b.Tag,
                    };
                    result.Add(eb);
                }
            }
            return result;
        }

        public async Task<List<EmailSuppression>> GetSuppressions(TimeSpan offset, DateTime? From = null, DateTime? To = null)
        {
            var result = new List<EmailSuppression>();
            var streams = await client.ListMessageStreams(Postmark.Model.MessageStreams.MessageStreamTypeFilter.All);
            var query = new PostmarkSuppressionQuery() { FromDate = From, ToDate = To };
            using (var dbc = await U3AdbFactory.CreateDbContextAsync())
            {
                foreach (var stream in streams?.MessageStreams)
                {
                    var suppressions = await client.ListSuppressions(query, stream.ID);
                    foreach (var suppression in suppressions.Suppressions)
                    {
                        var s = new EmailSuppression()
                        {
                            CreatedAt = suppression.CreatedAt + offset,
                            Email = suppression.EmailAddress,
                            Stream = stream.ID,
                            Person = await dbc.Person.FirstOrDefaultAsync(p => p.Email == suppression.EmailAddress),
                        };
                        if (suppression.SuppressionReason == "ManualSuppression" && suppression.Origin == "Recipient")
                        {
                            s.Reason = "Unsubscribe";
                        }
                        else { s.Reason = suppression.SuppressionReason; }
                        result.Add(s);
                    }
                }
            }
            return result.OrderByDescending(x => x.CreatedAt).ToList();
        }

        // Special version for overnight membership email
        public async Task<List<EmailSuppression>> GetSuppressions(U3ADbContext dbc, TimeSpan offset, DateTime? From = null, DateTime? To = null)
        {
            var result = new List<EmailSuppression>();
            var streams = await client.ListMessageStreams(Postmark.Model.MessageStreams.MessageStreamTypeFilter.All);
            var query = new PostmarkSuppressionQuery() { FromDate = From, ToDate = To };
            foreach (var stream in streams?.MessageStreams)
            {
                var suppressions = await client.ListSuppressions(query, stream.ID);
                foreach (var suppression in suppressions.Suppressions)
                {
                    var s = new EmailSuppression()
                    {
                        CreatedAt = suppression.CreatedAt + offset,
                        Email = suppression.EmailAddress,
                        Stream = stream.ID,
                        Person = await dbc.Person.FirstOrDefaultAsync(p => p.Email == suppression.EmailAddress),
                    };
                    if (suppression.SuppressionReason == "ManualSuppression" && suppression.Origin == "Recipient")
                    {
                        s.Reason = "Unsubscribe";
                    }
                    else { s.Reason = suppression.SuppressionReason; }
                    result.Add(s);
                }
            }
            return result.OrderByDescending(x => x.CreatedAt).ToList();
        }

        public async Task DeleteSuppressions(IEnumerable<EmailSuppression> SuppressionsToDelete)
        {
            List<PostmarkSuppressionChangeRequest> currentList;
            var streams = new Dictionary<string, List<PostmarkSuppressionChangeRequest>>();
            foreach (var s in SuppressionsToDelete)
            {
                var request = new PostmarkSuppressionChangeRequest()
                {
                    EmailAddress = s.Email,
                };
                if (!streams.TryGetValue(s.Stream, out currentList))
                {
                    currentList = new List<PostmarkSuppressionChangeRequest>();
                    streams[s.Stream] = currentList;
                }
                currentList.Add(request);
            }
            foreach (var kvp in streams)
            {
                client.DeleteSuppressions(kvp.Value, kvp.Key);
            }
        }
        public async Task<List<EmailMessage>> SearchMessagesAsync(TimeSpan offset, String recipient)
        {
            var result = new List<EmailMessage>();
            PostmarkMessageStreamListing streams = await client.ListMessageStreams();
            foreach (var stream in streams.MessageStreams)
            {
                var streamID = stream.ID;
                var messageResult = await client.GetOutboundMessagesAsync(recipient: recipient, messagestream: streamID);
                foreach (var message in messageResult.Messages)
                {
                    var msg = new EmailMessage()
                    {
                        MessageID = message.MessageID,
                        Attachments = message.Attachments.Count(),
                        From = message.From,
                        ReceivedAt = message.ReceivedAt + offset,
                        Status = message.Status,
                        Subject = message.Subject,
                        Stream = stream.Name
                    };
                    msg.Recipients.AddRange(message.Recipients);
                    result.Add(msg);
                }
            }
            return result;
        }
        public async Task<List<EmailMessage>> SearchMessagesAsync(TimeSpan offset, DateTime From, DateTime To)
        {
            var result = new List<EmailMessage>();
            var thisSet = new List<EmailMessage>();
            var recordOffset = 0;
            var count = 100;
            while (true)
            {
                thisSet = await SearchMessagesAsync(offset, recordOffset, count, From, To);
                foreach (var message in thisSet)
                {
                    foreach (var recipient in message.Recipients)
                    {
                        var msg = new EmailMessage();
                        message.CopyTo(msg);
                        msg.To = recipient;
                        result.Add(msg);
                    }
                }
                if (thisSet.Count < count) { break; }
                recordOffset += count;
            }
            return result.OrderByDescending(x => x.ReceivedAt).ToList();
        }

        public async Task<List<EmailMessage>> SearchMessagesAsync(TimeSpan offset, int recordOffset, int Count, DateTime From, DateTime To)
        {
            PostmarkMessageStreamListing streams = await client.ListMessageStreams();
            var result = new List<EmailMessage>();
            var sFrom = From.ToString("yyyy-MM-ddT00:00:00");
            var sTo = To.AddDays(1).AddSeconds(-1).ToString("yyyy-MM-ddThh:mm:ss");
            foreach (var stream in streams.MessageStreams)
            {

                var streamID = stream.ID;
                var messageResult = await client.GetOutboundMessagesAsync(
                                        offset: recordOffset,
                                        count: Count,
                                        fromDate: sFrom,
                                        toDate: sTo,
                                        messagestream: streamID
                                        );
                foreach (var message in messageResult.Messages)
                {
                    var msg = new EmailMessage()
                    {
                        MessageID = message.MessageID,
                        Attachments = message.Attachments.Count(),
                        From = message.From,
                        ReceivedAt = message.ReceivedAt + offset,
                        Status = message.Status,
                        Subject = message.Subject,
                        Stream = stream.Name
                    };
                    msg.Recipients.AddRange(message.Recipients);
                    result.Add(msg);
                }
            }
            return result;
        }

        public async Task<List<EmailMessageEvent>> GetEmailMessageDetailAsync(TimeSpan offset, EmailMessage message, string recipient)
        {
            var result = new List<EmailMessageEvent>();
            var details = await client.GetOutboundMessageDetailsAsync(message.MessageID);
            foreach (var detail in details.MessageEvents.Where(x => x.Recipient.ToLower() == recipient.ToLower()))
            {

                var ev = new EmailMessageEvent()
                {
                    Type = detail.Type,
                    ReceivedAt = detail.ReceivedAt + offset,
                    HtmlBody = details.HtmlBody,
                };
                result.Add(ev);
            }
            return result;
        }
    }

}
