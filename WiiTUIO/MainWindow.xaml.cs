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

using OSC.NET;
using WiiTUIO.WinTouch;
using WiiTUIO.Provider;
using WiiTUIO.Input;
using WiiTUIO.Properties;
using System.Windows.Input;
using WiiTUIO.Output;
using Microsoft.Win32;
using System.Diagnostics;
using TCD.System.ApplicationExtensions;
using Newtonsoft.Json;
using MahApps.Metro.Controls;

namespace WiiTUIO
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow, WiiCPP.WiiPairListener
    {
        private bool minimizedOnce = false;

        private Thread wiiPairThread;

        private bool providerHandlerConnected = false;

        private bool tryingToConnect = false;

        private bool startupPair = false;

        private Mutex statusStackMutex = new Mutex();

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
        public MainWindow()
        {
            
            // Load from the XAML.
            InitializeComponent();
            this.Initialize();
            //Process currentProcess = Process.GetCurrentProcess();
            //currentProcess.PriorityClass = ProcessPriorityClass.RealTime;
            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            if (Settings.Default.minimizeToTray)
            {
                MinimizeToTray.Enable(this);
            }
        }

        public async void Initialize()
        {
            this.mainPanel.Visibility = Visibility.Visible;
            this.canvasSettings.Visibility = Visibility.Collapsed;
            this.canvasAbout.Visibility = Visibility.Collapsed;
            this.canvasPairing.Visibility = Visibility.Collapsed;
            this.tbPair2.Visibility = Visibility.Visible;
            this.tbPairDone.Visibility = Visibility.Collapsed;

           
            //this.cbConnectOnStart.IsChecked = Settings.Default.connectOnStart;

            Application.Current.Exit += appWillExit;

            wiiPair = new WiiCPP.WiiPair();
            wiiPair.addListener(this);

            Settings.Default.PropertyChanged += Settings_PropertyChanged;

            // Create the providers.
            this.createProvider();
            this.createProviderHandler();

            if (Settings.Default.pairOnStart)
            {
                this.startupPair = true;
                this.runWiiPair();
            }
            else if (Settings.Default.connectOnStart)
            {
                this.connectProvider();
            }

            AppSettingsUC settingspanel = new AppSettingsUC();
            settingspanel.OnClose += SettingsPanel_OnClose;

            this.canvasSettings.Children.Add(settingspanel);

            AboutUC aboutpanel = new AboutUC();
            aboutpanel.OnClose += AboutPanel_OnClose;

            this.canvasAbout.Children.Add(aboutpanel);

        }

        private void AboutPanel_OnClose()
        {
            this.hideAbout();
        }

        private void SettingsPanel_OnClose()
        {
            this.hideConfig();
        }

        void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.FrameworkElement.Initialized"/> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
        protected override void OnActivated(EventArgs e)
        {
            // Call the base class.
            base.OnActivated(e);
            if (!this.minimizedOnce && Settings.Default.minimizeOnStart)
            {
                this.WindowState = System.Windows.WindowState.Minimized;
                this.minimizedOnce = true;
            }
        }
        
        private void appWillExit(object sender, ExitEventArgs e)
        {
            this.stopWiiPair();
            this.disconnectProvider();
            this.disconnectProviderHandler();
        }


        /// <summary>
        /// This is called when the wii remote is connected
        /// </summary>
        /// <param name="obj"></param>
        private void pWiiProvider_OnConnect(int ID, int totalWiimotes)
        {
            // Dispatch it.
            Dispatcher.BeginInvoke(new Action(delegate()
            {
                this.bConnected = true;

                // Update the button to say we are connected.
                //tbConnected.Visibility = Visibility.Hidden;
                //tbWaiting.Visibility = Visibility.Hidden;
                //tbConnected.Visibility = Visibility.Visible;

                this.connectedCount.Content = totalWiimotes;
                statusStackMutex.WaitOne();
                this.statusStack.Children.Add(new WiimoteStatusUC(ID));
                statusStackMutex.ReleaseMutex();

                connectProviderHandler();

            }), null);


        }

        /// <summary>
        /// This is called when the wii remote is disconnected
        /// </summary>
        /// <param name="obj"></param>
        private void pWiiProvider_OnDisconnect(int ID, int totalWiimotes)
        {
            // Dispatch it.
            Dispatcher.BeginInvoke(new Action(delegate()
            {
                statusStackMutex.WaitOne();
                foreach (UIElement child in this.statusStack.Children)
                {
                    WiimoteStatusUC uc = (WiimoteStatusUC)child;
                    if (uc.ID == ID)
                    {
                        this.statusStack.Children.Remove(child);
                        break;
                    }
                }
                statusStackMutex.ReleaseMutex();
                if (totalWiimotes == 0)
                {
                    this.bConnected = false;
                    
                    tbConnected.Visibility = Visibility.Hidden;
                    if (tryingToConnect)
                    {
                        tbWaiting.Visibility = Visibility.Visible;
                        tbConnect.Visibility = Visibility.Hidden;
                    }
                    else if(!Settings.Default.pairedOnce)
                    {
                        tbWaiting.Visibility = Visibility.Hidden;
                        tbConnect.Visibility = Visibility.Hidden;
                        tbPair.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        tbWaiting.Visibility = Visibility.Hidden;
                        tbConnect.Visibility = Visibility.Visible;
                        tbPair.Visibility = Visibility.Hidden;
                    }

                    batteryLabel.Content = "0%";

                    disconnectProviderHandler();
                }

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
        private void pWiiProvider_OnStatusUpdate(WiimoteStatus status)
        {
            // Dispatch it.
            Dispatcher.BeginInvoke(new Action(delegate()
            {
                statusStackMutex.WaitOne();
                foreach(UIElement child in this.statusStack.Children) {
                    WiimoteStatusUC uc = (WiimoteStatusUC)child;
                    if (uc.ID == status.ID)
                    {
                        uc.updateStatus(status);
                    }
                }
                statusStackMutex.ReleaseMutex();
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
            this.mainPanel.Visibility = Visibility.Collapsed;
            this.canvasAbout.Visibility = Visibility.Collapsed;
            this.canvasSettings.Visibility = Visibility.Visible;
        }

        private void hideConfig()
        {
            //this.enableMainControls();
            //this.configOverlay.Visibility = Visibility.Hidden;
            this.canvasSettings.Visibility = Visibility.Collapsed;
            this.canvasAbout.Visibility = Visibility.Collapsed;
            this.mainPanel.Visibility = Visibility.Visible;
        }

        private void showAbout()
        {
            this.mainPanel.Visibility = Visibility.Collapsed;
            this.canvasAbout.Visibility = Visibility.Visible;
            this.canvasSettings.Visibility = Visibility.Collapsed;
        }

        private void hideAbout()
        {
            this.canvasSettings.Visibility = Visibility.Collapsed;
            this.canvasAbout.Visibility = Visibility.Collapsed;
            this.mainPanel.Visibility = Visibility.Visible;
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
                Dispatcher.BeginInvoke(new Action(delegate()
                {
                    this.tbPair.Visibility = Visibility.Hidden;
                    this.tbConnect.Visibility = Visibility.Hidden;
                    this.tbWaiting.Visibility = Visibility.Visible;
                }), null);

                Launcher.Launch("Driver", "devcon", " enable \"BTHENUM*_VID*57e*_PID&0306*\"", null);

                this.startProvider();

                /*
                Thread thread = new Thread(new ThreadStart(tryConnectingProvider));
                thread.Start();
                 * */
            }
        }
        /*
        private void tryConnectingProvider()
        {
            this.tryingToConnect = true;
            while (this.tryingToConnect && !this.startProvider())
            {
                System.Threading.Thread.Sleep(2000);
            }
            this.tryingToConnect = false;
        }
        */
        /// <summary>
        /// Try to create the WiiProvider (this involves connecting to the Wiimote).
        /// </summary>
        private bool startProvider()
        {
            try
            {
                this.pWiiProvider.start();
                this.tryingToConnect = true;
                return true;
            }
            catch (Exception pError)
            {
                // Tear down.
                try
                {
                    this.pWiiProvider.stop();
                    this.tryingToConnect = false;
                }
                catch { }

                // Report the error.
                Console.WriteLine(pError.Message);
                showMessage(pError.Message, MessageType.Error);
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
                this.pWiiProvider.OnNewFrame += new EventHandler<FrameEventArgs>(pWiiProvider_OnNewFrame);
                this.pWiiProvider.OnStatusUpdate += new Action<WiimoteStatus>(pWiiProvider_OnStatusUpdate);
                this.pWiiProvider.OnConnect += new Action<int,int>(pWiiProvider_OnConnect);
                this.pWiiProvider.OnDisconnect += new Action<int,int>(pWiiProvider_OnDisconnect);
                return true;
            }
            catch (Exception pError)
            {
                // Tear down.
                try
                {
                    
                }
                catch { }
                Console.WriteLine(pError.Message);
                // Report the error.cr
                showMessage(pError.Message, MessageType.Error);
                //MessageBox.Show(pError.Message, "WiiTUIO", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Tear down the provider connections.
        /// </summary>
        private void disconnectProvider()
        {
            this.tryingToConnect = false;
            // Disconnect the Wiimote.
            if (this.pWiiProvider != null)
            {
                this.pWiiProvider.stop();
            }

            //this.pWiiProvider = null;
            if (Settings.Default.completelyDisconnect)
            {
                //Disable Wiimote in device manager to disconnect it from the computer (so it doesn't drain battery when not used)
                Launcher.Launch("Driver", "devcon", " disable \"BTHENUM*_VID*57e*_PID&0306*\"", null);
            }
        }
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

        #endregion


        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void btnOutputSettings_Click(object sender, RoutedEventArgs e)
        {
            if (this.pProviderHandler != null)
            {
                this.pProviderHandler.showSettingsWindow();
            }
        }

        private bool wiiPairRunning = false;

        private void PairWiimotes_Click(object sender, RoutedEventArgs e)
        {
            //this.disableMainControls();
            //this.pairWiimoteOverlay.Visibility = Visibility.Visible;
            //this.pairWiimoteOverlayPairing.Visibility = Visibility.Visible;

            this.runWiiPair();
        }

        private void runWiiPair() {
            if (!this.wiiPairRunning)
            {
                Dispatcher.BeginInvoke(new Action(delegate()
                {
                    this.canvasPairing.Visibility = Visibility.Visible;
                    this.tbPair2.Visibility = Visibility.Collapsed;
                    this.tbPairDone.Visibility = Visibility.Visible;

                    this.pairingTitle.Content = "Pairing Wiimotes";
                    this.pairWiimoteTRFail.Visibility = Visibility.Hidden;
                    this.pairWiimoteTryAgain.Visibility = Visibility.Hidden;
                    this.imgClosePairCheck.Visibility = Visibility.Hidden;
                    this.imgClosePairClose.Visibility = Visibility.Visible;
                    this.pairWiimoteCheckmarkImg.Visibility = Visibility.Hidden;
                    this.pairProgress.Visibility = Visibility.Visible;
                }), null);
                if (this.wiiPairThread != null)
                {
                    this.wiiPairThread.Abort();
                }
                this.wiiPairThread = new Thread(new ThreadStart(wiiPairThreadWorker));
                this.wiiPairThread.Priority = ThreadPriority.Normal;
                this.wiiPairThread.Start();
            }
        }

        private void wiiPairThreadWorker()
        {
            this.wiiPairRunning = true;
            wiiPair.start(true,10);//First remove all connected devices.
        }

        private void stopWiiPair() {
            wiiPair.stop();
        }

        public void onPairingSuccess(WiiCPP.WiiPairSuccessReport report)
        {
            Console.WriteLine("Success report: number=" + report.numberPaired + " removeMode=" + report.removeMode + " devicelist=" + report.deviceNames);

            if (report.numberPaired > 0)
            {
                Settings.Default.pairedOnce = true;
                
                Dispatcher.BeginInvoke(new Action(delegate()
                {
                    if (report.numberPaired == 1)
                    {
                        this.pairingTitle.Content = "One Wiimote Paired";
                    }
                    else
                    {
                        this.pairingTitle.Content = report.numberPaired + " Wiimotes Paired";
                    }
                    this.imgClosePairCheck.Visibility = Visibility.Visible;
                    this.imgClosePairClose.Visibility = Visibility.Hidden;
                }), null);
                
                if (!this.wiiPairRunning)
                {
                    if (report.deviceNames.Contains(@"Nintendo RVL-CNT-01-TR"))
                    {
                        Dispatcher.BeginInvoke(new Action(delegate()
                        {
                            //this.pairingTitle.Content = "Pairing Successful";
                            this.pairWiimoteText.Text = @"";
                            this.pairWiimotePressSync.Visibility = Visibility.Hidden;
                            this.pairWiimoteTRFail.Visibility = Visibility.Visible;
                            this.pairWiimoteTryAgain.Visibility = Visibility.Visible;

                            this.pairProgress.Visibility = Visibility.Hidden;
                            this.pairProgress.IsActive = false;
                            //this.pairProgress.IsIndeterminate = false;

                        }), null);
                    }
                    else
                    {
                        Dispatcher.BeginInvoke(new Action(delegate()
                        {
                            //this.pairWiimoteOverlayPairing.Visibility = Visibility.Hidden;
                            //this.pairWiimoteOverlayDone.Visibility = Visibility.Visible;
                            this.pairWiimoteText.Text = @"";
                            this.pairWiimotePressSync.Visibility = Visibility.Hidden;
                            this.pairWiimoteTryAgain.Visibility = Visibility.Hidden;
                            this.pairWiimoteCheckmarkImg.Visibility = Visibility.Visible;

                            this.pairProgress.Visibility = Visibility.Hidden;

                            this.pairProgress.IsActive = false;

                            this.canvasPairing.Visibility = Visibility.Collapsed;
                            this.tbPair2.Visibility = Visibility.Visible;
                            this.tbPairDone.Visibility = Visibility.Collapsed;
                        }), null);
                    }
                }
            }
            else
            {
                Dispatcher.BeginInvoke(new Action(delegate()
                {
                    this.imgClosePairCheck.Visibility = Visibility.Hidden;
                    this.imgClosePairClose.Visibility = Visibility.Visible;
                }), null);
            }
        }


        private void pairWiimoteTryAgain_Click(object sender, RoutedEventArgs e)
        {
            this.stopWiiPair();
            this.runWiiPair();
        }

        public void onPairingDone(WiiCPP.WiiPairSuccessReport report)
        {
            
            if (report.removeMode)
            {
                this.wiiPairRunning = true;

                Dispatcher.BeginInvoke(new Action(delegate()
                {
                    this.imgClosePairCheck.Visibility = Visibility.Hidden;
                    this.imgClosePairClose.Visibility = Visibility.Visible;

                    this.connectProvider();
                }), null);
                int stopat = 10;
                if (this.startupPair)
                {
                    stopat = 1;
                    this.startupPair = false;
                }
                wiiPair.start(false,stopat); //Run the actual pairing after removing all previous connected devices.
            }
            else
            {
                this.wiiPairRunning = false;
                Dispatcher.BeginInvoke(new Action(delegate()
                {
                    //this.pairingTitle.Content = "Pairing Cancelled";
                    //this.pairWiimoteTryAgain.Visibility = Visibility.Visible;
                    this.imgClosePairCheck.Visibility = Visibility.Hidden;
                    this.imgClosePairClose.Visibility = Visibility.Visible;
                    //this.pairWiimoteCheckmarkImg.Visibility = Visibility.Hidden;
                    this.canvasPairing.Visibility = Visibility.Collapsed;
                    this.tbPair2.Visibility = Visibility.Visible;
                    this.tbPairDone.Visibility = Visibility.Collapsed;

                    this.pairProgress.IsActive = false;
                }), null);
            }
        }

        public void onPairingStarted()
        {
            this.disconnectProvider();
            Dispatcher.BeginInvoke(new Action(delegate()
            {

                this.pairProgress.IsActive = true;
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

                    if (this.imgClosePairCheck.Visibility == Visibility.Hidden && this.imgClosePairClose.Visibility == Visibility.Hidden)
                    {
                        this.imgClosePairClose.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    pairWiimotePressSync.Visibility = Visibility.Hidden;
                }
            }), null);
        }

        private void imgClosePair_MouseUp(object sender, MouseButtonEventArgs e)
        {
            
            if (this.wiiPairRunning)
            {
                if (this.imgClosePairClose.Visibility == Visibility.Visible)
                {
                    this.pairWiimoteText.Text = "Cancelling...";
                }
                else
                {
                    this.pairWiimoteText.Text = "Finishing...";
                }
                this.imgClosePairCheck.Visibility = Visibility.Hidden;
                this.imgClosePairClose.Visibility = Visibility.Hidden;
                this.pairWiimotePressSync.Visibility = Visibility.Hidden;
                this.stopWiiPair();
            }
            else
            {
                this.pairWiimoteOverlay.Visibility = Visibility.Hidden;
                this.enableMainControls();
            }
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
            while (!TUIOVmultiProviderHandler.HasDriver())
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

        private void btnAppSettings_Click(object sender, RoutedEventArgs e)
        {
            this.showConfig();
        }

        private void btnAbout_Click(object sender, RoutedEventArgs e)
        {
            this.showAbout();
        }


        private void btnAboutBack_Click(object sender, RoutedEventArgs e)
        {
            this.hideAbout();
        }

        private void PairWiimotesDone_Click(object sender, RoutedEventArgs e)
        {
            if (this.wiiPairRunning)
            {
                if (this.imgClosePairClose.Visibility == Visibility.Visible)
                {
                    this.pairWiimoteText.Text = "Cancelling...";
                }
                else
                {
                    this.pairWiimoteText.Text = "Finishing...";
                }
                this.imgClosePairCheck.Visibility = Visibility.Hidden;
                this.imgClosePairClose.Visibility = Visibility.Hidden;
                
                //this.pairWiimotePressSync.Visibility = Visibility.Hidden;
                this.stopWiiPair();
            }
            else
            {
                //this.pairWiimoteOverlay.Visibility = Visibility.Hidden;
                //this.enableMainControls();
            }
        }

    }

    
}
