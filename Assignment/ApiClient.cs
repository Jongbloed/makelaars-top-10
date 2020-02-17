using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Assignment
{
    public interface IApiClient : IDisposable
    {
        Task<HttpResponseMessage> GetAsync(string url, CancellationToken cancellationToken);
    }

    public class ApiClient : IApiClient
    {
        private readonly HttpClient httpClient;

        public ApiClient()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://partnerapi.funda.nl")
            };
        }

        public void Dispose() => httpClient.Dispose();

        public Task<HttpResponseMessage> GetAsync(string url, CancellationToken cancellationToken)
        {
            return httpClient.GetAsync(url, cancellationToken);
        }
    }
}
