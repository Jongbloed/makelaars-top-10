using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using Assignment.Data;
using System;

namespace Assignment
{
    public interface IWoonObjectBron : IDisposable
    {
        Task<FundaResultaat> HaalPagina(int pagina, CancellationToken cancellationToken);
    }

    public class WoonObjectBron : IWoonObjectBron
    {
        private readonly string pageUrlTemplate;
        private readonly IApiClient apiClient;

        public WoonObjectBron(string zoekOpdracht, IApiClient apiClient)
        {
            pageUrlTemplate = $@"/feeds/Aanbod.svc/json/ac1b0b1572524640a0ecc54de453ea9f/?type=koop&zo=/{zoekOpdracht}/&page={{0}}&pagesize=25";
            this.apiClient = apiClient;
        }

        private string PageUrl(int pagina) => string.Format(pageUrlTemplate, pagina);

        public async Task<FundaResultaat> HaalPagina(int pagina, CancellationToken cancellationToken)
        {
            var response = await apiClient.GetAsync(PageUrl(pagina), cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var rawResult = await response.Content.ReadAsStringAsync();
                try
                {
                    var fundaResultaat = JsonConvert.DeserializeObject<FundaResultaat>(rawResult);
                    if (fundaResultaat.Paging == null)
                    {
                        throw new UnexpectedApiResponseException("Response malformed: Paging section missing");
                    }
                    return fundaResultaat;
                }
                catch (JsonException jsonException)
                {
                    throw new UnexpectedApiResponseException(jsonException);
                }
            }
            else if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                // Funda API uses code 401 when request limit is exceeded
                throw new RequestLimitExceededException(response.ReasonPhrase);
            }
            else
            {
                throw new UnexpectedApiResponseException(response.StatusCode, response.ReasonPhrase);
            }
        }

        public void Dispose() => apiClient.Dispose();
    }
}
