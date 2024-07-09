using BaiSol.Server.Models.Email;
using BaseLibrary.Services.Interfaces;
using MailKit.Net.Smtp;
using MimeKit;

namespace BaseLibrary.Services.Repositories
{
    public class EmailRepository(EmailModel _emailModel) : IEmailRepository
    {
        public void SendEmail(EmailMessage emailMessage)
        {

            var sendMessage = CreateEmailMessage(emailMessage);
            Send(sendMessage);
        }

        private MimeMessage CreateEmailMessage(EmailMessage message)
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress("email", _emailModel.From));
            emailMessage.To.AddRange(message.To);
            emailMessage.Subject = message.Subject;
            emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = message.Content };

            return emailMessage;
        }

        private void Send(MimeMessage mimeMessage)
        {
            using var client = new SmtpClient();
            try
            {
                client.Connect(_emailModel.SmtpServer, _emailModel.Port, true);
                client.AuthenticationMechanisms.Remove("XOAUTH2");
                client.Authenticate(_emailModel.UserName, _emailModel.Password);

                client.Send(mimeMessage);

            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Failed to send email: {ex.Message}");
            }
            finally
            {
                client.Disconnect(true);
                client.Dispose();
            }
        }
    }
}
