
using System.Net.Http;
using System.Threading.Tasks;

namespace U3A.Services.APIClient;
public interface ISimpleHttpClient
{
    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request);
}