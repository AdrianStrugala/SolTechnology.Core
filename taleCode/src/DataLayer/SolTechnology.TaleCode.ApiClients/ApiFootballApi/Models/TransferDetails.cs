namespace SolTechnology.TaleCode.ApiClients.ApiFootballApi.Models
{
    public class TransferDetails
    {
        public string Get { get; set; }
        public Parameters Parameters { get; set; }
        public List<object> Errors { get; set; }
        public int Results { get; set; }
        public Paging Paging { get; set; }
        public List<Response> Response { get; set; }
    }
}
