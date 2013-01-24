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
using System.Windows.Input;
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

namespace WiiTUIO
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class ClientForm : UserControl
    {

        private enum Mode
        {
            POINTER,
            PEN
        }

        private Mode currentMode = Mode.POINTER;


        /// <summary>
        /// A reference to the WiiProvider we want to use to get/forward input.
        /// </summary>
        private IProvider pWiiProvider = null;

        /// <summary>
        /// A reference to an OSC data transmitter.
        /// </summary>
        private OSCTransmitter pUDPWriter = null;

        /// <summary>
        /// A reference to the windows 7 HID driver data provider.  This takes data from the <see cref="pWiiProvider"/> and transforms it.
        /// </summary>
        private ProviderHandler pTouchDevice = null;

        /// <summary>
        /// A refrence to the calibration window.
        /// </summary>
        private CalibrationWindow pCalibrationWindow = null;

        /// <summary>
        /// Boolean to tell if we are connected to the mote and network.
        /// </summary>
        private bool bConnected = false;

        /// <summary>
        /// Boolean to tell if we have received a reconnect command.
        /// </summary>
        private bool bReconnect = false;

        /// <summary>
        /// Are windows touch events enabled.
        /// </summary>
        private bool bWindowsTouch = false;

        /// <summary>
        /// Are TUIO touch events enabled.
        /// </summary>
        private bool bTUIOTouch = false;

        /// <summary>
        /// Construct a new Window.
        /// </summary>
        public ClientForm()
        {
            // Load from the XAML.
            InitializeComponent();

            // Create a calibration window and hide it.
            this.pCalibrationWindow = new CalibrationWindow();
            this.pCalibrationWindow.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// This is called when calibration is finished.
        /// </summary>
        private void CalibrationCanvas_OnCalibrationFinished(WiiProvider.CalibrationRectangle pSource, WiiProvider.CalibrationRectangle pDestination, Vector vScreenSize)
        {
            // Persist the calibration data
            if (!savePersistentCalibration("./Calibration.dat", new PersistentCalibrationData(pSource, pDestination, vScreenSize)))
            {
                // Error - Failed to save calibration data
                MessageBox.Show("Failed to save calibration data", "WiiTUIO", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Close the calibration window.
            if (pCalibrationWindow != null)
            {
                pCalibrationWindow.Visibility = System.Windows.Visibility.Hidden;
            }
        }

        private Mutex pCommunicationMutex = new Mutex();
        private static int iFrame = 0;
        /// <summary>
        /// Process an event frame and convert the data into a TUIO message.
        /// </summary>
        /// <param name="e"></param>
        private void processEventFrame(FrameEventArgs e)
        {
            // Obtain mutual exclusion.
            this.pCommunicationMutex.WaitOne();

            // If Windows 7 events are enabled.
            if (bWindowsTouch)
            {
                // For every contact in the list of contacts.
                foreach (WiiContact pContact in e.Contacts)
                {
                    // Construct a new HID frame based on the contact type.
                    switch (pContact.Type)
                    {
                        case ContactType.Start:
                            this.pTouchDevice.enqueueContact(HidContactState.Adding, pContact);
                            break;
                        case ContactType.Move:
                            this.pTouchDevice.enqueueContact(HidContactState.Updated, pContact);
                            break;
                        case ContactType.End:
                            this.pTouchDevice.enqueueContact(HidContactState.Removing, pContact);
                            break;
                    }
                }

                // Flush the contacts?
                this.pTouchDevice.sendContacts();
            }


            // If TUIO events are enabled.
            if (bTUIOTouch)
            {
                // Create an new TUIO Bundle
                OSCBundle pBundle = new OSCBundle();

                // Create a fseq message and save it.  This is to associate a unique frame id with a bundle of SET and ALIVE.
                OSCMessage pMessageFseq = new OSCMessage("/tuio/2Dcur");
                pMessageFseq.Append("fseq");
                pMessageFseq.Append(++iFrame);//(int)e.Timestamp);
                pBundle.Append(pMessageFseq);

                // Create a alive message.
                OSCMessage pMessageAlive = new OSCMessage("/tuio/2Dcur");
                pMessageAlive.Append("alive");

                // Now we want to take the raw frame data and draw points based on its data.
                foreach (WiiContact pContact in e.Contacts)
                {
                    // Compile the set message.
                    OSCMessage pMessage = new OSCMessage("/tuio/2Dcur");
                    pMessage.Append("set");                 // set
                    pMessage.Append((int)pContact.ID);           // session
                    pMessage.Append((float)pContact.NormalPosition.X);   // x
                    pMessage.Append((float)pContact.NormalPosition.Y);   // y
                    pMessage.Append(0f);                 // dx
                    pMessage.Append(0f);                 // dy
                    pMessage.Append(0f);                 // motion
                    pMessage.Append((float)pContact.Size.X);   // height
                    pMessage.Append((float)pContact.Size.Y);   // width

                    // Append it to the bundle.
                    pBundle.Append(pMessage);

                    // Append the alive message for this contact to tbe bundle.
                    pMessageAlive.Append((int)pContact.ID);
                }

                // Save the alive message.
                pBundle.Append(pMessageAlive);

                // Send the message off.
                this.pUDPWriter.Send(pBundle);
            }

            // And release it!
            pCommunicationMutex.ReleaseMutex();
        }

        /// <summary>
        /// This is called when the WiiProvider has a new set of input to send.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pWiiProvider_OnNewFrame(object sender, FrameEventArgs e)
        {
            // Are we calibrating.
            if (this.pCalibrationWindow != null)
                if (this.pCalibrationWindow.CalibrationCanvas.IsCalibrating)
                    return;

            // If dispatching events is enabled.
            if (bConnected)
            {
                // Call these in another thread.
                Dispatcher.BeginInvoke(new Action(delegate()
                {
                    processEventFrame(e);
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
                this.barBattery.Value = obj;
            }), null);
        }

        #region Messages - Err/Inf

        enum MessageType { Info, Error };

        private void showMessage(string sMessage, MessageType eType)
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
            showMessage(pMessage, 750.0, eType);
        }

        private void showMessage(UIElement pMessage, MessageType eType)
        {
            showMessage(pMessage, 750.0, eType);
        }

        private void showMessage(UIElement pElement, double fTimeout, MessageType eType)
        {
            // Show (and possibly initialise) the error message overlay
            brdOverlay.Height = this.ActualHeight - 8;
            brdOverlay.Width = this.ActualWidth - 8;
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
        
        #region Create and Die

        #region Windows Touch

        /// <summary>
        /// Create the link to the Windows 7 HID driver.
        /// </summary>
        /// <returns></returns>
        private bool connectWindowsTouch()
        {
            try
            {
                // Close any open connections.
                disconnectWindowsTouch();

                // Reconnect with the new API.
                this.pTouchDevice = new ProviderHandler();
                return true;
            }
            catch (Exception pError)
            {
                // Tear down.
                try
                {
                    this.disconnectWindowsTouch();
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
        private void disconnectWindowsTouch()
        {
            // Remove any provider links.
            //if (this.pTouchDevice != null)
            //    this.pTouchDevice.Provider = null;
            this.pTouchDevice = null;
        }

        #endregion

        #region UDP TUIO
        /// <summary>
        /// Connect the UDP transmitter using the port and IP specified above.
        /// </summary>
        /// <returns></returns>
        private bool connectTransmitter()
        {
            try
            {
                // Close any open connections.
                disconnectTransmitter();

                // Reconnect with the new API.
                pUDPWriter = new OSCTransmitter(txtIPAddress.Text, Int32.Parse(txtPort.Text));
                pUDPWriter.Connect();
                return true;
            }
            catch (Exception pError)
            {
                // Tear down.
                try
                {
                    this.disconnectTransmitter();
                }
                catch { }

                // Report the error.
                showMessage(pError.Message, MessageType.Error);
                return false;
            }
        }

        /// <summary>
        /// Disconnect the UDP Transmitter.
        /// </summary>
        /// <returns></returns>
        private void disconnectTransmitter()
        {
            // Close any open connections.
            if (pUDPWriter != null)
                pUDPWriter.Close();
            pUDPWriter = null;
        }
        #endregion

        #region WiiProvider
        /// <summary>
        /// Try to create the WiiProvider (this involves connecting to the Wiimote).
        /// </summary>
        private bool createProviders()
        {
            try
            {
                // Connect a Wiimote, hook events then start.
                if (this.currentMode == Mode.POINTER)
                {
                    this.pWiiProvider = new WiiPointerProvider();
                }
                else
                {
                    this.pWiiProvider = new WiiProvider();
                }
                this.pWiiProvider.OnNewFrame += new EventHandler<FrameEventArgs>(pWiiProvider_OnNewFrame);
                this.pWiiProvider.OnBatteryUpdate += new Action<int>(pWiiProvider_OnBatteryUpdate);
                this.pWiiProvider.start();
                return true;
            }
            catch (Exception pError)
            {
                // Tear down.
                try
                {
                    this.disconnectProviders();
                }
                catch { }

                // Report the error.
                showMessage(pError.Message, MessageType.Error);
                //MessageBox.Show(pError.Message, "WiiTUIO", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Tear down the provider connections.
        /// </summary>
        private void disconnectProviders()
        {
            // Disconnect the Wiimote.
            if (this.pWiiProvider != null)
                this.pWiiProvider.stop();
            this.pWiiProvider = null;
        }
        #endregion

        #region Form Stuff
        /// <summary>
        /// Raises the <see cref="E:System.Windows.FrameworkElement.Initialized"/> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
        protected override void OnInitialized(EventArgs e)
        {
            // Create the providers.
            //this.createProviders();

            // Add text change events
            txtIPAddress.TextChanged += txtIPAddress_TextChanged;
            txtPort.TextChanged += txtPort_TextChanged;

            // Call the base class.
            base.OnInitialized(e);
        }

        ~ClientForm()
        {
            // Disconnect the providers.
            this.disconnectProviders();
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

        #endregion

        #region Persistent Calibration Data
        /// <summary>
        /// Creates and saves a file which contains the calibration data.
        /// </summary>
        /// <param name="sFile">The location of the file to persist to</param>
        /// <param name="oData">The calibration data to persist</param>
        public static bool savePersistentCalibration(string sFile, PersistentCalibrationData oData)
        {
            try
            {
                FileStream stream = File.Open(sFile, FileMode.Create);
                new BinaryFormatter().Serialize(stream, oData);
                stream.Close();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Loads the specified file, which should contain calibration data.
        /// </summary>
        /// <param name="sFile">The location of the file to load</param>
        public static PersistentCalibrationData loadPersistentCalibration(string sFile)
        {
            try
            {
                if (File.Exists(sFile))
                {
                    // De-serialise data from file
                    Stream stream = File.Open(sFile, FileMode.Open);
                    PersistentCalibrationData data = (PersistentCalibrationData)new BinaryFormatter().Deserialize(stream);
                    stream.Close();
                    return data;
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
        #endregion

        #region UI Events

        /// <summary>
        /// Called when the IP Address has been changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtIPAddress_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Flag that we want to reconnect.
            bReconnect = true;
            if (chkTUIOEnabled.IsChecked == true)
            {
                chkTUIOEnabled.IsChecked = false;
                App.TB.ShowBalloonTip("WiiTUIO", "Changes to the settings have been made.\nThe TUIO events have been disabled...", BalloonIcon.Info);
            }
        }

        /// <summary>
        /// Called when the port has been changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Flag that we want to reconnect.
            bReconnect = true;
            if (chkWin7Enabled.IsChecked == true)
            {
                chkWin7Enabled.IsChecked = false;
                App.TB.ShowBalloonTip("WiiTUIO", "Changes to the settings have been made.\nThe TUIO events have been disabled...", BalloonIcon.Info);
            }
        }

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
        /// Called when the TUIO checkbox is checked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkTUIOEnabled_Checked(object sender, RoutedEventArgs e)
        {
            // Acquire mutual exclusion.
            pCommunicationMutex.WaitOne();

            // Enable the TUIO touch and disconnect the provider.
            this.disconnectTransmitter();
            if (this.connectTransmitter())
            {
                bTUIOTouch = true;
                chkTUIOEnabled.IsChecked = true;
            }
            else
            {
                bTUIOTouch = false;
                chkTUIOEnabled.IsChecked = false;
            }

            // Release the mutex.
            pCommunicationMutex.ReleaseMutex();
        }

        /// <summary>
        /// Called when the TUIO checkbox is unchecked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkTUIOEnabled_Unchecked(object sender, RoutedEventArgs e)
        {
            // Acquire mutual exclusion.
            pCommunicationMutex.WaitOne();

            // Disable the TUIO touch and disconnect the provider.
            bTUIOTouch = false;
            this.disconnectTransmitter();

            // Release the mutex.
            pCommunicationMutex.ReleaseMutex();
        }

        /// <summary>
        /// Called when the Win7 checkbox is checked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkWin7Enabled_Checked(object sender, RoutedEventArgs e)
        {
            // Acquire mutual exclusion.
            pCommunicationMutex.WaitOne();

            // Enable the windows touch and disconnect the provider.
            this.disconnectWindowsTouch();
            if (this.connectWindowsTouch())
            {
                bWindowsTouch = true;
                chkWin7Enabled.IsChecked = true;
            }
            else
            {
                bWindowsTouch = false;
                chkWin7Enabled.IsChecked = false;
            }

            // Release the mutex.
            pCommunicationMutex.ReleaseMutex();
        }

        /// <summary>
        /// Called when the Win7 checkbox is unchecked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkWin7Enabled_Unchecked(object sender, RoutedEventArgs e)
        {
            // Acquire mutual exclusion.
            pCommunicationMutex.WaitOne();

            // Disable the windows touch and disconnect the provider.
            bWindowsTouch = false;
            this.disconnectWindowsTouch();

            // Release the mutex.
            pCommunicationMutex.ReleaseMutex();
        }

        /// <summary>
        /// Called when the question mark is clicked next to the TUIO touch events check box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAboutTUIO_Click(object sender, RoutedEventArgs e)
        {
            showMessage("TUIO is an open framework that defines a common protocol and API for tangible multitouch surfaces.\n\nThis program is capable of generating events compatible with this protocol.", MessageType.Info);
        }

        /// <summary>
        /// Called when the question mark is clicked next to the Win7 touch events check box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAboutWinTouch_Click(object sender, RoutedEventArgs e)
        {
            TextBlock pMessage = new TextBlock();
            pMessage.TextWrapping = TextWrapping.Wrap;
            pMessage.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            pMessage.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            pMessage.FontSize = 12.0;
            pMessage.FontWeight = FontWeights.Bold;

            pMessage.Inlines.Add("This application can communicate with Windows 7 via the UniSoftHID driver.  This allows it to emulate native muli-touch events in Windows 7.\n\nThe touch messages this application generates are a cut down version of the Multitouch.Driver.Logic namespace within MultiTouchVista.  Please don't ask them for support!\n\nThe UniSoftHID driver can be found bundled with 'MultiTouchVista' here: ");
            pMessage.Inlines.Add(createHyperlink("MultiTouchVista", "http://multitouchvista.codeplex.com/releases/view/28979"));

            showMessage(pMessage, MessageType.Info);
        }

        /// <summary>
        /// Called when the 'Connect' or 'Disconnect' button is clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {

            this.ConnectDisconnect();
            
        }

        public void ConnectDisconnect()
        {
            // If we are in reconnect mode..
            if (bReconnect)
            {
                disconnectProviders();
                btnConnect.Content = "Connect";
                btnCalibrate.IsEnabled = false;
                btnCalibrate.Content = "Calibrate";
                bConnected = false;
                bReconnect = false;
                barBattery.Value = 0;
            }

            // If we have been asked to connect.
            if (!bConnected)
            {
                // Connect.
                if (createProviders())
                {
                    // Update the button to say we are connected.
                    btnConnect.Content = "Disconnect";
                    bConnected = true;

                    if(this.pWiiProvider is WiiProvider)
                    {
                        btnCalibrate.IsEnabled = true;
                        // Load calibration data.
                        PersistentCalibrationData oData = loadPersistentCalibration("./Calibration.dat");
                        if (oData != null)
                        {
                            ((WiiProvider)this.pWiiProvider).setCalibrationData(oData.Source, oData.Destination, oData.ScreenSize);
                            btnCalibrate.Content = "Re-Calibrate";
                            App.TB.ShowBalloonTip("WiiTUIO", "Calibration loaded", BalloonIcon.Info);
                        }
                    }
                }
                else
                {
                    disconnectProviders();
                    btnConnect.Content = "Connect";
                    btnCalibrate.IsEnabled = false;
                    btnCalibrate.Content = "Calibrate";
                    bConnected = false;
                    barBattery.Value = 0;
                }
            }

            // Otherwise be sure I am disconnected.
            else
            {
                disconnectProviders();
                btnConnect.Content = "Connect";
                btnCalibrate.IsEnabled = false;
                btnCalibrate.Content = "Calibrate";
                bConnected = false;
                barBattery.Value = 0;
            }
        }

        /// <summary>
        /// Called when the calibrate button has been clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCalibrate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create a new calibration window.
                //if (pCalibrationWindow == null)
                //    this.pCalibrationWindow = new CalibrationWindow();
                //else
                this.pCalibrationWindow.Visibility = System.Windows.Visibility.Visible;

                //this.pCalibrationWindow.Topmost = true;
                this.pCalibrationWindow.WindowStyle = WindowStyle.None;
                this.pCalibrationWindow.WindowState = WindowState.Maximized;
                this.pCalibrationWindow.Show();

                // Event handler for the finish calibration.
                this.pCalibrationWindow.CalibrationCanvas.OnCalibrationFinished += new Action<WiiProvider.CalibrationRectangle, WiiProvider.CalibrationRectangle, Vector>(CalibrationCanvas_OnCalibrationFinished);

                // Begin the calibration.
                this.pCalibrationWindow.CalibrationCanvas.beginCalibration((WiiProvider)this.pWiiProvider);
            }
            catch (Exception pError)
            {
                MessageBox.Show(pError.Message, "WiiTUIO", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Called when the hide button is clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnHide_Click(object sender, RoutedEventArgs e)
        {
            //App.TB.TrayPopup.
        }

        /// <summary>
        /// Called when exit is clicked in the context menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            App.TB.Dispose();
            Application.Current.Shutdown(0);
        }

        /// <summary>
        /// Called when the 'About' button is clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAbout_Click(object sender, RoutedEventArgs e)
        {
            TextBlock pMessage = new TextBlock();
            pMessage.TextWrapping = TextWrapping.Wrap;
            pMessage.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            pMessage.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            pMessage.FontSize = 11.0;
            pMessage.FontWeight = FontWeights.Bold;

            pMessage.Inlines.Add("WiiTUIO is an application which stabilises the IR sources captured by a Wii Remote (Wiimote) and presents them as TUIO and Windows 7 Touch messages.\n\n");
            pMessage.Inlines.Add("WiiTUIO was written by John Hardy & Christopher Bull of the HighWire Programme at Lancaster University.\nYou can contact us at:\n  ");
            pMessage.Inlines.Add(createHyperlink("hardyj2@unix.lancs.ac.uk", "mailto:hardyj2@unix.lancs.ac.uk"));
            pMessage.Inlines.Add("\n  ");
            pMessage.Inlines.Add(createHyperlink("c.bull@lancaster.ac.uk", "mailto:c.bull@lancaster.ac.uk"));
            pMessage.Inlines.Add("\n\nCredits:\n  ");
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
        }

        #endregion

        private void ComboBox_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if (ModeComboBox.SelectedItem != null && ((ComboBoxItem)ModeComboBox.SelectedItem).Content != null)
            {
                ComboBoxItem cbItem = (ComboBoxItem)ModeComboBox.SelectedItem;
                if (cbItem.Content.ToString() == "Wii Sensor Bar")
                {
                    bConnected = true; //Hack so we wont do anything than disconnect.
                    this.ConnectDisconnect();
                    this.currentMode = Mode.POINTER;
                }
                else if (cbItem.Content.ToString() == "IR Pen")
                {
                    bConnected = true;
                    this.ConnectDisconnect();
                    this.currentMode = Mode.PEN;
                }
            }
        }

        
    }

    #region Persistent Calibration Data Serialisation Helper
    /// <summary>
    /// Wrapper class for persistent calibration data.
    /// This classes is required for saving data and is also returned from a load.
    /// </summary>
    [Serializable]
    public class PersistentCalibrationData
    {
        /// <summary> The source calibration rectangle. </summary>
        public WiiProvider.CalibrationRectangle Source { get; private set; }
        /// <summary> The destination calibration rectangle. </summary>
        public WiiProvider.CalibrationRectangle Destination { get; private set; }
        /// <summary> The screen size. </summary>
        public Vector ScreenSize { get; private set; }
        /// <summary> When the calibration data swas created. </summary>
        public DateTime TimeStamp { get; private set; }

        /// <summary>
        /// Creates a wrapper object for persisting calibration data.
        /// </summary>
        /// <param name="pSource">The source calibration rectangle</param>
        /// <param name="pDestination">The destination calibration rectangle</param>
        /// <param name="vScreenSize">The screen size</param>
        public PersistentCalibrationData(WiiProvider.CalibrationRectangle pSource, WiiProvider.CalibrationRectangle pDestination, Vector vScreenSize)
        {
            this.Source = pSource;
            this.Destination = pDestination;
            this.ScreenSize = vScreenSize;
            this.TimeStamp = DateTime.Now;
        }
    }
    #endregion
}
