namespace DreamTravel.Bot.DiscoverIndividualChances.Models
{
    using Infrastructure.Email;

    public class IndividualChanceEmail : IEmail
    {
        public IndividualChanceEmail(string message, string recipient, string subject)
        {
            Message = message;
            Recipient = recipient;
            Subject = subject;
        }

        public string Sender => "dreamtravelwebsite@gmail.com";
        public string Subject { get; }
        public string Message { get; set; }
        public string Recipient { get; set; }
    }
}
