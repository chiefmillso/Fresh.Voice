using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Web.Mvc;
using Fresh.Voice.DTO;
using Gurock.SmartInspect;
using Mandrill;
using Twilio.TwiML.Mvc;

namespace Fresh.Voice.Controllers
{
    public class VoiceController : TwilioController
    {
        // GET: Voice
        public ActionResult Receive(string email)
        {
            SiAuto.Main.LogMessage("VoiceController.Receive");
            return View("Receive", "_Raw");
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

        public ActionResult Transcribed(TranscriptionCallback callback)
        {
            SiAuto.Main.LogMessage("VoiceController.Transcribed");
            try
            {
                SiAuto.Main.LogObject("VoiceController.Transcribed - Callback", callback);

                var appSettings = ConfigurationManager.AppSettings;
                string key = appSettings["Mandrill.ApiKey"];
                string to = appSettings["Voice.Email.To"];
                if (to != Request["Email"])
                    return View("Error");

                string from = ConfigurationManager.AppSettings["Voice.Email.From"];
                var api = new MandrillApi(key);
                var mail = new EmailMessage
                {
                    to = new List<EmailAddress> {new EmailAddress {email = to, name = ""}},
                    from_email = from
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

                List<EmailResult> result = api.SendMessage(mail);
                var smsTo = appSettings["Voice.SMS.To"];
                if (!string.IsNullOrEmpty(smsTo))
                    SendMessage(callback, smsTo);
                return View("Transcribed", "_Raw");
            }
            catch(Exception ex)
            {
                SiAuto.Main.LogException("VoiceController.Transcribed - " + ex.Message, ex);
                throw;
            }
        }

        private void SendMessage(TranscriptionCallback callback, string smsTo)
        {
            var appSettings = ConfigurationManager.AppSettings;
            var body = GetShortMessage(callback);
            var client = new Twilio.TwilioRestClient(appSettings["Twilio.AccountSid"], appSettings["Twilio.AuthToken"]);
            var message = client.SendSmsMessage(appSettings["Voice.SMS.From"], smsTo, body);
            if (message.RestException != null)
                SiAuto.Main.LogObject(Level.Error, "VoiceController.Transcribed.SendMessage", message.RestException);

        }

        private string GetShortMessage(TranscriptionCallback callback)
        {
            if (callback.TranscriptionStatus != TranscriptionStatuses.Completed)
            {
                return string.Format("Voicemail:{0}\n{1}", callback.Caller, callback.RecordingUrl);
            }
            return string.Format("Voicemail:{0}\n{1}", callback.Caller, callback.TranscriptionText);
        }

        public ActionResult Goodbye()
        {
            SiAuto.Main.LogMessage("VoiceController.Goodbye");
            return View("Goodbye", "_Raw");
        }
    }
}