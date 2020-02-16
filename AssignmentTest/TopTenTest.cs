using Assignment;
using NUnit.Framework;

namespace Tests
{
    public class Tests
    {
        [Test]
        public void Given_RecordsOf3SameBrokersAnd2Uniques_Returns_TableWith3Records()
        {
            // Arrange
            var topten = new TopTen();
            topten.AddWoonObjecten(new[] {
                new WoonObject {
                    Adres = "Zaanstraat 1", MakelaarId = 20043, MakelaarNaam = "Erik",
                },
                new WoonObject {
                    Adres = "Zaanstraat 2", MakelaarId = 20041, MakelaarNaam = "Bart",
                },
                new WoonObject {
                    Adres = "Bloemstraat 3", MakelaarId = 20043, MakelaarNaam = "Erik",
                },
                new WoonObject {
                    Adres = "Bloemstraat 100", MakelaarId = 20040, MakelaarNaam = "Brian",
                },
                new WoonObject {
                    Adres = "Maria Austrastraat 82", MakelaarId = 20043, MakelaarNaam = "Erik",
                },
            });

            // Act
            var table = topten.GetTopTen();

            // Assert
            Assert.AreEqual(3, table.Length);
        }

        [Test]
        public void Given_RecordsOf3SameBrokersAnd2Uniques_Returns_BrokerWith3RecordsFirst()
        {
            // Arrange
            var topten = new TopTen();
            topten.AddWoonObjecten(new[] {
                new WoonObject {
                    Adres = "Zaanstraat 2", MakelaarId = 20041, MakelaarNaam = "Bart",
                },
                new WoonObject {
                    Adres = "Bloemstraat 3", MakelaarId = 20043, MakelaarNaam = "Erik",
                },
                new WoonObject {
                    Adres = "Bloemstraat 100", MakelaarId = 20040, MakelaarNaam = "Brian",
                },
                new WoonObject {
                    Adres = "Zaanstraat 1", MakelaarId = 20043, MakelaarNaam = "Erik",
                },
                new WoonObject {
                    Adres = "Maria Austrastraat 82", MakelaarId = 20043, MakelaarNaam = "Erik",
                },
            });

            // Act
            var table = topten.GetTopTen();

            // Assert
            Assert.AreEqual("Erik", table[0].MakelaarNaam);
        }
    }
}