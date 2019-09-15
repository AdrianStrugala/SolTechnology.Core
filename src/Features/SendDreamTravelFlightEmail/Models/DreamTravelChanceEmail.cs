namespace DreamTravel.Features.SendDreamTravelFlightEmail.Models
{
    public class DreamTravelChanceEmail : Infrastructure.Email.IEmail
    {
        public DreamTravelChanceEmail(string message, string recipient)
        {
            Message = message;
            Recipient = recipient;
        }

        public string Subject => "Today's Dream Travel Chances!";

        public string Sender => "dreamtravelwebsite@gmail.com";

        public string Message { get; set; }
        public string Recipient { get; set; }
    }
}
