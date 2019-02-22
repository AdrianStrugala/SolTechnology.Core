namespace DreamTravel.Bot.DiscoverIndividualChances
{
    using DataAccess;
    using Interfaces;
    using Models;
    using SharedModels;
    using System.Collections.Generic;

    class DiscoverIndividualChances : IDiscoverIndividualChances
    {
        private readonly IProvideSubscriptions _provideSubscriptions;
        private readonly IGetFlightsFromSkyScanner _getFlightsFromSkyScanner;

        public DiscoverIndividualChances(IProvideSubscriptions provideSubscriptions, IGetFlightsFromSkyScanner getFlightsFromSkyScanner)
        {
            _provideSubscriptions = provideSubscriptions;
            _getFlightsFromSkyScanner = getFlightsFromSkyScanner;
        }
        public void Execute()
        {
            List<Subscription> subscriptions = _provideSubscriptions.Execute();

            foreach (var subscription in subscriptions)
            {
                Chance chance = _getFlightsFromSkyScanner.Execute(subscription);
            }
        }
    }
}
