namespace DreamTravel.Bot.DiscoverDreamTravelChances.SendEmail
{
    public class DreamTravelChanceEmail
    {
        public DreamTravelChanceEmail(string message, string recipient)
        {
            Message = message;
            Recipient = recipient;
        }

        public string Sender = "dreamtravelwebsite@gmail.com";
        public string Subject = "Today's Dream Travel Chances!";
        public string Message { get; set; }
        public string Recipient { get; set; }
    }
}
