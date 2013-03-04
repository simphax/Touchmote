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
        private double WIIMOTE_DISCONNECT_THRESHOLD = 2000; //If we haven't recieved input from a wiimote in 2 seconds we consider it disconnected.
        private ulong OLD_FRAME_THRESHOLD = 200;

        private Mutex pDeviceMutex = new Mutex();

        private Thread wiimoteConnectorThread;

        private Dictionary<string, WiimoteControl> pWiimoteMap = new Dictionary<string, WiimoteControl>();

        private int nextWiimoteIndex = 1;

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


        public event Action<int,int> OnConnect;
        public event Action<int,int> OnDisconnect;

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

            this.settingsControl = new WiiPointerProviderSettings();

            this.pWC = new WiimoteCollection();

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

        #endregion

        #region Start and Stop

        /// <summary>
        /// Instructs this input provider to begin generating events.
        /// </summary>
        public void start()
        {
            // Ensure we cannot process any events.
            //pDeviceMutex.WaitOne();

            this.pWC.FindAllWiimotes();

            wiimoteConnectorThread = new Thread(new ThreadStart(wiimoteConnectorThreadWorker));
            wiimoteConnectorThread.Start();

            // Create a new reference to a wiimote device.
            /*
            Exception pError = null;
            
            if (!this.initialiseWiimoteConnection(out pError))
            {
                //pDeviceMutex.ReleaseMutex();
                throw new Exception("Could not establish connection to Wiimote: " + pError.Message, pError);
            }
            */

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


            // Release processing.
            //pDeviceMutex.ReleaseMutex();
        }

        private void wiimoteConnectorThreadWorker()
        {
            Exception pError;
            while (this.bRunning)
            {
                if (!this.initialiseWiimoteConnections(out pError))
                {
                    Console.WriteLine("Could not establish connection to a Wiimote: " + pError.Message, pError);
                }
                Thread.Sleep(2000);
            }
        }

        /// <summary>
        /// Instructs this input provider to stop generating events.
        /// </summary>
        public void stop()
        {
            // Set the running flag.
            this.bRunning = false;

            if (this.wiimoteConnectorThread != null)
            {
                this.wiimoteConnectorThread.Abort();
            }
            
            this.teardownWiimoteConnections();

            this.pWC.Clear();

            this.nextWiimoteIndex = 1;

        }
        #endregion

        #region Connection creation and teardown.
        /// <summary>  
        /// This method creates and sets up our connection to our class-gloal Wiimote device.
        /// This destroys any existing connection before creating a new one.
        /// </summary>
        /// <param name="pErrorReport">A reference to an exception which we want to contain our error if one happened.</param>
        private bool initialiseWiimoteConnections(out Exception pErrorReport)
        {
            // If we have an existing device, teardown the connection.
            //this.teardownWiimoteConnection();

            pDeviceMutex.WaitOne();
            pErrorReport = null;

            this.pWC.Clear();
            this.pWC.FindAllWiimotes();

            foreach (Wiimote pDevice in pWC)
            {
                try
                {
                    if (!pWiimoteMap.Keys.Contains(pDevice.HIDDevicePath))
                    {
                        // Try to establish a connection, enable the IR reader and flag some LEDs.
                        pDevice.Connect();
                        pDevice.SetReportType(InputReport.IRAccel, true);
                        pDevice.SetRumble(true);
                        new Timer(stopRumble, pDevice, 80, 0); //Stop the rumble after 80ms
                        pDevice.SetLEDs((this.nextWiimoteIndex - 1) % 4 + 1);

                        WiimoteControl control = new WiimoteControl(this.nextWiimoteIndex);

                        pWiimoteMap[pDevice.HIDDevicePath] = control;

                        // Hook up device event handlers.
                        pDevice.WiimoteChanged += new EventHandler<WiimoteChangedEventArgs>(handleWiimoteChanged);
                        pDevice.WiimoteExtensionChanged += new EventHandler<WiimoteExtensionChangedEventArgs>(handleWiimoteExtensionChanged);

                        OnConnect(this.nextWiimoteIndex, this.pWiimoteMap.Count);

                        this.nextWiimoteIndex = this.pWiimoteMap.Count + 1;
                        
                    }
                    else if (DateTime.Now.Subtract(pWiimoteMap[pDevice.HIDDevicePath].LastWiimoteEventTime).TotalMilliseconds > WIIMOTE_DISCONNECT_THRESHOLD)
                    {
                        teardownWiimoteConnection(pDevice);
                    }
                }
                // If something went wrong - notify the user..
                catch (Exception pError)
                {

                    // Ensure we are ok.
                    try
                    {
                        this.teardownWiimoteConnection(pDevice);
                    }
                    finally { }
                    // Say we screwed up.
                    pErrorReport = pError;
                    //throw new Exception("Error establishing connection: " + , pError);
                    
                }
                
            }
            if(pErrorReport != null)
            {
                pDeviceMutex.ReleaseMutex();
                return false;
            }
            pDeviceMutex.ReleaseMutex();
            return true;
        }

        /// <summary>
        /// This method destroys our connection to our class-global Wiimote device.
        /// </summary>
        private void teardownWiimoteConnections()
        {
            if (pWC != null)
            {
                foreach (Wiimote pDevice in pWC)
                {
                    teardownWiimoteConnection(pDevice);
                }
            }
        }

        private void teardownWiimoteConnection(Wiimote pDevice)
        {
            if (pDevice != null)
            {
                pDeviceMutex.WaitOne();
                int wiimoteid;
                if (pWiimoteMap.Keys.Contains(pDevice.HIDDevicePath))
                {
                    wiimoteid = this.pWiimoteMap[pDevice.HIDDevicePath].ID;
                    this.pWiimoteMap.Remove(pDevice.HIDDevicePath);
                }
                else
                {
                    wiimoteid = this.pWiimoteMap.Count + 1;
                }
                this.nextWiimoteIndex = this.pWiimoteMap.Count + 1;

                pDevice.SetRumble(false);

                // Close the connection and dispose of the device.
                pDevice.Disconnect();
                pDevice.Dispose();
                pDeviceMutex.ReleaseMutex();

                OnDisconnect(wiimoteid, this.pWiimoteMap.Count);
            }
        }
        #endregion

        private void stopRumble(Object device)
        {
            Wiimote pDevice = (Wiimote)device;
            bool rumbleStatus = true;
            while (rumbleStatus) //Sometimes the Wiimote does not disable the rumble on the first try
            {
                try {
                    if (pDevice != null)
                    {
                        pDevice.SetRumble(false);
                        System.Threading.Thread.Sleep(30);
                        rumbleStatus = pDevice.WiimoteState.Rumble;
                    }
                    else
                    {
                        rumbleStatus = false;
                    }
                } catch {}
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
                pDeviceMutex.WaitOne();
                if (pWiimoteMap.Keys.Contains(((Wiimote)sender).HIDDevicePath))
                {
                    WiimoteControl senderControl = pWiimoteMap[((Wiimote)sender).HIDDevicePath];

                    senderControl.handleWiimoteChanged(sender, e);

                    senderControl.Handled = true;

                    if (senderControl.FrameQueue.Count > 0)
                    {
                        FrameEventArgs senderFrame = senderControl.FrameQueue.Dequeue();

                        Queue<WiiContact> allContacts = new Queue<WiiContact>(senderFrame.Contacts);
                        
                        foreach (WiimoteControl control in pWiimoteMap.Values) //Include the contacts for all Wiimotes, only send hover and move events for their contacts, using the last sent contact.
                        {
                            if (control != senderControl)
                            {
                                FrameEventArgs lastFrame = control.LastFrameEvent;
                                if (lastFrame != null)
                                {
                                    ulong timeDelta = ((ulong)Stopwatch.GetTimestamp() / 10000) - (lastFrame.Timestamp / 10000);
                                    if (timeDelta < OLD_FRAME_THRESHOLD) //Happens when the pointer is out of reach
                                    {
                                        IEnumerable<WiiContact> contacts = lastFrame.Contacts;
                                        foreach (WiiContact contact in contacts)
                                        {
                                            if (contact.Type == ContactType.EndToHover)
                                            {
                                                //Console.WriteLine("Convert entohover" + contact.ID);
                                                WiiContact newContact = new WiiContact(contact.ID, ContactType.Hover, contact.Position, new Vector(Util.ScreenWidth, Util.ScreenHeight));
                                                allContacts.Enqueue(newContact);
                                            }
                                            else if (contact.Type == ContactType.Start)
                                            {
                                                //Console.WriteLine("Convert start" + contact.ID);
                                                WiiContact newContact = new WiiContact(contact.ID, ContactType.Move, contact.Position, new Vector(Util.ScreenWidth, Util.ScreenHeight));
                                                allContacts.Enqueue(newContact);
                                            }
                                            else if (contact.Type == ContactType.End || contact.Type == ContactType.EndFromHover)
                                            {
                                            }
                                            else //contact type was hover or move
                                            {
                                                //Console.WriteLine("Add hover or move" + contact.ID);
                                                allContacts.Enqueue(contact);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        //Console.WriteLine("Sending " + allContacts.Count + " contacts");
                        FrameEventArgs newFrame = new FrameEventArgs(senderFrame.Timestamp, allContacts);

                        this.OnNewFrame(this, newFrame);
                    }
                    
                    /*
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

                                    //Fast forward events which are only consisting of Move or Hover events
                                    bool importantContact = false;
                                    foreach (WiiContact contact in lastFrameEvent.Contacts)
                                    {
                                        if (contact.Type != ContactType.Hover && contact.Type != ContactType.Move)
                                        {
                                            importantContact = true;
                                            break;
                                        }
                                    }

                                    //If the next event contains an End event this one is important too, because End events need to be at the same position as the last one.
                                    FrameEventArgs nextFrameEvent = control.FrameQueue.Peek();
                                    foreach (WiiContact contact in nextFrameEvent.Contacts)
                                    {
                                        if (contact.Type == ContactType.End || contact.Type == ContactType.EndFromHover || contact.Type == ContactType.EndToHover)
                                        {
                                            importantContact = true;
                                            break;
                                        }
                                    }

                                    if (importantContact)
                                    {
                                        break;
                                    }

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
                        if (allContacts.Count > 0)
                        {
                            FrameEventArgs newFrame = new FrameEventArgs(timestamp, allContacts);

                            this.OnNewFrame(this, newFrame);
                        }
                    }
                 * */
                }
                pDeviceMutex.ReleaseMutex();
            }
        }
        #endregion

        public UserControl getSettingsControl()
        {
            return this.settingsControl;
        }
    }
}
