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

        public Makelaar Copy() => (Makelaar)MemberwiseClone();
    }

    class TopTen
    {
        readonly ConcurrentDictionary<int, Makelaar> makelaars = new ConcurrentDictionary<int, Makelaar>();

        public void AddListings(IEnumerable<WoonObject> listings)
        {
            foreach (var l in listings)
            {
                makelaars.AddOrUpdate(
                    key: l.MakelaarId,
                    addValue: new Makelaar { MakelaarId = l.MakelaarId, MakelaarNaam = l.MakelaarNaam, AantalListings = 1 },
                    updateValueFactory: (int makelaarId, Makelaar current) =>
                    {
                        var copy = current.Copy();
                        copy.AantalListings++; // TODO is this necessary? Doesn't ConcurrentDict lock the item for us until this lambda is done?
                        return copy;
                    }
                );
            }
        }

        public Makelaar[] GetTopTen() // TODO test the output of this method
        {
            var snapshot = makelaars.ToArray()
                .Select(x => x.Value.Copy())
                .OrderByDescending(m => m.AantalListings)
                .ThenBy(x => x.MakelaarNaam)
                .Take(10)
                .ToArray();
            return snapshot;
        }
    }
}
