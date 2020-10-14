using System;
using Microsoft.Extensions.Configuration;
using System.Net.Mail;
using System.Net;

namespace Extensions
{
    public class NotificationTool
    {
        private IConfigurationRoot Configuration { get; set; }

        public string ErrorStr { get; private set; }

        public NotificationTool(string ErrorMessage)
        {
            Init(ErrorMessage);
        }
        private void Init(string ErrorMessage)
        {
            try
            {
                if (Environment.MachineName != "NF-105-17") return;

                var builder = new ConfigurationBuilder();
                builder.AddUserSecrets<NotificationTool>();

                Configuration = builder.Build();

                var CurrentAppSets = new AppSets();
                Configuration.GetSection(nameof(AppSets)).Bind(CurrentAppSets);

                if (string.IsNullOrEmpty(CurrentAppSets.Email)) return;

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var fromAddress = new MailAddress(CurrentAppSets.Email, "Report service");
                var toAddress = new MailAddress(CurrentAppSets.Email, "");
                string fromPassword = CurrentAppSets.EmailPassword;
                string subject = "[Measurements ERROR]Сбой в программе Измерения";
                string body = $"Во время работы программы измерений произошла ошибка. Текст ошибки:{Environment.NewLine}{ErrorMessage}";

                using (var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                })
                {
                    using (var message = new MailMessage(fromAddress, toAddress)
                    {
                        Subject = subject,
                        Body = body
                    })
                    {
                        smtp.Send(message);
                    }
                }
            }
            catch (Exception ex)
            { ErrorStr = ex.ToString(); }
        }
    }
}
