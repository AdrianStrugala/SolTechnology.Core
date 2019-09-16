namespace DreamTravel.Bot.DiscoverIndividualChances.Models
{
    public class Subscription
    {
        public string UserName { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public int LengthOfStay { get; set; }
        public string Email { get; set; }
        public string Currency { get; set; }
    }
}
