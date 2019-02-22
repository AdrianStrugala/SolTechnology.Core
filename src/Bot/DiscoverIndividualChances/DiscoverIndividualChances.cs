namespace DreamTravel.Bot.DiscoverIndividualChances
{
    using Interfaces;
    using Models;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using DataAccess;
    using SharedModels;

    class DiscoverIndividualChances : IDiscoverIndividualChances
    {
        private readonly IProvideSubscriptions _provideSubscriptions;
        private readonly IGetFlightsFromSkyScanner _getFlightsFromSkyScanner;

        public DiscoverIndividualChances(IProvideSubscriptions provideSubscriptions, IGetFlightsFromSkyScanner getFlightsFromSkyScanner)
        {
            _provideSubscriptions = provideSubscriptions;
            _getFlightsFromSkyScanner = getFlightsFromSkyScanner;
        }
        public async Task Execute()
        {
            List<Subscription> subscriptions = _provideSubscriptions.Execute();

            foreach (var subscription in subscriptions)
            {
                Chance chance = await _getFlightsFromSkyScanner.Execute(subscription);
            }
        }
    }
}
