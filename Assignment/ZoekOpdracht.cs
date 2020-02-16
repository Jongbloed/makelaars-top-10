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

namespace Assignment
{
    interface IWoonObjectBron : IDisposable
    {
        Task<FundaResultaat> HaalPagina(int pagina, CancellationToken cancellationToken);
    }

    class RequestLimitExceededException : Exception
    {
        public RequestLimitExceededException(string reasonPhrase)
            : base(reasonPhrase)
        { }
    }

    class UnexpectedApiResponseException : Exception
    {
        public UnexpectedApiResponseException(string message)
            : base(message)
        { }

        public UnexpectedApiResponseException(Exception innerException)
            : base("Failed to parse response from API", innerException)
        { }

        public UnexpectedApiResponseException(HttpStatusCode statusCode, string reasonPhrase) 
            : base($"Unexpected status code returned from API: [{statusCode}] {reasonPhrase}")
        { }
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

        public async Task<FundaResultaat> HaalPagina(int pagina, CancellationToken cancellationToken)
        {
            var response = await httpClient.GetAsync(PageUrl(pagina), cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var rawResult = await response.Content.ReadAsStringAsync();
                try
                {
                    var fundaResultaat = JsonConvert.DeserializeObject<FundaResultaat>(rawResult);
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

        public void Dispose() => httpClient.Dispose();
    }

    class FetchProgress
    {
        public void Print()
        {
            Console.SetCursorPosition(2, 16);
            Console.Write($"|{new string(PagesComplete.Select(b => b ? '×' : ' ').ToArray())}| {ProgressPercentageString()}");
        }
        public bool[] PagesComplete = new bool[0];
        public string ProgressPercentageString() =>
            $"{PagesComplete.Count(x => x) / (float)PagesComplete.Length:P0} geladen...";
    }

    class Fetcher : IDisposable
    {
        private readonly IWoonObjectBron bron;

        public Fetcher(IWoonObjectBron bron)
        {
            this.bron = bron;
        }

        public void Dispose() => bron.Dispose();

        public async Task FetchAllAsync(BlockingCollection<WoonObject[]> outputQueue, FetchProgress progress, CancellationToken cancellationToken)
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

            (int paginaNummer, Task taak)[] startPaginaBatch(IEnumerable<int> batch) {
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

            var resterendePaginaNummerBatches = Enumerable.Range(2, eerstePagina.Paging.AantalPaginas - 1)
                .Batch(99) // do not do > 100 requests per minute, also counting the first one
                .ToArray();

            for (var batchIndex = 0; batchIndex < resterendePaginaNummerBatches.Length; batchIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batch = resterendePaginaNummerBatches[batchIndex];
                var timer = Stopwatch.StartNew();
                var takenBatch = startPaginaBatch(batch);

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
