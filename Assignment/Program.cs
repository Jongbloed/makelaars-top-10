using System;
using System.Collections.Concurrent;

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
        static void Main()
        {
            var queue = new BlockingCollection<WoonObject[]>();
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

                using (var bron = new Fetcher(new WoonObjectBron(zoekOpdracht)))
                {
                    bron.FetchAllAsync(queue);

                    Console.ReadKey();
                }
                topTenDisplayTask.Stop();
            }
        }
    }
}
