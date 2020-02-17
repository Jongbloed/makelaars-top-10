using Assignment.Data;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Assignment
{
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
                var progress = new FetchProgress();
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                var topTenDisplayTask = new TopTienWeergaveTaak(
                    useDelay: Console.ReadKey().KeyChar == 'y',
                    inputQueue: queue,
                    progress: progress,
                    outputAction: makelaars => new ConsoleTable(makelaars).Print()
                );
                topTenDisplayTask.Start(cancellationTokenSource.Token);
                Console.Clear();

                try
                {
                    using (var bron = new Fetcher(new WoonObjectBron(zoekOpdracht, new ApiClient()), progress, queue))
                    {
                        taak = bron.FetchAllAsync(cancellationTokenSource.Token);

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
            }
            return 0;
        }
    }
}
