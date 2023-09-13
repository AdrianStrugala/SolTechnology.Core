namespace DreamTravel.Identity.HttpClients.Aiia;

public class CreateAcceptPaymentRequest
{
    public decimal amount { get; set; }
    public string currency { get; set; }
    public string schemeId { get; set; }
    public string referece { get; set; }
    public string destinationId { get; set; }
    public string preselectedCountry { get; set; }
    public string redirectUrl { get; set; }
}