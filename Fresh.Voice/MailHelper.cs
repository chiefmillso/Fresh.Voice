using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Fresh.Voice.DTO;
using Gurock.SmartInspect;
using Mandrill;
using Microsoft.ServiceBus.Notifications;

namespace Fresh.Voice
{
    public class MailHelper
    {
        public async Task SendNotificationAsync(TranscriptionCallback callback)
        {
            var connectionString = ConfigurationManager.AppSettings["Push.ConnectionString"];
            var hubName = ConfigurationManager.AppSettings["Push.HubName"];
            NotificationHubClient hub = NotificationHubClient.CreateClientFromConnectionString(connectionString, hubName);
            string toast = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                                 "<wp:Notification xmlns:wp=\"WPNotification\">" +
                                 "<wp:Toast>" +
                                 "<wp:Text1>" + GetShortMessage(callback) + "</wp:Text1>" +
                                 "</wp:Toast> " +
                                 "</wp:Notification>";

            SiAuto.Main.LogString("Toast", toast);

            await hub.SendMpnsNativeNotificationAsync(toast);
        }

        public void SendSMS(TranscriptionCallback callback)
        {
            var appSettings = ConfigurationManager.AppSettings;
            var smsTo = appSettings["Voice.SMS.To"];
            if (string.IsNullOrEmpty(smsTo))
                return;
            var body = GetShortMessage(callback);
            var client = new Twilio.TwilioRestClient(appSettings["Twilio.AccountSid"], appSettings["Twilio.AuthToken"]);
            var message = client.SendSmsMessage(appSettings["Voice.SMS.From"], smsTo, body);
            if (message.RestException != null)
                SiAuto.Main.LogObject(Level.Error, "VoiceController.Transcribed.SendSMS", message.RestException);
        }

        public async Task SendMailAsync(TranscriptionCallback callback)
        {
            var appSettings = ConfigurationManager.AppSettings;
            string to = appSettings["Voice.Email.To"];
            string key = appSettings["Mandrill.ApiKey"];
            string from = ConfigurationManager.AppSettings["Voice.Email.From"];
            var api = new MandrillApi(key);
            var mail = new EmailMessage
            {
                to = new List<EmailAddress> { new EmailAddress { email = to, name = "" } },
                from_email = @from
            };

            if (callback.TranscriptionStatus != TranscriptionStatuses.Completed)
            {
                mail.subject = string.Format("Error transcribing voicemail from {0}", callback.Caller);
                mail.text = GetMessageFailure(callback);
            }
            else
            {
                mail.subject = string.Format("New voicemail from {0}", callback.Caller);
                mail.text = GetMessageSuccess(callback);
            }
            List<EmailResult> result = await api.SendMessageAsync(mail);
            SiAuto.Main.LogCollection("Email Result", result);
        }

        private string GetShortMessage(TranscriptionCallback callback)
        {
            if (callback.TranscriptionStatus != TranscriptionStatuses.Completed)
            {
                return string.Format("Voicemail:{0}\n{1}", callback.Caller, callback.RecordingUrl);
            }
            return string.Format("Voicemail:{0}\n{1}", callback.Caller, callback.TranscriptionText);
        }


        private string GetMessageFailure(TranscriptionCallback callback)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("You have a new voicemail from {0}", callback.Caller);
            sb.AppendLine();
            sb.AppendFormat("Click this link to listen to the message: {0}", callback.RecordingUrl);
            sb.AppendLine();
            return sb.ToString();
        }

        private string GetMessageSuccess(TranscriptionCallback callback)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("You have a new voicemail from {0}", callback.Caller);
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendFormat(":: {0} ::", callback.TranscriptionText);
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendFormat("Click this link to listen to the message: {0}", callback.RecordingUrl);
            sb.AppendLine();
            return sb.ToString();
        }

    }
}