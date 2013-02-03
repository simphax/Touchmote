using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;

using Hardcodet.Wpf.TaskbarNotification;
using OSC.NET;
using WiiTUIO.WinTouch;
using WiiTUIO.Provider;
using WiiTUIO.Input;
using WiiTUIO.Properties;
using System.Windows.Input;
using WiiTUIO.Output;
using Microsoft.Win32;
using System.Diagnostics;

namespace WiiTUIO
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class ClientForm : UserControl, WiiCPP.WiiPairListener
    {

        String appKey = "Touchmote";

        RegistryKey winStartupRegisterKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

        private bool providerHandlerConnected = false;

        private bool tryingToConnect = false;

        /// <summary>
        /// A reference to the WiiProvider we want to use to get/forward input.
        /// </summary>
        private IProvider pWiiProvider = null;

        WiiCPP.WiiPair wiiPair = null;

        /// <summary>
        /// A reference to the windows 7 HID driver data provider.  This takes data from the <see cref="pWiiProvider"/> and transforms it.
        /// </summary>
        private IProviderHandler pProviderHandler = null;


        /// <summary>
        /// Boolean to tell if we are connected to the mote and network.
        /// </summary>
        private bool bConnected = false;

        /// <summary>
        /// Construct a new Window.
        /// </summary>
        public ClientForm()
        {
            
            // Load from the XAML.
            InitializeComponent();

            InputFactory.InputType inputType = InputFactory.getType(Settings.Default.input);
            OutputFactory.OutputType outputType = OutputFactory.getType(Settings.Default.output);

            switch (inputType)
            {
                case InputFactory.InputType.POINTER:
                    this.cbiPointer.IsSelected = true;
                    break;
                case InputFactory.InputType.PEN:
                    this.cbiPen.IsSelected = true;
                    break;
            }
            /*
            switch (outputType)
            {
                case OutputFactory.OutputType.TOUCH:
                    this.cbiTouch.IsSelected = true;
                    break;
                case OutputFactory.OutputType.TUIO:
                    this.cbiTUIO.IsSelected = true;
                    break;
            }
             * */

            this.cbConnectOnStart.IsChecked = Settings.Default.connectOnStart;
            this.cbWindowsStart.IsChecked = this.winStartupRegisterKey.GetValue("Touchmote") != null;
            
            Application.Current.Exit += appWillExit;

            wiiPair = new WiiCPP.WiiPair();
            wiiPair.addListener(this);

            Settings.Default.PropertyChanged += Settings_PropertyChanged;

            if (!Settings.Default.pairedOnce)
            {
                this.tbConnect.Visibility = Visibility.Hidden;
                this.tbPair.Visibility = Visibility.Visible;
            }
            
            if (!TUIOVmultiProviderHandler.HasDriver())
            {
                this.driverNotInstalled();
            }

        }

        void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (Settings.Default.pairedOnce)
            {
                Dispatcher.BeginInvoke(new Action(delegate()
                {
                    this.tbPair.Visibility = Visibility.Hidden;
                    if(!tryingToConnect && !bConnected)
                    {
                        this.tbConnect.Visibility = Visibility.Visible;
                    }
                }), null);
            }
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.FrameworkElement.Initialized"/> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
        protected override void OnInitialized(EventArgs e)
        {
            // Create the providers.
            this.createProvider();
            this.createProviderHandler();

            if (Settings.Default.connectOnStart)
            {
                this.connectProvider();
            }

            // Call the base class.
            base.OnInitialized(e);
        }

        private void appWillExit(object sender, ExitEventArgs e)
        {
            this.disconnectProvider();
            this.disconnectProviderHandler();
        }


        /// <summary>
        /// This is called when the wii remote is connected
        /// </summary>
        /// <param name="obj"></param>
        private void pWiiProvider_OnConnect(int obj)
        {
            // Dispatch it.
            Dispatcher.BeginInvoke(new Action(delegate()
            {
                this.bConnected = true;

                // Update the button to say we are connected.
                tbConnected.Visibility = Visibility.Collapsed;
                tbWaiting.Visibility = Visibility.Collapsed;
                tbConnected.Visibility = Visibility.Visible;

                connectProviderHandler();

            }), null);


        }

        /// <summary>
        /// This is called when the wii remote is disconnected
        /// </summary>
        /// <param name="obj"></param>
        private void pWiiProvider_OnDisconnect(int obj)
        {
            // Dispatch it.
            Dispatcher.BeginInvoke(new Action(delegate()
            {
                this.bConnected = false;

                tbConnected.Visibility = Visibility.Collapsed;
                if (tryingToConnect)
                {
                    tbWaiting.Visibility = Visibility.Visible;
                    tbConnect.Visibility = Visibility.Collapsed;
                }
                else
                {
                    tbWaiting.Visibility = Visibility.Collapsed;
                    tbConnect.Visibility = Visibility.Visible;
                }

                batteryLabel.Content = "0%";

                disconnectProviderHandler();

            }), null);
        }


        private Mutex pCommunicationMutex = new Mutex();

        /// <summary>
        /// This is called when the WiiProvider has a new set of input to send.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pWiiProvider_OnNewFrame(object sender, FrameEventArgs e)
        {
            
            // If dispatching events is enabled.
            if (bConnected)
            {
                // Call these in another thread.
                Dispatcher.BeginInvoke(new Action(delegate()
                {
                    if (this.pProviderHandler != null && providerHandlerConnected)
                    {
                        this.pProviderHandler.processEventFrame(e);
                    }
                }), null);
            }
        }

        /// <summary>
        /// This is called when the battery state changes.
        /// </summary>
        /// <param name="obj"></param>
        private void pWiiProvider_OnBatteryUpdate(int obj)
        {
            // Dispatch it.
            Dispatcher.BeginInvoke(new Action(delegate()
            {
                this.batteryLabel.Content = obj.ToString() + "%";
            }), null);
        }

        #region Messages - Err/Inf

        enum MessageType { Info, Error };

        private void showMessage(string sMessage, MessageType eType)
        {
            Console.WriteLine(sMessage);
            Dispatcher.BeginInvoke(new Action(delegate()
            {
            TextBlock pMessage = new TextBlock();
            pMessage.Text = sMessage;
            pMessage.TextWrapping = TextWrapping.Wrap;
            pMessage.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            pMessage.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            pMessage.FontWeight = FontWeights.Bold;
            if (eType == MessageType.Error)
            {
                pMessage.Foreground = new SolidColorBrush(Colors.White);
                pMessage.FontSize = 16.0;
            }

            this.showMessage(pMessage, eType);

            }), null);
        }

        private void showMessage(UIElement pMessage, MessageType eType)
        {
            showMessage(pMessage, 750.0, eType);
        }

        private void showMessage(UIElement pElement, double fTimeout, MessageType eType)
        {
            Dispatcher.BeginInvoke(new Action(delegate()
            {
            // Show (and possibly initialise) the error message overlay
            //brdOverlay.Height = this.ActualHeight - 8;
            //brdOverlay.Width = this.ActualWidth - 8;
            brdOverlay.Opacity = 0.0;
            brdOverlay.Visibility = System.Windows.Visibility.Visible;
            switch (eType)
            {
                case MessageType.Error:
                    brdOverlay.Background = new SolidColorBrush(Color.FromArgb(192, 255, 0, 0));
                    break;
                case MessageType.Info:
                    brdOverlay.Background = new SolidColorBrush(Colors.White);
                    break;
            }

            // Set the message
            brdOverlay.Child = pElement;

            // Fade in and out.
            messageFadeIn(fTimeout, false);
            
            }), null);
        }

        private void messageFadeIn(double fTimeout, bool bFadeOut)
        {
            // Now fade it in with an animation.
            DoubleAnimation pAnimation = createDoubleAnimation(1.0, fTimeout, false);
            pAnimation.Completed += delegate(object sender, EventArgs pEvent)
            {
                if (bFadeOut)
                    this.messageFadeOut(fTimeout);
            };
            pAnimation.Freeze();
            brdOverlay.BeginAnimation(Canvas.OpacityProperty, pAnimation, HandoffBehavior.Compose);

        }
        private void messageFadeOut(double fTimeout)
        {
            // Now fade it in with an animation.
            DoubleAnimation pAnimation = createDoubleAnimation(0.0, fTimeout, false);
            pAnimation.Completed += delegate(object sender, EventArgs pEvent)
            {
                // We are now faded out so make us invisible again.
                brdOverlay.Visibility = Visibility.Hidden;
            };
            pAnimation.Freeze();
            brdOverlay.BeginAnimation(Canvas.OpacityProperty, pAnimation, HandoffBehavior.Compose);
        }

        #region Animation Helpers
        /**
         * @brief Helper method to create a double animation.
         * @param fNew The new value we want to move too.
         * @param fTime The time we want to allow in ms.
         * @param bFreeze Do we want to freeze this animation (so we can't modify it).
         */
        private static DoubleAnimation createDoubleAnimation(double fNew, double fTime, bool bFreeze)
        {
            // Create the animation.
            DoubleAnimation pAction = new DoubleAnimation(fNew, new Duration(TimeSpan.FromMilliseconds(fTime)))
            {
                // Specify settings.
                AccelerationRatio = 0.1,
                DecelerationRatio = 0.9,
                FillBehavior = FillBehavior.HoldEnd
            };

            // Pause the action before starting it and then return it.
            if (bFreeze)
                pAction.Freeze();
            return pAction;
        }
        #endregion
        #endregion


        private void showConfig()
        {
            this.disableMainControls();
            this.configOverlay.Visibility = Visibility.Visible;

        }

        private void hideConfig()
        {
            this.enableMainControls();
            this.configOverlay.Visibility = Visibility.Hidden;

        }

        #region Create and Die

        /// <summary>
        /// Create the link to the Windows 7 HID driver.
        /// </summary>
        /// <returns></returns>
        private bool createProviderHandler()
        {
            try
            {
                // Close any open connections.
                disconnectProviderHandler();

                // Reconnect with the new API.
                this.pProviderHandler = OutputFactory.createProviderHandler(Settings.Default.output);
                this.pProviderHandler.OnConnect += pProviderHandler_OnConnect;
                this.pProviderHandler.OnDisconnect += pProviderHandler_OnDisconnect;
                
                return true;
            }
            catch (Exception pError)
            {
                // Tear down.
                try
                {
                    this.disconnectProviderHandler();
                }
                catch { }

                // Report the error.
                showMessage(pError.Message, MessageType.Error);
                //MessageBox.Show(pError.Message, "WiiTUIO", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        void pProviderHandler_OnDisconnect()
        {
            providerHandlerConnected = false;
        }

        void pProviderHandler_OnConnect()
        {
            providerHandlerConnected = true;
        }

        /// <summary>
        /// Create the link to the Windows 7 HID driver.
        /// </summary>
        /// <returns></returns>
        private bool connectProviderHandler()
        {
            try
            {
                this.pProviderHandler.connect();
                return true;
            }
            catch (Exception pError)
            {
                // Tear down.
                try
                {
                    this.disconnectProviderHandler();
                }
                catch { }

                // Report the error.
                showMessage(pError.Message, MessageType.Error);
                //MessageBox.Show(pError.Message, "WiiTUIO", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Destroy the link to the Windows 7 HID driver.
        /// </summary>
        /// <returns></returns>
        private void disconnectProviderHandler()
        {
            // Remove any provider links.
            //if (this.pTouchDevice != null)
            //    this.pTouchDevice.Provider = null;
            if (this.pProviderHandler != null)
            {
                this.pProviderHandler.disconnect();
            }
        }

        #endregion


        #region WiiProvider


        /// <summary>
        /// Try to create the WiiProvider (this involves connecting to the Wiimote).
        /// </summary>
        private void connectProvider()
        {
            if (!this.tryingToConnect)
            {

                this.tbConnect.Visibility = Visibility.Collapsed;
                this.tbWaiting.Visibility = Visibility.Visible;

                Thread thread = new Thread(new ThreadStart(tryConnectingProvider));
                thread.Start();
            }
        }

        private void tryConnectingProvider()
        {
            this.tryingToConnect = true;
            while (this.tryingToConnect && !this.startProvider())
            {
                System.Threading.Thread.Sleep(2000);
            }
            this.tryingToConnect = false;
        }

        /// <summary>
        /// Try to create the WiiProvider (this involves connecting to the Wiimote).
        /// </summary>
        private bool startProvider()
        {
            try
            {
                this.pWiiProvider.start();
                return true;
            }
            catch (Exception pError)
            {
                // Tear down.
                try
                {
                    this.pWiiProvider.stop();
                }
                catch { }

                // Report the error.
                Console.WriteLine(pError.Message);
                //showMessage(pError.Message, MessageType.Error);
                //MessageBox.Show(pError.Message, "WiiTUIO", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        
        /// <summary>
        /// Try to create the WiiProvider (this involves connecting to the Wiimote).
        /// </summary>
        private bool createProvider()
        {
            try
            {
                // Connect a Wiimote, hook events then start.
                this.pWiiProvider = InputFactory.createInputProvider(Settings.Default.input);
                this.pWiiProvider.OnButtonDown += pWiiProvider_OnButtonDown;
                this.pWiiProvider.OnButtonUp += pWiiProvider_OnButtonUp;
                this.pWiiProvider.OnNewFrame += new EventHandler<FrameEventArgs>(pWiiProvider_OnNewFrame);
                this.pWiiProvider.OnBatteryUpdate += new Action<int>(pWiiProvider_OnBatteryUpdate);
                this.pWiiProvider.OnConnect += new Action<int>(pWiiProvider_OnConnect);
                this.pWiiProvider.OnDisconnect += new Action<int>(pWiiProvider_OnDisconnect);
                return true;
            }
            catch (Exception pError)
            {
                // Tear down.
                try
                {
                    
                }
                catch { }

                // Report the error.
                showMessage(pError.Message, MessageType.Error);
                //MessageBox.Show(pError.Message, "WiiTUIO", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        void pWiiProvider_OnButtonUp(int obj)
        {
            this.connectProviderHandler();
        }

        void pWiiProvider_OnButtonDown(int obj)
        {
            this.disconnectProviderHandler();
        }

        /// <summary>
        /// Tear down the provider connections.
        /// </summary>
        private void disconnectProvider()
        {
            this.tryingToConnect = false;
            // Disconnect the Wiimote.
            if (this.pWiiProvider != null)
                this.pWiiProvider.stop();
            //this.pWiiProvider = null;
        }
        #endregion

        #region Form Stuff


        ~ClientForm()
        {
            // Disconnect the providers.
            this.disconnectProvider();
        }

        private Hyperlink createHyperlink(string sText, string sUri)
        {
            Hyperlink link = new Hyperlink();
            link.Inlines.Add(sText);
            link.NavigateUri = new Uri(sUri);
            link.Click += oLinkToBrowser;
            return link;
        }

        private RoutedEventHandler oLinkToBrowser = new RoutedEventHandler(delegate(object oSource, RoutedEventArgs pArgs)
        {
            System.Diagnostics.Process.Start((oSource as Hyperlink).NavigateUri.ToString());
        });
        #endregion



        #region UI Events

        /// <summary>
        /// Called when there is a mouse down event over the 'brdOverlay'
        /// </summary>
        /// Enables the messages that are displayed to disappear on mouse down events
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void brdOverlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            messageFadeOut(750.0);
        }

        /// <summary>
        /// Called when the 'Connect' or 'Disconnect' button is clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (!this.bConnected && !this.tryingToConnect)
            {
                this.connectProvider();
            }
            else
            {
                this.disconnectProvider();
            }
        }

        /// <summary>
        /// Called when exit is clicked in the context menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown(0);
        }


        /// <summary>
        /// Called when the 'About' button is clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAbout_Click(object sender, RoutedEventArgs e)
        {
            this.infoOverlay.Visibility = Visibility.Visible;

            /*
            TextBlock pMessage = new TextBlock();
            pMessage.TextWrapping = TextWrapping.Wrap;
            pMessage.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            pMessage.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            pMessage.FontSize = 11.0;
            pMessage.FontWeight = FontWeights.Bold;

            pMessage.Inlines.Add("Touchmote communicates with a Wii Remote to simulate touch events as a TUIO server or Windows 7/8 touch messages.\n\n");
            pMessage.Inlines.Add("You will have to pair the Wii Remote manually before connecting with Touchmote.\n");
            pMessage.Inlines.Add("Please visit ");
            pMessage.Inlines.Add(createHyperlink("touchmote.net", "http://www.touchmote.net/"));
            pMessage.Inlines.Add(" for more information\n\n");

            pMessage.Inlines.Add("\n\nCredits:\n  ");
            pMessage.Inlines.Add(createHyperlink("WiiTUIO project", "http://code.google.com/p/wiituio/"));
            pMessage.Inlines.Add("\n  ");
            pMessage.Inlines.Add(createHyperlink("Johnny Chung Lee", "http://johnnylee.net/projects/wii/"));
            pMessage.Inlines.Add("\n  ");
            pMessage.Inlines.Add(createHyperlink("Brian Peek", "http://www.brianpeek.com/"));
            pMessage.Inlines.Add("\n  ");
            pMessage.Inlines.Add(createHyperlink("Nesher", "http://www.codeplex.com/site/users/view/nesher"));
            pMessage.Inlines.Add("\n  ");
            pMessage.Inlines.Add(createHyperlink("TUIO Project", "http://www.tuio.org"));
            pMessage.Inlines.Add("\n  ");
            pMessage.Inlines.Add(createHyperlink("MultiTouchVista", "http://multitouchvista.codeplex.com/"));
            pMessage.Inlines.Add("\n  ");
            pMessage.Inlines.Add(createHyperlink("OSC.NET Library", "http://luvtechno.net/"));
            pMessage.Inlines.Add("\n  ");
            pMessage.Inlines.Add(createHyperlink("WiimoteLib 1.7", "http://wiimotelib.codeplex.com/"));
            pMessage.Inlines.Add("\n  ");
            pMessage.Inlines.Add(createHyperlink("HIDLibrary", "http://hidlibrary.codeplex.com/"));
            pMessage.Inlines.Add("\n  ");
            pMessage.Inlines.Add(createHyperlink("WPFNotifyIcon", "http://www.hardcodet.net/projects/wpf-notifyicon"));

            showMessage(pMessage, MessageType.Info);
             * */
        }

        #endregion

        private void ComboBox_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if (ModeComboBox.SelectedItem != null && ((ComboBoxItem)ModeComboBox.SelectedItem).Content != null)
            {
                ComboBoxItem cbItem = (ComboBoxItem)ModeComboBox.SelectedItem;
                if (cbItem == cbiPointer)
                {
                    this.disconnectProvider();
                    Settings.Default.input = InputFactory.getType(InputFactory.InputType.POINTER);
                    this.createProvider();
                }
                else if (cbItem == cbiPen)
                {
                    this.disconnectProvider();
                    Settings.Default.input = InputFactory.getType(InputFactory.InputType.PEN);
                    this.createProvider();
                }
            }
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            if (this.pWiiProvider != null)
            {
                this.providerSettingsContent.Children.Clear();
                this.providerSettingsContent.Children.Add(this.pWiiProvider.getSettingsControl());
                this.disableMainControls();
                this.providerSettingsOverlay.Visibility = Visibility.Visible;
            }
        }
        /*
        private void OutputComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (OutputComboBox.SelectedItem != null)
            {
                ComboBoxItem cbItem = (ComboBoxItem)OutputComboBox.SelectedItem;
                if (cbItem == cbiTUIO)
                {
                    this.disconnectProviderHandler();
                    Settings.Default.output = OutputFactory.getType(OutputFactory.OutputType.TUIO);
                    this.createProviderHandler();
                }
                else if (cbItem == cbiTouch)
                {
                    this.disconnectProviderHandler();
                    Settings.Default.output = OutputFactory.getType(OutputFactory.OutputType.TOUCH);
                    this.createProviderHandler();
                }
            }
        }
        */
        private void btnOutputSettings_Click(object sender, RoutedEventArgs e)
        {
            if (this.pProviderHandler != null)
            {
                this.pProviderHandler.showSettingsWindow();
            }
        }

        private void PairWiimotes_Click(object sender, RoutedEventArgs e)
        {
            this.disableMainControls();
            this.pairWiimoteOverlay.Visibility = Visibility.Visible;
            this.pairWiimoteOverlayPairing.Visibility = Visibility.Visible;
            this.pairWiimoteOverlayDone.Visibility = Visibility.Hidden;
            this.runWiiPair();
        }

        private void runWiiPair() {
            Dispatcher.BeginInvoke(new Action(delegate()
            {
            this.pairingTitle.Content = "Pairing Wiimote";
            this.pairWiimoteTryAgain.Visibility = Visibility.Hidden;
            }), null);
            Thread thread = new Thread(new ThreadStart(wiiPair.start));
            thread.Start();
        }

        private void stopWiiPair() {
            wiiPair.stop();
        }

        public void onPairingSuccess(WiiCPP.WiiPairSuccessReport report)
        {
            Console.WriteLine("Success report: number=" + report.numberPaired + " permanent=" + report.permanent);

            Settings.Default.pairedOnce = true;
            Dispatcher.BeginInvoke(new Action(delegate()
            {
                this.pairWiimoteOverlayPairing.Visibility = Visibility.Hidden;
                this.pairWiimoteOverlayDone.Visibility = Visibility.Visible;
            }), null);
        }


        private void pairWiimoteTryAgain_Click(object sender, RoutedEventArgs e)
        {
            this.stopWiiPair();
            this.runWiiPair();
        }

        public void onPairingCancelled()
        {
            Dispatcher.BeginInvoke(new Action(delegate()
            {
            this.pairingTitle.Content = "Pairing Cancelled";
            this.pairWiimoteTryAgain.Visibility = Visibility.Visible;

            this.pairProgress.IsIndeterminate = false;
            }), null);
        }

        public void onPairingStarted()
        {
            Dispatcher.BeginInvoke(new Action(delegate()
            {

            this.pairProgress.IsIndeterminate = true;
            }), null);
        }

        public void pairingConsole(string message)
        {
            Console.Write(message);
        }

        public void pairingMessage(string message, WiiCPP.WiiPairListener.MessageType type)
        {
            Dispatcher.BeginInvoke(new Action(delegate()
            {
                this.pairWiimoteText.Text = message;
                if (message == "Scanning...")
                {
                    pairWiimotePressSync.Visibility = Visibility.Visible;
                }
                else
                {
                    pairWiimotePressSync.Visibility = Visibility.Hidden;
                }
            }), null);
        }

        private void imgClosePair_MouseUp(object sender, MouseButtonEventArgs e)
        {
            this.stopWiiPair();
            this.pairWiimoteOverlay.Visibility = Visibility.Hidden;
            this.enableMainControls();
        }

        private void Icon_MouseEnter(object sender, MouseEventArgs e)
        {
            ((Image)sender).Opacity = ((Image)sender).Opacity + 0.2;
        }

        private void Icon_MouseLeave(object sender, MouseEventArgs e)
        {
            ((Image)sender).Opacity = ((Image)sender).Opacity - 0.2;
        }

        private void InfoImg_MouseUp(object sender, MouseButtonEventArgs e)
        {
            this.btnAbout_Click(null, null);
        }

        private void imgClose_MouseUp(object sender, MouseButtonEventArgs e)
        {
            this.btnConnect_Click(null, null);
        }

        private void imgPair_MouseEnter(object sender, MouseEventArgs e)
        {
            imgConnect.Opacity = 0.6;
        }

        private void imgPair_MouseLeave(object sender, MouseEventArgs e)
        {
            imgConnect.Opacity = 0.4;
        }

        private void imgConnect_MouseEnter(object sender, MouseEventArgs e)
        {
            imgConnect.Opacity = 0.6;
        }

        private void imgConnect_MouseLeave(object sender, MouseEventArgs e)
        {
            imgConnect.Opacity = 0.4;
        }

        private void ConfigImg_MouseUp(object sender, MouseButtonEventArgs e)
        {
            this.showConfig();
        }

        private void cbWindowsStart_Checked(object sender, RoutedEventArgs e)
        {
            Console.WriteLine(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            this.winStartupRegisterKey.SetValue(appKey, System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
        }

        private void cbWindowsStart_Unchecked(object sender, RoutedEventArgs e)
        {
            this.winStartupRegisterKey.DeleteValue(appKey, false);
        }

        private void cbConnectOnStart_Checked(object sender, RoutedEventArgs e)
        {
            Settings.Default.connectOnStart = true;
        }

        private void cbConnectOnStart_Unchecked(object sender, RoutedEventArgs e)
        {
            Settings.Default.connectOnStart = false;
        }

        private void btnConfigDone_Click(object sender, RoutedEventArgs e)
        {
            this.hideConfig();
        }

        private void btnProviderSettingsDone_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.Save();
            this.providerSettingsOverlay.Visibility = Visibility.Hidden;
            this.enableMainControls();
        }

        private void driverNotInstalled()
        {
            Dispatcher.BeginInvoke(new Action(delegate()
            {
                this.disableMainControls();
                this.driverMissingOverlay.Visibility = Visibility.Visible;
            }), null);
        }

        private void linkInstallDriver_Click(object sender, RoutedEventArgs e)
        {
            Launcher.Launch("", "elevate", "DriverInstall.exe -install", new Action(delegate()
            {
                
            }));
            this.driverMissingOverlay.IsEnabled = false;
            Thread thread = new Thread(new ThreadStart(waitForDriver));
            thread.Start();
        }

        private void waitForDriver()
        {
            while (!ProviderHandler.HasDriver())
            {
                System.Threading.Thread.Sleep(3000);
            }
            this.driverInstalled();
        }

        private void driverInstalled()
        {
            Dispatcher.BeginInvoke(new Action(delegate()
            {
                this.driverMissingOverlay.Visibility = Visibility.Hidden;
                this.driverInstalledOverlay.Visibility = Visibility.Visible;
            }), null);
        }

        private void btnRestart_Click(object sender, RoutedEventArgs e)
        {
            Launcher.RestartComputer();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void infoOverlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.infoOverlay.Visibility = Visibility.Hidden;
        }

        private void disableMainControls() {
            this.canvasMain.IsEnabled = false;
        }

        private void enableMainControls()
        {
            this.canvasMain.IsEnabled = true;
        }

    }

    
}
