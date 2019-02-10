namespace DreamTravel.Bot.SendEmail
{
    using Scrap_AzairEu;
    using System.Collections.Generic;
    using Interfaces;

    public class ComposeMessage : IComposeMessage
    {
        public string Execute(List<Chance> chances)
        {
            string message = @"
Welcome Traveler!

Feel welcome to check out the newest flight chances. Prices below contains flying tickets there and back:
";

            foreach (var chance in chances)
            {
                message += $"• {chance.ThereDepartureCity} — {chance.ThereArrivalCity}  ({chance.ThereDate}-{chance.BackDate}) — {chance.Price}€ \n";
            }

            message += @"

Are you interested in? Check more details at: http://www.azair.eu/

Nothing interesting today? Plan your own Dream Travel at: https://dreamtravel.azurewebsites.net/

Stay in touch!
Dream Travel Team
";

            return message;
        }

        public string ExecuteHtml(List<Chance> chances)
        {
            string message = @"<html> <body> 
<h4> Welcome Traveler! </h4></br>
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
<p> Nothing interesting today? Plan your own Dream Travel at: https://dreamtravel.azurewebsites.net/ </p></br>
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