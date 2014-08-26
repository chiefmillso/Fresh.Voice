using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Notification;
using Microsoft.Phone.Shell;
using Fresh.Voice.WinPhone.Resources;
using Microsoft.WindowsAzure.Messaging;
using Xamarin.Forms;

namespace Fresh.Voice.WinPhone
{
    public partial class MainPage : PhoneApplicationPage
    {
        private HttpNotificationChannel httpChannel;
        private const string ChannelName = Voice.App.PushChannel;
        const string FileName = "PushNotificationsSettings.dat";

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            Forms.Init();

            RegisterForNotifications();

            Content = Fresh.Voice.App.GetMainPage().ConvertPageToUIElement(this);
        }

        private async void RegisterForNotifications()
        {
            if (!await TryFindChannelAsync())
                await DoConnect();
        }

        #region Misc logic
        private async Task DoConnect()
        {
            try
            {
                //First, try to pick up existing channel
                httpChannel = HttpNotificationChannel.Find(ChannelName);

                if (null != httpChannel)
                {
                    Trace("Channel Exists - no need to create a new one");
                    SubscribeToChannelEvents();

                    Trace("Register the URI with 3rd party web service");
                    await SubscribeToServiceAsync();

                    Trace("Subscribe to the channel to Tile and Toast notifications");
                    SubscribeToNotifications();

                    Dispatcher.BeginInvoke(() => Trace("Channel recovered"));
                }
                else
                {
                    Trace("Trying to create a new channel...");
                    //Create the channel
                    httpChannel = new HttpNotificationChannel(ChannelName);
                    Trace("New Push Notification channel created successfully");

                    SubscribeToChannelEvents();

                    Trace("Trying to open the channel");
                    httpChannel.Open();
                    Dispatcher.BeginInvoke(() => Trace("Channel open requested"));
                }
            }
            catch (Exception ex)
            {
                Dispatcher.BeginInvoke(() => Trace("Channel error: " + ex.Message));
            }
        }

        #endregion

        #region Subscriptions
        private void SubscribeToChannelEvents()
        {
            //Register to UriUpdated event - occurs when channel successfully opens
            httpChannel.ChannelUriUpdated += new EventHandler<NotificationChannelUriEventArgs>(httpChannel_ChannelUriUpdated);

            //Subscribed to Raw Notification
            httpChannel.HttpNotificationReceived += new EventHandler<HttpNotificationEventArgs>(httpChannel_HttpNotificationReceived);

            //general error handling for push channel
            httpChannel.ErrorOccurred += new EventHandler<NotificationChannelErrorEventArgs>(httpChannel_ExceptionOccurred);

            //subscrive to toast notification when running app    
            httpChannel.ShellToastNotificationReceived += new EventHandler<NotificationEventArgs>(httpChannel_ShellToastNotificationReceived);
        }

        private async Task SubscribeToServiceAsync()
        {
            try
            {
                var hub = new NotificationHub(
                    Voice.App.HubName,
                    Voice.App.ConnectionString);

                Registration registration = await hub.RegisterNativeAsync(httpChannel.ChannelUri.ToString());
                Trace("Registered with Azure Notification Hub, Registration Id:" + registration.RegistrationId);

                Dispatcher.BeginInvoke(() => Trace("Registered with Windows Azure Notification Hub"));
            }
            catch (RegistrationAuthorizationException rEx)
            {
                Trace(rEx.Message);
                throw;
            }
        }

        private void SubscribeToNotifications()
        {
            //////////////////////////////////////////
            // Bind to Toast Notification 
            //////////////////////////////////////////
            try
            {
                if (httpChannel.IsShellToastBound == true)
                {
                    Trace("Already bounded (register) to to Toast notification");
                }
                else
                {
                    Trace("Registering to Toast Notifications");
                    httpChannel.BindToShellToast();
                }
            }
            catch (Exception ex)
            {
                // handle error here
            }

            //////////////////////////////////////////
            // Bind to Tile Notification 
            //////////////////////////////////////////
            try
            {
                if (httpChannel.IsShellTileBound == true)
                {
                    Trace("Already bounded (register) to Tile Notifications");
                }
                else
                {
                    Trace("Registering to Tile Notifications");

                    httpChannel.BindToShellTile();
                }
            }
            catch (Exception ex)
            {
                //handle error here
            }
        }

        #endregion

        #region Channel event handlers
        async void httpChannel_ChannelUriUpdated(object sender, NotificationChannelUriEventArgs e)
        {
            Trace("Channel opened. Got Uri:\n" + httpChannel.ChannelUri.ToString());
            Dispatcher.BeginInvoke(SaveChannelInfo);

            Trace("Subscribing to channel events");
            await SubscribeToServiceAsync();
            SubscribeToNotifications();

            Dispatcher.BeginInvoke(() => Trace("Channel created successfully"));
        }

        void httpChannel_ExceptionOccurred(object sender, NotificationChannelErrorEventArgs e)
        {
            Dispatcher.BeginInvoke(() => Trace(e.ErrorType + " occurred: " + e.Message));
        }

        void httpChannel_HttpNotificationReceived(object sender, HttpNotificationEventArgs e)
        {
            Trace("===============================================");
            Trace("RAW notification arrived:");

            //string weather, location, temperature;
            //ParseRAWPayload(e.Notification.Body, out weather, out location, out temperature);

            //Dispatcher.BeginInvoke(() => this.textBlockListTitle.Text = location);
            //Dispatcher.BeginInvoke(() => this.txtTemperature.Text = temperature);
            //Dispatcher.BeginInvoke(() => this.imgWeatherConditions.Source = new BitmapImage(new Uri(@"Images/" + weather + ".png", UriKind.Relative)));
            //Trace(string.Format("Got weather: {0} with {1}F at location {2}", weather, temperature, location));

            Trace("===============================================");
        }

        void httpChannel_ShellToastNotificationReceived(object sender, NotificationEventArgs e)
        {
            Trace("===============================================");
            Trace("Toast/Tile notification arrived:");
            foreach (var key in e.Collection.Keys)
            {
                string msg = e.Collection[key];

                Trace(msg);
                Dispatcher.BeginInvoke(() => Trace("Toast/Tile message: " + msg));
            }

            Trace("===============================================");
        }
        #endregion

        #region Loading/Saving Channel Info
        private async Task<bool> TryFindChannelAsync()
        {
            bool bRes = false;

            Trace("Getting IsolatedStorage for current Application");
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
                Trace("Checking channel data");
                if (isf.FileExists(FileName))
                {
                    Trace("Channel data exists! Loading...");
                    using (var isfs = new IsolatedStorageFileStream(FileName, FileMode.Open, isf))
                    {
                        using (var sr = new StreamReader(isfs))
                        {
                            string uri = sr.ReadLine();
                            Trace("Finding channel");
                            httpChannel = HttpNotificationChannel.Find(ChannelName);

                            if (null != httpChannel)
                            {
                                if (httpChannel.ChannelUri.ToString() == uri)
                                {
                                    Dispatcher.BeginInvoke(() => Trace("Channel retrieved"));
                                    SubscribeToChannelEvents();
                                    await SubscribeToServiceAsync();
                                    bRes = true;
                                }

                                sr.Close();
                            }
                        }
                    }
                }
                else
                    Trace("Channel data not found");
            }

            return bRes;
        }

        private void SaveChannelInfo()
        {
            Trace("Getting IsolatedStorage for current Application");
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
                Trace("Creating data file");
                using (IsolatedStorageFileStream isfs = new IsolatedStorageFileStream(FileName, FileMode.Create, isf))
                {
                    using (StreamWriter sw = new StreamWriter(isfs))
                    {
                        Trace("Saving channel data...");
                        sw.WriteLine(httpChannel.ChannelUri.ToString());
                        sw.Close();
                        Trace("Saving done");
                    }
                }
            }
        }
        #endregion
        
        private void Trace(string message)
        {
#if DEBUG
            Debug.WriteLine(message);
#endif
        }
        
        // Sample code for building a localized ApplicationBar
        //private void BuildLocalizedApplicationBar()
        //{
        //    // Set the page's ApplicationBar to a new instance of ApplicationBar.
        //    ApplicationBar = new ApplicationBar();

        //    // Create a new button and set the text value to the localized string from AppResources.
        //    ApplicationBarIconButton appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.add.rest.png", UriKind.Relative));
        //    appBarButton.Text = AppResources.AppBarButtonText;
        //    ApplicationBar.Buttons.Add(appBarButton);

        //    // Create a new menu item with the localized string from AppResources.
        //    ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.AppBarMenuItemText);
        //    ApplicationBar.MenuItems.Add(appBarMenuItem);
        //}
    }
}