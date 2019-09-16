using DreamTravel.DatabaseData;

namespace DreamTravel.Bot.DiscoverIndividualChances
{
    using Interfaces;
    using Models;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Infrastructure.Email;
    using Chance = Models.Chance;

    class DiscoverIndividualChances : IDiscoverIndividualChances
    {
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IGetFlightsFromSkyScanner _getFlightsFromSkyScanner;
        private readonly IComposeMessage _composeMessage;

        public DiscoverIndividualChances(ISubscriptionRepository subscriptionRepository, IGetFlightsFromSkyScanner getFlightsFromSkyScanner, IComposeMessage composeMessage)
        {
            _subscriptionRepository = subscriptionRepository;
            _getFlightsFromSkyScanner = getFlightsFromSkyScanner;
            _composeMessage = composeMessage;
        }
        public async Task Execute()
        {
            List<Subscription> subscriptions = _subscriptionRepository.GetSubscriptions();

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
