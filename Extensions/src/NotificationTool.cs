using System;
using System.Net.Mail;
using System.Net;
using System.Collections.Generic;

namespace Extensions
{
    public static class NotificationTool
    {
        public const string RegataMailTarget = "MeasurementsMailHost";
        public static List<string> Adresses = new List<string>();

        // TODO: make async

        public static void SendMessage(string ErrorMessage)
        {
            try
            {
                if (Environment.MachineName != "NF-105-17") return;

                if (Adresses == null || Adresses.Count == 0) return;

                var cm = AdysTech.CredentialManager.CredentialManager.GetCredentials(RegataMailTarget);

                if (cm == null)
                    throw new ArgumentException("Can't load regata mail credential. Please add it to the windows credential manager");

                if (string.IsNullOrEmpty(cm.Password)) return;

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var fromAddress = new MailAddress(cm.UserName, "Report service");
                string fromPassword = cm.Password;
                string subject = "[Measurements ERROR]Сбой в программе 'Измерения'";
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
                    foreach (var adress in Adresses)
                    {
                        var toAddress = new MailAddress(adress, "");
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
            }
            catch (Exception ex)
            { // write to log? }
            }
        }
    }
}
