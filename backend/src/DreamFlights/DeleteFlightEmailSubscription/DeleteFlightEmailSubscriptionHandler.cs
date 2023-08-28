using System;
using DreamTravel.Domain.FlightEmailSubscriptions;
using DreamTravel.Infrastructure;

namespace DreamTravel.DreamFlights.DeleteFlightEmailSubscription
{
    public class DeleteFlightEmailSubscriptionHandler : ICommandHandler<DeleteFlightEmailSubscriptionCommand>
    {
        private readonly IFlightEmailSubscriptionRepository _flightEmailSubscriptionRepository;

        public DeleteFlightEmailSubscriptionHandler(IFlightEmailSubscriptionRepository flightEmailSubscriptionRepository)
        {
            _flightEmailSubscriptionRepository = flightEmailSubscriptionRepository;
        }


        public void Handle(int id)
        {
            _flightEmailSubscriptionRepository.Delete(id);
        }

        public CommandResult Handle(DeleteFlightEmailSubscriptionCommand command)
        {
            try
            {
                _flightEmailSubscriptionRepository.Delete(command.Id);
            }

            catch (Exception e)
            {
                return CommandResult.Failed(e.Message);
            }

            return CommandResult.Succeeded();

        }
    }
}