using System.Net;
using System.Net.Mail;

namespace WatchStore.Services
{
    public interface IEmailSenderSimple
    {
        Task SendAsync(string to, string subject, string htmlBody);
    }

    public class SmtpEmailSender : IEmailSenderSimple
    {
        private readonly IConfiguration _cfg;
        public SmtpEmailSender(IConfiguration cfg) => _cfg = cfg;

        public async Task SendAsync(string to, string subject, string htmlBody)
        {
            var host = _cfg["Smtp:Host"];
            var port = int.Parse(_cfg["Smtp:Port"] ?? "587");
            var user = _cfg["Smtp:User"];
            var pass = _cfg["Smtp:Pass"];
            var from = _cfg["Smtp:From"] ?? user;
            var enableSsl = bool.Parse(_cfg["Smtp:EnableSsl"] ?? "true");

            using var smtp = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                Credentials = new NetworkCredential(user, pass)
            };
            using var msg = new MailMessage(from, to)
            {
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            await smtp.SendMailAsync(msg);
        }
    }
}
