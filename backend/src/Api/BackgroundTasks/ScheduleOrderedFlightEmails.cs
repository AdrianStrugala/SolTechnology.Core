using System;
using System.Collections.Generic;
using DreamTravel.DatabaseData.Query.GetSubscriptionDetailsByDay;
using DreamTravel.DreamFlights.GetTodaysFlightEmailData;
using DreamTravel.DreamFlights.SendOrderedFlightEmail.Interfaces;
using Hangfire;

namespace DreamTravel.Api.BackgroundTasks
{
    public interface IScheduleOrderedFlightEmails
    {
        void Schedule();
    }

    public class ScheduleOrderedFlightEmails : IScheduleOrderedFlightEmails
    {
        private readonly IBackgroundJobClient _backgroundJobs;
        private readonly IGetTodaysFlightEmailData _getTodaysFlightEmailData;
        private readonly ISendOrderedFlightEmail _sendOrderedFlightEmail;
        private readonly Random _random = new Random();

        public ScheduleOrderedFlightEmails(
            IBackgroundJobClient backgroundJobs,
            IGetTodaysFlightEmailData getTodaysFlightEmailData,
            ISendOrderedFlightEmail sendOrderedFlightEmail)
        {
            _backgroundJobs = backgroundJobs;
            _getTodaysFlightEmailData = getTodaysFlightEmailData;
            _sendOrderedFlightEmail = sendOrderedFlightEmail;
        }

        public void Schedule()
        {
            List<FlightEmailData> flightEmailData = _getTodaysFlightEmailData.Handle();

            //Break between sending orders in equal time (in total 12h)
            int twelveHoursInSec = 43200;
            int pollingInterval = twelveHoursInSec / flightEmailData.Count;

            for (int i = 0; i < flightEmailData.Count; i++)
            {
                int delay = (pollingInterval * i) + _random.Next(60);

                _backgroundJobs.Schedule(() => _sendOrderedFlightEmail.Handle(flightEmailData[i]),
                    DateTime.UtcNow.AddSeconds(delay));
            }
        }
    }
}
