using System.Collections.Generic;
using DreamTravel.Domain.Flights;
using DreamTravel.DreamFlights.SendDreamTravelFlightEmail.Interfaces;

namespace DreamTravel.DreamFlights.SendDreamTravelFlightEmail.Executors
{
    public class ComposeMessage : IComposeMessage
    {
        public string ExecuteHtml(List<Flight> chances, string userName)
        {
            string message = @"<html> <body> 
<h4> Welcome " + userName + @"! </h4></br>
</br>
<p> Feel welcome to check out the newest flight chances. Prices below contains flying tickets there and back: </p></br>
<ul>
";

            foreach (var chance in chances)
            {
                message += $@"<li> {chance.ThereDepartureCity} — {chance.ThereArrivalCity}  ({chance.ThereDate}-{chance.BackDate}) — {chance.Price}€ </li>";
            }

            message += @"</ul>
</br>
</br>
<p> Are you interested in? Check more details at: http://www.azair.eu/ </p></br>
</br>
<p> Nothing interesting today? Plan your own Dream Travel at: https://dreamtravels.azurewebsites.net/ </p></br>
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