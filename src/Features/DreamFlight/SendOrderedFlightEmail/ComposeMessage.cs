using System.Collections.Generic;
using DreamTravel.Domain.FlightEmailOrders;
using DreamTravel.Domain.Flights;
using DreamTravel.Features.DreamFlight.SendOrderedFlightEmail.Interfaces;

namespace DreamTravel.Features.DreamFlight.SendOrderedFlightEmail
{
    public class ComposeMessage : IComposeMessage
    {
        public string Execute(List<Flight> flights, FlightEmailData flightEmailOrder)
        {
            string message = $@"
<html> 
<body> 
<h4> Hello {flightEmailOrder.UserName}! </h4></br>
</br>
<p> Check out the details of your Dream Travel to {flightEmailOrder.To}: </p></br>
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