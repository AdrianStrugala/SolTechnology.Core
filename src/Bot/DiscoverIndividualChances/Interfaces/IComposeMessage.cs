namespace DreamTravel.Bot.DiscoverIndividualChances.Interfaces
{
    using Models;

    public interface IComposeMessage
    {
        string Execute(Chance chance, Subscription subscription);
    }
}