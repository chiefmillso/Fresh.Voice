using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Fresh.Voice.DTO;
using Gurock.SmartInspect;
using Mandrill;
using Twilio.TwiML.Mvc;

namespace Fresh.Voice.Controllers
{
    public class VoiceController : AsyncTwilioController
    {
        // GET: Voice
        public ActionResult Receive(string email)
        {
            SiAuto.Main.LogMessage("VoiceController.Receive");
            return View("Receive", "_Raw");
        }

        public async Task<ActionResult> Transcribed(TranscriptionCallback callback)
        {
            SiAuto.Main.LogMessage("VoiceController.Transcribed");
            try
            {
                SiAuto.Main.LogObject("VoiceController.Transcribed - Callback", callback);

                var appSettings = ConfigurationManager.AppSettings;
                string to = appSettings["Voice.Email.To"];
                if (to != Request["Email"])
                    return View("Error");

                var helper = new MailHelper();
                await helper.SendMailAsync(callback);
                await helper.SendNotificationAsync(callback);
                helper.SendSMS(callback);
                
                return View("Transcribed", "_Raw");
            }
            catch (Exception ex)
            {
                SiAuto.Main.LogException("VoiceController.Transcribed - " + ex.Message, ex);
                throw;
            }
        }

        public ActionResult Goodbye()
        {
            SiAuto.Main.LogMessage("VoiceController.Goodbye");
            return View("Goodbye", "_Raw");
        }
    }
}