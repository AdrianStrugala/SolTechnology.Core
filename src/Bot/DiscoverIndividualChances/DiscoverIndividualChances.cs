namespace DreamTravel.Bot.DiscoverIndividualChances
{
    using Interfaces;
    using Models;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Infrastructure.Email;
    using Newtonsoft.Json;
    using Chance = Models.Chance;

    class DiscoverIndividualChances : IDiscoverIndividualChances
    {
        private readonly IProvideSubscriptions _provideSubscriptions;
        private readonly IGetFlightsFromSkyScanner _getFlightsFromSkyScanner;
        private readonly IComposeMessage _composeMessage;

        public DiscoverIndividualChances(IProvideSubscriptions provideSubscriptions, IGetFlightsFromSkyScanner getFlightsFromSkyScanner, IComposeMessage composeMessage)
        {
            _provideSubscriptions = provideSubscriptions;
            _getFlightsFromSkyScanner = getFlightsFromSkyScanner;
            _composeMessage = composeMessage;
        }
        public async Task Execute()
        {
            List<Subscription> subscriptions = _provideSubscriptions.Execute();

            foreach (var subscription in subscriptions)
            {
                Chance chance = await _getFlightsFromSkyScanner.Execute(subscription);

                EmailAgent.Send(new IndividualChanceEmail(
                    _composeMessage.Execute(chance, subscription),
                    subscription.Email,
                    $"{subscription.UserName} your travel to {subscription.To} is here!"));
            }
        }
    }
}
