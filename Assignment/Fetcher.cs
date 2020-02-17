using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Linq;
using MoreLinq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net;
using System.Collections.Generic;
using System.Threading;
using Assignment.Data;

namespace Assignment
{
    public interface IWoonObjectBron : IDisposable
    {
        Task<FundaResultaat> HaalPagina(int pagina, CancellationToken cancellationToken);
    }

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

    public interface IFetchProgress
    {
        void Print();
        bool[] PagesComplete { get; set; }
    }

    class FetchProgress : IFetchProgress
    {
        public void Print()
        {
            Console.SetCursorPosition(2, 16);
            Console.Write($"|{new string(PagesComplete.Select(b => b ? '×' : ' ').ToArray())}| {GetProgressPercentageString()}");
        }
        public bool[] PagesComplete { get; set; } = new bool[0];
        string GetProgressPercentageString() =>
            $"{PagesComplete.Count(x => x) / (float)PagesComplete.Length:P0} geladen...";
    }

    public class Fetcher : IDisposable
    {
        private readonly IWoonObjectBron bron;
        private readonly IFetchProgress progress;
        private readonly BlockingCollection<WoonObject[]> outputQueue;

        public Fetcher(IWoonObjectBron bron, IFetchProgress progress, BlockingCollection<WoonObject[]> outputQueue)
        {
            this.bron = bron;
            this.progress = progress;
            this.outputQueue = outputQueue;
        }

        public void Dispose() => bron.Dispose();

        public async Task FetchAllAsync(CancellationToken cancellationToken)
        {
            var eerstePagina = await bron.HaalPagina(1, cancellationToken);
            var aantalPaginas = eerstePagina.Paging.AantalPaginas;
            if (aantalPaginas < 1)
            {
                throw new UnexpectedApiResponseException("Incorrect number of pages was returned: " + aantalPaginas);
            }
            progress.PagesComplete = new bool[aantalPaginas];
            progress.PagesComplete[0] = true;
            outputQueue.Add(eerstePagina.Objects);

            var resterendePaginaNummerBatches = Enumerable.Range(2, eerstePagina.Paging.AantalPaginas - 1)
                .Batch(99) // do not do > 100 requests per minute, also counting the first one
                .ToArray();

            await FetchBatchPerMinute(resterendePaginaNummerBatches, cancellationToken);
        }

        (int paginaNummer, Task taak)[] StartPaginaBatch(IEnumerable<int> batch, CancellationToken cancellationToken) {
            return batch.Select(paginaNummer =>
            {
                var taak = bron.HaalPagina(paginaNummer, cancellationToken).ContinueWith(t =>
                {
                    var woonObjecten = t.Result.Objects;
                    progress.PagesComplete[paginaNummer - 1] = true;
                    outputQueue.Add(woonObjecten);
                }, TaskContinuationOptions.OnlyOnRanToCompletion);
                return (paginaNummer, taak);
            }).ToArray();
        }

        async Task FetchBatchPerMinute(IEnumerable<int>[] resterendePaginaNummerBatches, CancellationToken cancellationToken)
        {
            for (var batchIndex = 0; batchIndex < resterendePaginaNummerBatches.Length; batchIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batch = resterendePaginaNummerBatches[batchIndex];
                var timer = Stopwatch.StartNew();
                var takenBatch = StartPaginaBatch(batch, cancellationToken);

                await Task.WhenAll(takenBatch.Select(x => x.taak));

                timer.Stop();
                // if this was not the last round, we need to wait until our minute's up before we can do more API calls
                if (batchIndex + 1 < resterendePaginaNummerBatches.Length)
                {
                    var restVanMinuut = TimeSpan.FromMinutes(1).Subtract(timer.Elapsed);
                    await Task.Delay(restVanMinuut, cancellationToken);
                }
            }
        }
    }
}
