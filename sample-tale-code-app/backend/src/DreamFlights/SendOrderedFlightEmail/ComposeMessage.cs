﻿using System.Collections.Generic;
using DreamTravel.DatabaseData.Query.GetSubscriptionDetailsByDay;
using DreamTravel.Domain.FlightEmailSubscriptions;
using DreamTravel.Domain.Flights;
using DreamTravel.DreamFlights.SendOrderedFlightEmail.Interfaces;

namespace DreamTravel.DreamFlights.SendOrderedFlightEmail
{
    public class ComposeMessage : IComposeMessage
    {
        public string Execute(List<Flight> flights, FlightEmailData flightEmailData)
        {
            string message = $@"
<html> 
<body> 
<h4> Hello {flightEmailData.UserName}! </h4></br>
</br>
<p> Check out the details of your Dream Travel to {flightEmailData.To}: </p></br>
</br>
<ul>
";
            foreach (var flight in flights)
            {
                message += $@"<li> {flight.ThereDepartureCity} — {flight.ThereArrivalCity}  ({flight.ThereDate}:{flight.ThereDepartureHour} - {flight.BackDate}:{flight.BackDepartureHour}) — {flight.Price}€ </li>";
            }

            message += @"</ul>

</br>
</br>
<p> Looks interesting? Check more details at: http://www.azair.eu/ </p></br>
</br>
<p> And in meantime plan your next Dream Travel at: https://dreamtravel.azurewebsites.net/ </p></br>
</br>
<p> Stay in touch! <br>
Dream Travel Team </p>
</body> 
</html> 
";

            return message;
        }
    }
}