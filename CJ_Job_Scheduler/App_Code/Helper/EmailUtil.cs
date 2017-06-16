using System;
using System.Collections.Generic;
using System.Text;


using System.Net.Mail;
using DWMS_OCR.App_Code.Helper;

namespace CJ_Job_Scheduler.App_Code.Helper
{
    class EmailUtil
    {
        protected MailAddress From { get; private set; }
        protected MailPriority Priority { get; private set; }


        /// <summary>
        /// Initializes a new instance with the sender information.
        /// </summary>
        /// <param name="senderAddress">For example: info@hiend.com</param>
        /// <param name="senderDisplayName">For example: Hiend Software</param>
        public EmailUtil(string senderAddress, string senderDisplayName)
        {
            this.From = new MailAddress(senderAddress, senderDisplayName, Encoding.UTF8);
        }

        public EmailUtil(string senderAddress, string senderDisplayName, MailPriority priority) :
            this(senderAddress, senderDisplayName)
        {
            this.Priority = priority;
        }

        public void SendEmail(string toAddresses, string ccAddresses, string subject, string body)
        {
            MailMessage message = new MailMessage();
            char[] delimiterChars = { ';', ',', ':' };


            // From
            message.From = this.From;

            // To
            if (!String.IsNullOrEmpty(toAddresses))
            {
                //message.To.Add(toAddresses);
                string[] toEmails = toAddresses.Split(delimiterChars);
                foreach (string s in toEmails)
                    if (s != null && s.Trim() != "" && Validation.IsEmail(s.Trim()))
                        message.To.Add(new MailAddress(s.Trim()));
            }

            // Cc
            if (!String.IsNullOrEmpty(ccAddresses))
            {
                //message.CC.Add(ccAddresses);
                string[] toEmails = ccAddresses.Split(delimiterChars);
                foreach (string s in toEmails)
                    if (s != null && s.Trim() != "" && Validation.IsEmail(s.Trim()))
                        message.CC.Add(new MailAddress(s.Trim()));

            }

            message.Body = body;
            message.BodyEncoding = Encoding.UTF8;
            message.IsBodyHtml = true;
            message.Subject = subject;
            message.SubjectEncoding = Encoding.UTF8;
            message.Priority = this.Priority;

            // Host and port will be picked up from configuration file settings with
            // the default constructor.
            SmtpClient client = new SmtpClient();

            client.Send(message);
        }

       

    }
}
