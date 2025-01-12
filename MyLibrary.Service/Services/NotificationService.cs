﻿using System.Net;
using System.Net.Mail;

namespace MyLibrary.Service.Services
{
    public class NotificationService
    {
        private readonly string _email;
        private readonly string _password;

        public NotificationService(string email, string password)
        {
            _email = email;
            _password = password;
        }

        public void SendEmail(string to, string subject, string body)
        {
            try
            {
                using (var client = new SmtpClient("smtp.gmail.com", 587))
                {
                    client.Credentials = new NetworkCredential(_email, _password);
                    client.EnableSsl = true;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_email),
                        Subject = subject,
                        Body = body
                    };
                    mailMessage.To.Add(to);

                    client.Send(mailMessage);
                }
                Console.WriteLine($"Email sent to: {to}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send email: {ex.Message}");
            }
        }
    }
}