using System;
using System.IO;
using System.Net.Http;
using System.Xml;
using Newtonsoft.Json;
using System.Xml.Serialization;
using System.Linq;
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
                var topTenDisplayTask = new TopTienWeergaveTaak(queue, makelaars => new ConsoleTable(makelaars).Print());
                topTenDisplayTask.Start();
                topTenDisplayTask.Slow = Console.ReadKey().KeyChar == 'y';
                Console.Clear();

                var bron = new ZoekOpdracht(zoekOpdracht);
                bron.FetchAllAsync(queue); // TODO do not block

                Console.ReadKey();
                topTenDisplayTask.Stop();
            }
        }
    }
}
