namespace DreamTravel.Infrastructure.Email
{
    using System.Net.Mail;
    using System.Text;

    public static class EmailAgent
    {
        private static readonly SmtpClient Client;

        static EmailAgent()
        {
            Client = new SmtpClient
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
