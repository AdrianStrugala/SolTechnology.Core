namespace DreamTravel.Bot.DiscoverIndividualChances.Interfaces
{
    using System.Threading.Tasks;
    using Models;
    using SharedModels;

    public interface IGetFlightsFromSkyScanner
    {
        Task<Chance> Execute(Subscription subscription);
    }
}