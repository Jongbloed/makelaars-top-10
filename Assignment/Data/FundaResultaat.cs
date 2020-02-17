namespace Assignment.Data
{
    public class WoonObject
    {
        public int MakelaarId { get; set; }
        public string MakelaarNaam { get; set; }
        public string Adres { get; set; }
    }

    public class PaginaInfo
    {
        public int AantalPaginas { get; set; }
        public int HuidigePagina { get; set; }
    }

    public class FundaResultaat
    {
        public WoonObject[] Objects { get; set; }
        public PaginaInfo Paging { get; set; }
    }
}
