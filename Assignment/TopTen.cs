using Assignment.Data;
using System.Collections.Generic;
using System.Linq;

namespace Assignment
{
    public class Makelaar
    {
        public int MakelaarId;
        public string MakelaarNaam;
        public int AantalListings;
    }

    public class TopTen
    {
        private readonly Dictionary<int, Makelaar> makelaars = new Dictionary<int, Makelaar>();

        public void AddWoonObjecten(WoonObject[] woonObjecten)
        {
            foreach (var woonObject in woonObjecten)
            {
                if (makelaars.ContainsKey(woonObject.MakelaarId))
                {
                    makelaars[woonObject.MakelaarId].AantalListings++;
                }
                else
                {
                    makelaars[woonObject.MakelaarId] = new Makelaar
                    {
                        MakelaarId = woonObject.MakelaarId,
                        MakelaarNaam = woonObject.MakelaarNaam,
                        AantalListings = 1
                    };
                }
            }
        }

        public Makelaar[] GetTopTen()
        {
            return makelaars.Values
                .OrderByDescending(x => x.AantalListings)
                .ThenBy(x => x.MakelaarNaam)
                .Take(10)
                .ToArray();
        }
    }
}
