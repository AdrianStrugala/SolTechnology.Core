namespace DreamTravel.Bot.DiscoverIndividualChances.DataAccess
{
    using Interfaces;
    using Models;
    using SharedModels;

    public class GetFlightsFromSkyScanner : IGetFlightsFromSkyScanner
    {
        public Chance Execute(Subscription subscription)
        {
            Chance result = new Chance();

            return result;
        }
    }
}
