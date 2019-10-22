using DreamTravel.Infrastructure.Email;

namespace DreamTravel.Features.DreamFlight.SendOrderedFlightEmail.Models
{
    public class OrderedFlightEmail : IEmail
    {
        public OrderedFlightEmail(string message, string recipient, string subject)
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
