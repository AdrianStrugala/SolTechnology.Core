using System.Reflection;
using DreamTravel.Domain.FlightEmailSubscriptions;

namespace DreamTravel.DreamFlights.UpdateSubscriptions
{
    public class UpdateSubscriptionsHandler : IUpdateSubscriptions
    {
        private readonly ISubscriptionDaysRepository _subscriptionDaysRepository;

        public UpdateSubscriptionsHandler(ISubscriptionDaysRepository subscriptionDaysRepository)
        {
            _subscriptionDaysRepository = subscriptionDaysRepository;
        }

        public void Handle(UpdateSubscriptionsCommand command)
        {
            if (command.Events.Count == 0)
            {
                return;
            }

            var subscriptionsDictionary = _subscriptionDaysRepository.GetByUser(command.UserId);

            foreach (var dayChangedEvent in command.Events)
            {
                if (!subscriptionsDictionary.ContainsKey(dayChangedEvent.SubscriptionId)) continue;

                PropertyInfo dayProperty = subscriptionsDictionary[dayChangedEvent.SubscriptionId].GetType()
                    .GetProperty(dayChangedEvent.Day, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                if (dayProperty == null)
                {
                    continue;
                }

                dayProperty.SetValue(subscriptionsDictionary[dayChangedEvent.SubscriptionId], dayChangedEvent.Value);
            }

            foreach (var subscriptionDays in subscriptionsDictionary.Values)
            {
                _subscriptionDaysRepository.Update(subscriptionDays);
            }
        }
    }
}
