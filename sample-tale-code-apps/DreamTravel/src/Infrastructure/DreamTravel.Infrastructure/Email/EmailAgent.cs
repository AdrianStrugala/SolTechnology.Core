namespace DreamTravel.Infrastructure.Email
{
    using System.Net;
    using System.Net.Mail;
    using System.Text;

    public static class EmailAgent
    {
        private static readonly SmtpClient Client;

        static EmailAgent()
        {
            Client = new SmtpClient();

            Client.Port = 587;          
            Client.EnableSsl = true;
            Client.Timeout = 10000;
            Client.DeliveryMethod = SmtpDeliveryMethod.Network;
            Client.UseDefaultCredentials = false;
            Client.Host = "smtp.gmail.com";
            Client.Credentials = new NetworkCredential("dreamtravelwebsite@gmail.com", "");
        }

        public static void Send(IEmail email)
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


            Client.Send(mailMessage);
        }
    }
}
