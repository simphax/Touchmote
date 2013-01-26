using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WiiTUIO.Provider
{

    

    /// <summary>
    /// Interaction logic for WiiProviderSettings.xaml
    /// </summary>
    public partial class WiiProviderSettings : UserControl
    {


        /// <summary>
        /// A refrence to the calibration window.
        /// </summary>
        public CalibrationWindow pCalibrationWindow = null;

        private WiiProvider pWiiProvider;

        public WiiProviderSettings(WiiProvider pWiiProvider)
        {

            InitializeComponent();

            this.pWiiProvider = pWiiProvider;

            // Create a calibration window and hide it.
            this.pCalibrationWindow = new CalibrationWindow();
            this.pCalibrationWindow.Visibility = Visibility.Hidden;

            this.pWiiProvider.OnNewFrame += new EventHandler<FrameEventArgs>(pWiiProvider_OnNewFrame);
            this.pWiiProvider.OnConnect += new Action<int>(pWiiProvider_OnConnect);
            this.pWiiProvider.OnDisconnect += new Action<int>(pWiiProvider_OnDisconnect);
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
                this.pCalibrationWindow.CalibrationCanvas.beginCalibration(this.pWiiProvider);
            }
            catch (Exception pError)
            {
                MessageBox.Show(pError.Message, "Touchmote", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                MessageBox.Show("Failed to save calibration data", "Touchmote", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Close the calibration window.
            if (pCalibrationWindow != null)
            {
                pCalibrationWindow.Visibility = System.Windows.Visibility.Hidden;
            }
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
                if (this.pWiiProvider is WiiProvider)
                {
                    btnCalibrate.IsEnabled = true;
                    // Load calibration data.
                    PersistentCalibrationData oData = loadPersistentCalibration("./Calibration.dat");
                    if (oData != null)
                    {
                        ((WiiProvider)this.pWiiProvider).setCalibrationData(oData.Source, oData.Destination, oData.ScreenSize);
                        //btnCalibrate.Content = "Re-Calibrate";
                        App.TB.ShowBalloonTip("Touchmote", "Calibration loaded", BalloonIcon.Info);
                    }
                }
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
                btnCalibrate.IsEnabled = false;
                //btnCalibrate.Content = "Calibrate";
            }), null);
        }


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

        
        /// <summary>
        /// This is called when the WiiProvider has a new set of input to send.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pWiiProvider_OnNewFrame(object sender, FrameEventArgs e)
        {
            // Are we calibrating.

                    return;
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
}
