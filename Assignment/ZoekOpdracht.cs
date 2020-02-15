using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Assignment
{
    class ZoekOpdracht
    {
        private readonly Func<int, string> PageUrl;
        public ZoekOpdracht(params string[] zoekOpdracht)
        {
            string zo = string.Join("/", zoekOpdracht);
            PageUrl = page => $@"/feeds/Aanbod.svc/json/ac1b0b1572524640a0ecc54de453ea9f/?type=koop&zo=/{zo}/&page={page}&pagesize=25";
        }

        public async Task FetchAllAsync(BlockingCollection<WoonObject[]> outputQueue)
        {
            using (var httpClient = new HttpClient { BaseAddress = new Uri("http://partnerapi.funda.nl") })
            {
                var paginaInfo = await getPage(1);

                var resterendePaginaTaken = Enumerable.Range(2, paginaInfo.AantalPaginas - 1)
                    .Select(getPage)
                    .ToArray();

                foreach (var batch in resterendePaginaTaken.Batch(99)/* do not do > 100 requests per minute) */.ToArray()) {
                    var time = Stopwatch.StartNew();
                    await Task.WhenAll(batch);
                    time.Stop();
                    var theresProbablyMore = batch.Count() == 99;
                    if(theresProbablyMore)
                        await Task.Delay(TimeSpan.FromMinutes(1).Subtract(time.Elapsed)); // wait till the minute's up :)
                }

                //await Task.WhenAll(resterendePaginaTaken); // TODO retry faulted

                async Task<PaginaInfo> getPage(int page)
                {
                    var response = await httpClient.GetAsync(PageUrl(page));
                    var rawResult = await response.Content.ReadAsStringAsync();
                    try
                    {
                        var result = JsonConvert.DeserializeObject<FundaResultaat>(rawResult);
                        outputQueue.Add(result.Objects);
                        return result.Paging;
                    }
                    catch(Exception e) // 401 Request limit exceeded: handle + prevent
                    {
                        Console.SetCursorPosition(2, 30);
                        Console.WriteLine($"{e.GetType().Name}: {e.Message}"); // TODO log and retry
                        return null;
                    }
                }
            }
        }
    }
}
