namespace DreamTravel.Bot.DiscoverIndividualChances.Interfaces
{
    using Models;
    using SharedModels;

    public interface IGetFlightsFromSkyScanner
    {
        Chance Execute(Subscription subscription);
    }
}