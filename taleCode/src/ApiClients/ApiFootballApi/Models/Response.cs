namespace SolTechnology.TaleCode.ApiClients.ApiFootballApi.Models;

public class Response
{
    public Player Player { get; set; }
    public DateTime Update { get; set; }
    public List<Transfer> Transfers { get; set; }
}