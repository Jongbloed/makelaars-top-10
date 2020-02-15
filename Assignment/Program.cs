using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Assignment
{
    class WoonObject
    {
        public int MakelaarId { get; set; }
        public string MakelaarNaam { get; set; }
        public string Adres { get; set; }
    }

    class PaginaInfo
    {
        public int AantalPaginas { get; set; }
        public int HuidigePagina { get; set; }
    }

    class FundaResultaat
    {
        public WoonObject[] Objects { get; set; }
        public PaginaInfo Paging { get; set; }
    }

    class Program
    {
        static int Main()
        {
            var queue = new BlockingCollection<WoonObject[]>();
            Task taak;

            foreach (var (zoekOpdrachtLabel, zoekOpdracht) in new[] { ("Amsterdam", "amsterdam"), ("Amsterdam | Tuin", "amsterdam/tuin") })
            {
                Console.Clear();
                Console.WriteLine($"Top 10 makelaars koop {zoekOpdrachtLabel}");
                Console.WriteLine("Spanning erin houden? (y/n)");
                var topTenDisplayTask = new TopTienWeergaveTaak(
                    useDelay: Console.ReadKey().KeyChar == 'y',
                    inputQueue: queue, 
                    outputAction: makelaars => new ConsoleTable(makelaars).Print()
                );
                topTenDisplayTask.Start();
                Console.Clear();

                try
                {
                    using (var bron = new Fetcher(new WoonObjectBron(zoekOpdracht)))
                    {
                        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                        taak = bron.FetchAllAsync(queue, cancellationTokenSource.Token);

                        Console.ReadKey();
                        cancellationTokenSource.Cancel();
                    }
                    taak.Wait();
                }
                catch (AggregateException aggregateException)
                {
                    var unexpectedExceptions = aggregateException.Flatten().InnerExceptions.Where(e => !(e is TaskCanceledException)).ToArray();
                    foreach (var e in unexpectedExceptions)
                    {
                        Console.Error.WriteLine($"[{e.GetType().Name}]: {e.Message}");
                    }
                    if (unexpectedExceptions.Any())
                    {
                        Console.ReadKey();
                        return -1;
                    }
                }
                finally
                {
                    topTenDisplayTask.Stop();
                }
            }
            return 0;
        }
    }
}
