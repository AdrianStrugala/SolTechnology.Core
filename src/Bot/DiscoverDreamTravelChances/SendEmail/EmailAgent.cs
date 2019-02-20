namespace DreamTravel.Bot.DiscoverDreamTravelChances.SendEmail
{
    using Inftastructure.Email;
    using Interfaces;
    using System.Net.Mail;
    using System.Text;

    public class EmailAgent : IEmailAgent
    {
        private readonly SmtpClient _client;

        public EmailAgent()
        {
            _client = new SmtpClient
            {
                Port = 587,
                Host = "smtp.gmail.com",
                EnableSsl = true,
                Timeout = 10000,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new System.Net.NetworkCredential("dreamtravelwebsite@gmail.com", "P4ssw0rd.")
            };
        }

        public void Send(IEmail email)
        {
            MailMessage mailMessage = new MailMessage(
                email.Sender,
                email.Recipient,
                email.Subject,
                email.Message)
            {
                IsBodyHtml = true,
                BodyEncoding = Encoding.UTF8,
                DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure
            };


            _client.Send(mailMessage);
        }
    }
}
