using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Linq;
using MoreLinq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Assignment
{
    interface IWoonObjectBron : IDisposable
    {
        Task<FundaResultaat> HaalPagina(int pagina);
    }

    class WoonObjectBron : IWoonObjectBron
    {
        private readonly string pageUrlTemplate;
        private readonly HttpClient httpClient;

        public WoonObjectBron(string zoekOpdracht)
        {
            pageUrlTemplate = $@"/feeds/Aanbod.svc/json/ac1b0b1572524640a0ecc54de453ea9f/?type=koop&zo=/{zoekOpdracht}/&page={{0}}&pagesize=25";
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://partnerapi.funda.nl")
            };
        }

        private string PageUrl(int pagina) => string.Format(pageUrlTemplate, pagina);

        public async Task<FundaResultaat> HaalPagina(int pagina)
        {
            var response = await httpClient.GetAsync(PageUrl(pagina));
            var rawResult = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<FundaResultaat>(rawResult);
            return result;
        }

        public void Dispose() => httpClient.Dispose();
    }

    class Fetcher : IDisposable
    {
        private readonly IWoonObjectBron bron;

        public Fetcher(IWoonObjectBron bron)
        {
            this.bron = bron;
        }

        public void Dispose() => bron.Dispose();

        public async Task FetchAllAsync(BlockingCollection<WoonObject[]> outputQueue)
        {
            var eerstePagina = await bron.HaalPagina(1);
            outputQueue.Add(eerstePagina.Objects);

            var resterendePaginaNummerBatches = Enumerable.Range(2, eerstePagina.Paging.AantalPaginas - 1)
                .Batch(99) /* do not do > 100 requests per minute) */
                .ToArray();

            for (var batchIndex = 0; batchIndex < resterendePaginaNummerBatches.Length; batchIndex++) {
                var batch = resterendePaginaNummerBatches[batchIndex];
                var timer = Stopwatch.StartNew();
                await Task.WhenAll(batch.Select(async paginaNummer =>
                {
                    var fundaResultaat = await bron.HaalPagina(paginaNummer);
                    var woonObjecten = fundaResultaat.Objects;
                    outputQueue.Add(woonObjecten);
                }));
                timer.Stop();
                // if this was not the last round, we need to wait until our minute's up before we can do more API calls
                if (batchIndex + 1 < resterendePaginaNummerBatches.Length)
                {
                    var restVanMinuut = TimeSpan.FromMinutes(1).Subtract(timer.Elapsed);
                    await Task.Delay(restVanMinuut);
                }
            }
        }
    }
}
