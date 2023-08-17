
using AuthSystem.IServices;
using MailKit.Net.Smtp;
using MimeKit;

namespace AuthenticationSystem.Services
{
    public class EmailService : IEmailService
    {
        public void SendEmail(string name, string emailto, string subject, string plaintext, string htmlcontent)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("localhost","manmeetsingh.hsn@outlook.com"));
            message.To.Add(MailboxAddress.Parse(emailto));
            message.Subject = subject;

            message.Body = new TextPart(MimeKit.Text.TextFormat.Html){ Text = htmlcontent };

            using (var client = new SmtpClient())
            {
                client.Connect("smtp-mail.outlook.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                client.Authenticate("manmeetsingh.hsn@outlook.com", "");//replave the string with your password
                client.Send(message);
                client.Disconnect(true);
            }

        }
    }
}
