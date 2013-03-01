using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using WiimoteLib;
using System.Runtime.InteropServices;
using System.Drawing;
using WindowsInput;
using WiiTUIO.Properties;
using System.Windows.Controls;

namespace WiiTUIO.Provider
{
    /// <summary>
    /// The WiiProvider implements <see cref="IProvider"/> in order to offer a type of object which uses the Wiimote to generate new event frames.
    /// </summary>
    public class MultiWiiPointerProvider : IProvider
    {
        Dictionary<Guid, WiimoteControl> pWiimoteMap = new Dictionary<Guid, WiimoteControl>();

        private WiimoteCollection pWC;

        private UserControl settingsControl = null;

        private bool changeSystemCursor = false;

        #region Properties and Constructor
        /// <summary>
        /// Boolean which indicates if we are generating input or not.
        /// </summary>
        private bool bRunning = false;

        /// <summary>
        /// A queue for the frame of events.
        /// </summary>
        //private Queue<WiiContact> lFrame = new Queue<WiiContact>(1);

        /// <summary>
        /// An input classifier which we will use to organise points.
        /// </summary>
        public SpatioTemporalClassifier InputClassifier { get; protected set; }



        /// <summary>
        /// A property to determine if this input provider is running (and thus generating events).
        /// </summary>
        public bool IsRunning { get { return this.bRunning; } }

        /// <summary>
        /// Do we want to use the calibration transformation step when generating input.
        /// </summary>
        public bool TransformResults { get; set; }

        /// <summary>
        /// This defines an event which is raised when a new frame of touch events is prepared and ready to be dispatched by this provider.
        /// </summary>
        public event EventHandler<FrameEventArgs> OnNewFrame;

        #region Battery State
        /// <summary>
        /// An event which is fired when the battery state changes.
        /// </summary>
        public event Action<int> OnBatteryUpdate;


        public event Action<int> OnConnect;
        public event Action<int> OnDisconnect;

        /// <summary>
        /// The internal battery state.
        /// </summary>
        private int iBatteryState = 0;

        /// <summary>
        /// Get the current battery state.
        /// </summary>
        public int BatteryState
        {
            get
            {
                return iBatteryState;
            }
            protected set
            {
                if (value != iBatteryState)
                {
                    iBatteryState = value;
                    if (OnBatteryUpdate != null)
                        OnBatteryUpdate(iBatteryState);
                }
            }
        }
        #endregion

        /// <summary>
        /// Construct a new wiimote provider.
        /// </summary>
        public MultiWiiPointerProvider()
        {

            

            Settings.Default.SettingChanging += SettingChanging;

            this.settingsControl = new WiiPointerProviderSettings();

            /*
            this.mouseMode = this.keyMapper.KeyMap.Pointer.ToLower() == "mouse";
            this.showPointer = Settings.Default.pointer_moveCursor;
            if (this.showPointer && !this.mouseMode)
            {
                this.duoTouch.enableHover();
            }
            else
            {
                this.duoTouch.disableHover();
            }
            */
        }

       


        private void SettingChanging(object sender, System.Configuration.SettingChangingEventArgs e)
        {
           
        }
        #endregion

        #region Start and Stop

        /// <summary>
        /// Instructs this input provider to begin generating events.
        /// </summary>
        public void start()
        {
            // Ensure we cannot process any events.
            //pDeviceMutex.WaitOne();

            // Create a new reference to a wiimote device.
            Exception pError = null;
            if (!this.initialiseWiimoteConnection(out pError))
            {
                //pDeviceMutex.ReleaseMutex();
                throw new Exception("Could not establish connection to Wiimote: " + pError.Message, pError);
            }


            // Set the running flag.
            this.bRunning = true;

            /*
            this.changeSystemCursor = Settings.Default.pointer_changeSystemCursor;

            if (this.changeSystemCursor)
            {
                try
                {
                    MouseSimulator.SetSystemCursor(cursor);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
            */

            OnConnect(1);

            // Release processing.
            //pDeviceMutex.ReleaseMutex();
        }

        /// <summary>
        /// Instructs this input provider to stop generating events.
        /// </summary>
        public void stop()
        {
            // Set the running flag.
            this.bRunning = false;
            
            this.teardownWiimoteConnection();

            OnDisconnect(1);
        }
        #endregion

        #region Connection creation and teardown.
        /// <summary>  
        /// This method creates and sets up our connection to our class-gloal Wiimote device.
        /// This destroys any existing connection before creating a new one.
        /// </summary>
        /// <param name="pErrorReport">A reference to an exception which we want to contain our error if one happened.</param>
        private bool initialiseWiimoteConnection(out Exception pErrorReport)
        {
            // If we have an existing device, teardown the connection.
            this.teardownWiimoteConnection();

            pWC = new WiimoteCollection();
            int index = 1;

            pWC.FindAllWiimotes();

            foreach (Wiimote pDevice in pWC)
            {
                try
                {
                    // Hook up device event handlers.
                    pDevice.WiimoteChanged += new EventHandler<WiimoteChangedEventArgs>(handleWiimoteChanged);
                    pDevice.WiimoteExtensionChanged += new EventHandler<WiimoteExtensionChangedEventArgs>(handleWiimoteExtensionChanged);

                    // Try to establish a connection, enable the IR reader and flag some LEDs.
                    pDevice.Connect();
                    pDevice.SetReportType(InputReport.IRAccel, true);
                    //pDevice.SetRumble(true);
                    pDevice.SetLEDs(index);

                    WiimoteControl control = new WiimoteControl(index);

                    pWiimoteMap[pDevice.ID] = control;

                    index++;
                }

                // If something went wrong - notify the user..
                catch (Exception pError)
                {

                    // Ensure we are ok.
                    try
                    {
                        this.teardownWiimoteConnection();
                    }
                    finally { }
                    // Say we screwed up.
                    pErrorReport = pError;
                    //throw new Exception("Error establishing connection: " + , pError);
                    return false;
                }
            }
            new Timer(stopRumble, null, 80, 0); //Stop the rumble after 80ms
            pErrorReport = null;
            return true;
        }

        /// <summary>
        /// This method destroys our connection to our class-global Wiimote device.
        /// </summary>
        private void teardownWiimoteConnection()
        {
            if (pWC != null)
            {
                foreach (Wiimote pDevice in pWC)
                {
                    //this.pDevice.SetLEDs(false, false, false, false);
                    pDevice.SetRumble(false);

                    // Close the connection and dispose of the device.
                    pDevice.Disconnect();
                    pDevice.Dispose();
                }
            }
        }
        #endregion

        private void stopRumble(Object nothing)
        {
            foreach (Wiimote pDevice in pWC)
            {
                bool rumbleStatus = true;
                while (rumbleStatus) //Sometimes the Wiimote does not disable the rumble on the first try
                {
                    try {
                            pDevice.SetRumble(false);
                            System.Threading.Thread.Sleep(30);
                            rumbleStatus = pDevice.WiimoteState.Rumble;
                    
                    } catch(Exception e) {}
                }
            }
        }

        #region Wiimote Event Handlers
        /// <summary>
        /// This is called when an extension is attached or unplugged.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void handleWiimoteExtensionChanged(object sender, WiimoteExtensionChangedEventArgs e)
        {
            // Check we have a valid device.
            //if (this.pDevice == null)
            //    return;
            /*
            // If an extension is attached at runtime we want to enable it.
            if (e.Inserted)
                this.pDevice.SetReportType(InputReport.IRExtensionAccel, true);
            else
                this.pDevice.SetReportType(InputReport.IRAccel, true);
             * */
        }

        /// <summary>
        /// This is called when the state of the wiimote changes and a new state report is available.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void handleWiimoteChanged(object sender, WiimoteChangedEventArgs e)
        {
            if (bRunning)
            {
                WiimoteControl senderControl = pWiimoteMap[((Wiimote)sender).ID];


                senderControl.handleWiimoteChanged(sender, e);

                senderControl.Handled = true;

                bool handledAll = false;
                Queue<WiiContact> allContacts = new Queue<WiiContact>(2);
                ulong timestamp = 0;

                foreach (WiimoteControl control in pWiimoteMap.Values)
                {
                    handledAll = control.Handled;
                    if (handledAll == false)
                    {
                        break;
                    }
                }

                if (handledAll)
                {
                    foreach (WiimoteControl control in pWiimoteMap.Values)
                    {
                        if (control.FrameQueue.Count > 0)
                        {
                            FrameEventArgs lastFrameEvent = control.FrameQueue.Dequeue();
                            while (control.FrameQueue.Count > 0)
                            {
                                lastFrameEvent = control.FrameQueue.Dequeue();
                            }

                            timestamp = timestamp < lastFrameEvent.Timestamp ? timestamp : lastFrameEvent.Timestamp;

                            foreach (WiiContact contact in lastFrameEvent.Contacts)
                            {
                                allContacts.Enqueue(contact);
                            }
                        }
                        control.Handled = false;
                    }
                    if(allContacts.Count > 0) {
                        FrameEventArgs newFrame = new FrameEventArgs(timestamp, allContacts);

                        this.OnNewFrame(this, newFrame);
                    }
                }
            }
        }
        #endregion

        public UserControl getSettingsControl()
        {
            return this.settingsControl;
        }
    }
}
