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
        return await _aiiaHttpClient.PostAsync<CreateAcceptPaymentRequest, CreatePaymentResponse>(
            $"v2/payments/accept",
            paymentRequest);
    }
}