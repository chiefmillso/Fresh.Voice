using System;
using Xamarin.Forms;

namespace Fresh.Voice
{
	public class App
	{
	    public const string PushChannel = "Fresh.Voice.MailNotification";
	    public const string HubName = "fresh.hub";
        public const string ConnectionString = "Endpoint=sb://fresh-hub-ns.servicebus.windows.net/;SharedAccessKeyName=DefaultListenSharedAccessSignature;SharedAccessKey=spC47U/SrFPC2ZLr+nE7TzI2X1s2ijwGiqTXRa8Mcu4=";

		public static Page GetMainPage ()
		{	
			return new ContentPage { 
				Content = new Label {
					Text = "Hello, Forms!",
					VerticalOptions = LayoutOptions.CenterAndExpand,
					HorizontalOptions = LayoutOptions.CenterAndExpand,
				},
			};
		}
	}
}

