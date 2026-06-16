using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace Diplom.Services.Email
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly SmtpEmailSettings _settings;

        public SmtpEmailSender(IOptions<SmtpEmailSettings> options)
        {
            _settings = options.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };

            message.To.Add(toEmail);

            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl,
                Credentials = new NetworkCredential(_settings.UserName, _settings.Password)
            };

            await client.SendMailAsync(message);
        }
    }
}
