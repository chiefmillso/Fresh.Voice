using System.Web.Mvc;
using Twilio.TwiML;
using Twilio.TwiML.Mvc;

namespace Fresh.Voice.Controllers
{
    // ReSharper disable once InconsistentNaming
    public abstract class AsyncTwilioController : AsyncController
    {
        public TwiMLResult TwiML(TwilioResponse response)
        {
            return new TwiMLResult(response);
        }
    }
}