using DreamTravel.Domain.FlightEmailSubscriptions;
using DreamTravel.Infrastructure;

namespace DreamTravel.DreamFlights.SubscribeForFlightEmail
{
    public class SubscribeForFlightEmailHandler : ICommandHandler<SubscribeForFlightEmailsCommand>
    {
        private readonly IFlightEmailSubscriptionRepository _flightEmailSubscriptionRepository;
        private readonly ISubscriptionDaysRepository _subscriptionDaysRepository;

        public SubscribeForFlightEmailHandler(
            IFlightEmailSubscriptionRepository flightEmailSubscriptionRepository,
            ISubscriptionDaysRepository subscriptionDaysRepository)
        {
            _flightEmailSubscriptionRepository = flightEmailSubscriptionRepository;
            _subscriptionDaysRepository = subscriptionDaysRepository;
        }
        public CommandResult Handle(SubscribeForFlightEmailsCommand command)
        {
            command.SubscriptionDays.FlightEmailSubscriptionId = _flightEmailSubscriptionRepository.Insert(command.FlightEmailSubscription);

            _subscriptionDaysRepository.Insert(command.SubscriptionDays);

            return CommandResult.Succeeded();
        }

    }
}
