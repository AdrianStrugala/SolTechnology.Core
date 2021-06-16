using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using DreamTravel.Domain.FlightEmailSubscriptions;
using DreamTravel.DreamFlights.UpdateSubscriptions;
using NSubstitute;
using Xunit;

namespace DreamTravel.FeaturesTests.UpdateSubscriptions
{
    public class UpdateSubscriptionsHandlerTests
    {
        private readonly ISubscriptionDaysRepository _repository;
        private readonly UpdateSubscriptionsHandler _sut;
        private readonly Fixture _fixture;

        public UpdateSubscriptionsHandlerTests()
        {
            _fixture = new Fixture();

            _repository = Substitute.For<ISubscriptionDaysRepository>();

            _sut = new UpdateSubscriptionsHandler(_repository);
        }

        [Fact]
        public void Handle_NoEvents_RepositoriesAreNotCalled()
        {
            //Arrange
            UpdateSubscriptionsCommand command = new UpdateSubscriptionsCommand();
            command.Events = new List<DayChangedEvent>();


            //Act
            _sut.Handle(command);


            //Assert
            _repository.Received(0).GetByUser(Arg.Any<Guid>());
            _repository.Received(0).Update(Arg.Any<SubscriptionDays>());
        }


        [Fact]
        public void Handle_FourEvents_UpdateIsCalledForChangedSubscription()
        {
            //Arrange
            Dictionary<long, SubscriptionDays> subscriptions = new Dictionary<long, SubscriptionDays>();
            subscriptions.Add(1, new SubscriptionDays());

            _repository.GetByUser(Arg.Any<Guid>()).Returns(subscriptions);

            UpdateSubscriptionsCommand command = new UpdateSubscriptionsCommand();
            command.Events = _fixture.CreateMany<DayChangedEvent>(4).ToList();
            foreach (var dayChangedEvent in command.Events)
            {
                dayChangedEvent.SubscriptionId = 1;
            }


            //Act
            _sut.Handle(command);


            //Assert
            _repository.Received(1).GetByUser(Arg.Any<Guid>());
            _repository.Received(1).Update(Arg.Any<SubscriptionDays>());
        }

        [Fact]
        public void Handle_EventForNotExistingSubscription_ValuesAreNotChanged()
        {
            //Arrange
            Dictionary<long, SubscriptionDays> subscriptions = new Dictionary<long, SubscriptionDays>();
            subscriptions.Add(1, new SubscriptionDays());

            _repository.GetByUser(Arg.Any<Guid>()).Returns(subscriptions);

            UpdateSubscriptionsCommand command = new UpdateSubscriptionsCommand();
            command.Events = _fixture.CreateMany<DayChangedEvent>(4).ToList();
            foreach (var dayChangedEvent in command.Events)
            {
                dayChangedEvent.SubscriptionId = 2137;
                dayChangedEvent.Value = true;
            }


            //Act
            _sut.Handle(command);


            //Assert
            _repository.Received(1).GetByUser(Arg.Any<Guid>());
            _repository.Received(1).Update(Arg.Is<SubscriptionDays>(s =>
               s.Monday == false &&
               s.Tuesday == false &&
               s.Wednesday == false &&
               s.Thursday == false &&
               s.Friday == false &&
               s.Saturday == false &&
               s.Sunday == false
                ));
        }

        [Fact]
        public void Handle_IncomingEvents_TheyAreApplied()
        {
            //Arrange
            Dictionary<long, SubscriptionDays> subscriptions = new Dictionary<long, SubscriptionDays>();
            subscriptions.Add(1, new SubscriptionDays());

            _repository.GetByUser(Arg.Any<Guid>()).Returns(subscriptions);

            UpdateSubscriptionsCommand command = new UpdateSubscriptionsCommand();
            command.Events = new List<DayChangedEvent>
            {
                new DayChangedEvent
                {
                    Day = "monday",
                    SubscriptionId = 1,
                    Value = true
                },
                new DayChangedEvent
                {
                    Day = "friday",
                    SubscriptionId = 1,
                    Value = true
                }
            };


            //Act
            _sut.Handle(command);


            //Assert
            _repository.Received(1).GetByUser(Arg.Any<Guid>());
            _repository.Received(1).Update(Arg.Is<SubscriptionDays>(s => s.Monday == true && s.Friday == true));
        }
    }
}
