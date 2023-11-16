namespace DreamTravel.Identity.HttpClients.Aiia;

public class AiiaApi : IAiiaApi
{
    private readonly HttpClient _aiiaHttpClient;

    public AiiaApi(HttpClient aiiaHttpClient)
    {
        _aiiaHttpClient = aiiaHttpClient;
    }

    public async Task<CreatePaymentResponse> CreateAcceptPayment(CreateAcceptPaymentRequest paymentRequest)
    {
        var apiResponse = await _aiiaHttpClient
            .CreateRequest("v2/payments/accept")
            .WithBody(paymentRequest)
            .PostAsync<CreatePaymentResponse>();
        return apiResponse;
    }
}