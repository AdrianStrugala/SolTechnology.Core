namespace DreamTravel.Bot.DiscoverIndividualChances.Models
{
    using Infrastructure.Email;

    public class IndividualChanceEmail : IEmail
    {
        public string Sender { get; }
        public string Subject { get; }
        public string Message { get; set; }
        public string Recipient { get; set; }
    }
}
