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
        private readonly IGetSubscriptions _getSubscriptions;
        private readonly IGetFlightsFromSkyScanner _getFlightsFromSkyScanner;
        private readonly IComposeMessage _composeMessage;

        public DiscoverIndividualChances(IGetSubscriptions getSubscriptions, IGetFlightsFromSkyScanner getFlightsFromSkyScanner, IComposeMessage composeMessage)
        {
            _getSubscriptions = getSubscriptions;
            _getFlightsFromSkyScanner = getFlightsFromSkyScanner;
            _composeMessage = composeMessage;
        }
        public async Task Execute()
        {
            List<Subscription> subscriptions = _getSubscriptions.Execute();

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
