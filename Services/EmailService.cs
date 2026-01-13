using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using ValternativeServer.Models;

namespace ValternativeServer.Services
{
    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _smtp;

        public EmailService(IOptions<SmtpSettings> smtp)
        {
            _smtp = smtp.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlContent)
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse("noreply@niceniceglobe.com"));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;

            email.Body = new BodyBuilder
            {
                HtmlBody = htmlContent
            }.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_smtp.Host, _smtp.Port, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_smtp.Username, _smtp.Password);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}