﻿using System;
using System.Collections.Generic;

namespace DreamTravel.Domain.FlightEmailSubscriptions
{
    public interface IFlightEmailSubscriptionRepository
    {
        int Insert(FlightEmailSubscription flightEmailSubscription);

        List<FlightEmailSubscription> GetByUserId(Guid userId);

        void Delete(int id);
    }
}