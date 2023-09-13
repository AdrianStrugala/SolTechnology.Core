namespace DreamTravel.Infrastructure.Email
{
    public interface IEmail
    {
        string Sender { get; }
        string Subject { get; }
        string Message { get; set; }
        string Recipient { get; set; }
    }
}
