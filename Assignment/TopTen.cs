using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assignment
{
    class Makelaar
    {
        public int MakelaarId;
        public string MakelaarNaam;
        public int AantalListings;
    }

    class TopTen
    {
        private readonly Dictionary<int, Makelaar> makelaars = new Dictionary<int, Makelaar>();

        public void AddListings(IEnumerable<WoonObject> listings)
        {
            foreach (var l in listings)
            {
                if (makelaars.ContainsKey(l.MakelaarId))
                {
                    makelaars[l.MakelaarId].AantalListings++;
                }
                else
                {
                    makelaars[l.MakelaarId] = new Makelaar
                    {
                        MakelaarId = l.MakelaarId,
                        MakelaarNaam = l.MakelaarNaam,
                        AantalListings = 1
                    };
                }
            }
        }

        public Makelaar[] GetTopTen() // TODO test the output of this method
        {
            return makelaars.ToArray()
                .Select(x => x.Value)
                .OrderByDescending(x => x.AantalListings)
                .ThenBy(x => x.MakelaarNaam)
                .Take(10)
                .ToArray();
        }
    }
}
