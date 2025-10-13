using DnsClient;
using DnsClient.Protocol;
using PostmarkDotNet;
using PostmarkDotNet.Model;

namespace U3A.Super.Services
{
    public class PostmarkAdminService
    {

        private readonly PostmarkAdminClient client;

        public PostmarkAdminService(IConfiguration config)
        {
            U3ASecrets secrets = new(config);
            client = new PostmarkAdminClient(secrets.POSTMARK_ADMIN_KEY);
        }

        public async Task<PostmarkServerList> ListServersAsync()
        {
            return await client.GetServersAsync();
        }

        public async Task<PostmarkServer> CreateServer(string ServerName)
        {
            PostmarkServer? result = (await ListServersAsync()).Servers.FirstOrDefault(x => x.Name.ToLower() == ServerName.ToLower());
            result ??= await client.CreateServerAsync(ServerName,
                                trackLinks: LinkTrackingOptions.HtmlAndText,
                                trackOpens: true);
            return result;
        }

        public async Task<string> DeleteServer(string APIkey)
        {
            string result = string.Empty;
            PostmarkServer? server = default;
            foreach (PostmarkServer? s in (await ListServersAsync()).Servers)
            {
                foreach (string? t in s.ApiTokens)
                {
                    if (t == APIkey) { server = s; break; }
                }
                if (server != null) { break; }
            }
            if (server != null)
            {
                try
                {
                    _ = await client.DeleteServerAsync(server.ID);
                }
                catch (Exception) { throw; }
                finally { result = server.Name; }
            }
            return result;
        }

        public async Task<PostmarkDomainList> ListDomainsAsync()
        {
            return await client.GetDomainsAsync();
        }

        public async Task<IEnumerable<PostmarkCompleteDomain>> ListCompleteDomainsAsync()
        {
            List<PostmarkCompleteDomain> result = [];
            foreach (PostmarkBaseDomain? d in (await client.GetDomainsAsync()).Domains)
            {
                PostmarkCompleteDomain completeDomain = await GetDomainDetailsAsync(d.ID);
                if (completeDomain != null) { result.Add(completeDomain); }
            }
            return result;
        }

        public async Task<PostmarkBaseDomain> CreateDomain(string DomainName)
        {
            PostmarkBaseDomain? result = (await ListDomainsAsync()).Domains.FirstOrDefault(x => x.Name.ToLower() == DomainName.ToLower());
            result ??= await client.CreateDomainAsync(DomainName);
            return result;
        }

        public async Task<PostmarkCompleteDomain> GetDomainDetailsAsync(int ID)
        {
            return await client.GetDomainAsync(ID);
        }

        public async Task<string> DeleteDomain(string DomainName)
        {
            string result = string.Empty;
            PostmarkBaseDomain? domain = (await ListDomainsAsync()).Domains.FirstOrDefault(x => x.Name.ToLower() == DomainName.ToLower());
            if (domain != null)
            {
                try
                {
                    _ = await client.DeleteDomainAsync(domain.ID);
                }
                catch (Exception) { throw; }
                finally { result = domain.Name; }
            }
            return result;
        }
        public async Task<PostmarkCompleteDomain> VerifyDomainDkim(PostmarkCompleteDomain domain)
        {
            LookupClientOptions options = new() { UseCache = false };
            LookupClient dnsLookup = new(options);
            IDnsQueryResponse dnsResult = await dnsLookup.QueryAsync(domain.DKIMPendingHost, QueryType.TXT);
            if (dnsResult.Answers.Count == 0) { throw new Exception($"Unable to resolve hostname [{domain.DKIMPendingHost}]"); }
            ;
            TxtRecord? answer = dnsResult.Answers[0] as TxtRecord;
            if (answer!.Text.First() != domain.DKIMPendingTextValue) { throw new Exception($"Hostname [{domain.DKIMPendingHost}] of type TXT found but text values do not match."); }
            ;
            PostmarkCompleteDomain result = await client.VerifyDomainDkim(domain.ID);
            return result;
        }
        public async Task<PostmarkCompleteDomain> VerifyDomainReturnPath(PostmarkCompleteDomain domain)
        {
            LookupClient dnsLookup = new();
            string bouncePath = $"pm-bounces.{domain.Name}";
            IDnsQueryResponse dnsResult = await dnsLookup.QueryAsync(bouncePath, QueryType.CNAME);
            if (dnsResult.Answers.Count == 0) { throw new Exception($"Unable to resolve hostname [{bouncePath}]"); }
            ;
            string canonicalName = (dnsResult.Answers[0] as CNameRecord)!.CanonicalName;
            if (canonicalName.EndsWith(".")) { canonicalName = canonicalName.TrimEnd('.'); }
            if (canonicalName != domain.ReturnPathDomainCNAMEValue) { throw new Exception($"Hostname [{bouncePath}] of type CNAME found but values do not match."); }
            ;
            PostmarkCompleteDomain result = await client.VerifyDomainReturnPath(domain.ID);
            return result;
        }

    }
}
