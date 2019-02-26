namespace DreamTravel.Bot.DiscoverIndividualChances.SendEmail
{
    using Interfaces;
    using Models;

    public class ComposeMessage : IComposeMessage
    {
        public string Execute(Chance chance, Subscription subscription)
        {
            string message = $@"
<html> 
<body> 
<h4> Hello {subscription.UserName}! </h4></br>
</br>
<p> Check out the details of your Dream Travel to {subscription.To}: </p></br>
</br>
<p>Price: {chance.Price} € </p>
<p>From: {chance.Origin}, By: {chance.ThereCarrier}, At: {chance.ThereDay}</p>
<p>To: {chance.Destination}, By: {chance.BackCarrier}, At: {chance.BackDay}</p>
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