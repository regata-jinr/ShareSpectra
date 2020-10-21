using System;
using System.Net.Mail;
using System.Net;
using System.Collections.Generic;
using System.IO;

namespace Extensions
{
    public static class NotificationTool
    {
        public const string RegataMailTarget = "RegataMail";
        public static List<string> Adresses;
        public static bool IsAlreadySend = false;

        static NotificationTool()
        {
            Adresses = new List<string>();
            if (File.Exists("emails.txt"))
                Adresses.AddRange(File.ReadAllLines("emails.txt"));
        }

        public static void SendMessage(string ErrorMessage)
        {
            try
            {
                if (IsAlreadySend) return;

                if (Adresses == null || Adresses.Count == 0) return;

                var cm = AdysTech.CredentialManager.CredentialManager.GetCredentials(RegataMailTarget);

                if (cm == null)
                    throw new ArgumentException("Can't load regata mail credential. Please add it to the windows credential manager");

                if (string.IsNullOrEmpty(cm.Password)) return;

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var fromAddress = new MailAddress(cm.UserName, "Regata report service");
                string fromPassword = cm.Password;
                string subject = "[Measurements ERROR]Сбой в программе 'Измерения'";
                string body = $"Во время работы программы измерений произошла ошибка. Текст ошибки:{Environment.NewLine}{ErrorMessage}";

                using (var smtp = new SmtpClient
                {
                    Host = "mail.jinr.ru",
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
                IsAlreadySend = true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
