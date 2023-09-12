namespace DreamTravel.Identity.HttpClients.Aiia;

public interface IAiiaApi
{
    Task<CreatePaymentResponse> CreateAcceptPayment(CreateAcceptPaymentRequest paymentRequest);
}