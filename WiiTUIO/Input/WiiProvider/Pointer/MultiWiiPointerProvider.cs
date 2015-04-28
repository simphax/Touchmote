using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows;
using WiimoteLib;
using System.Runtime.InteropServices;
using System.Drawing;
using WindowsInput;
using WiiTUIO.Properties;
using System.Windows.Controls;
using System.Threading;
using WiiTUIO.Output.Handlers.Touch;
using WiiTUIO.Output;

namespace WiiTUIO.Provider
{
    /// <summary>
    /// The WiiProvider implements <see cref="IProvider"/> in order to offer a type of object which uses the Wiimote to generate new event frames.
    /// </summary>
    public class MultiWiiPointerProvider : IProvider
    {
        private int WIIMOTE_POWER_SAVE_DISCONNECT_TIMEOUT = 15000;
        private int POWER_SAVE_STATUS_INTERVAL = 6000;
 
        private int WIIMOTE_DISCONNECT_TIMEOUT = 2000; //If we haven't recieved input from a wiimote in 2 seconds we consider it disconnected.
        private int WIIMOTE_SIGNIFICANT_DISCONNECT_TIMEOUT = Settings.Default.autoDisconnectTimeout; //If we haven't recieved significant input from a wiimote in 60 seconds we will put it to sleep
        private ulong OLD_FRAME_TIMEOUT = 200; //Timeout for a previous frame from a Wiimote to be considered old, so we wont enable it when getting input from other wiimotes.
        private int CONNECTION_THREAD_SLEEP = 2000;
        private int POWER_SAVE_BLINK_DELAY = 10000;
        private int CONNECT_RUMBLE_TIME = 100;

        private int cursorUpdateToggle = 0;

        private int blinkWait = 0;
        private int statusWait = 0;

        private Mutex pDeviceMutex = new Mutex();
        private Mutex connectionMutex = new Mutex();

        private Timer wiimoteConnectorTimer;
        private Thread wiimoteHandlerThread;

        private Dictionary<string, WiimoteControl> pWiimoteMap = new Dictionary<string, WiimoteControl>();

        private Dictionary<string, WiimoteChangedEventArgs> eventBuffer = new Dictionary<string, WiimoteChangedEventArgs>();

        private WiimoteCollection pWC;

        private bool readyToRender = false;

        private EventHandler<WiimoteChangedEventArgs> wiimoteChangedEventHandler;
        private EventHandler<WiimoteExtensionChangedEventArgs> wiimoteExtensionChangedEventHandler;

        #region Properties and Constructor
        /// <summary>
        /// Boolean which indicates if we are generating input or not.
        /// </summary>
        private bool bRunning = false;

        /// <summary>
        /// An input classifier which we will use to organise points.
        /// </summary>
        public SpatioTemporalClassifier InputClassifier { get; protected set; }

        /// <summary>
        /// A property to determine if this input provider is running (and thus generating events).
        /// </summary>
        public bool IsRunning { get { return this.bRunning; } }

        /// <summary>
        /// This defines an event which is raised when a new frame of touch events is prepared and ready to be dispatched by this provider.
        /// </summary>
        public event EventHandler<FrameEventArgs> OnNewFrame;

        #region Battery State
        /// <summary>
        /// An event which is fired when the battery state changes.
        /// </summary>
        public event Action<WiimoteStatus> OnStatusUpdate;

        public event Action<int,int> OnConnect;
        public event Action<int,int> OnDisconnect;

   
        #endregion

        /// <summary>
        /// Construct a new wiimote provider.
        /// </summary>
        public MultiWiiPointerProvider()
        {

            this.pWC = new WiimoteCollection();

            this.wiimoteChangedEventHandler = new EventHandler<WiimoteChangedEventArgs>(handleWiimoteChanged);
            this.wiimoteExtensionChangedEventHandler = new EventHandler<WiimoteExtensionChangedEventArgs>(handleWiimoteExtensionChanged);

            wiimoteConnectorTimer = new Timer(wiimoteConnectorTimer_Elapsed, null, Timeout.Infinite, CONNECTION_THREAD_SLEEP);

            wiimoteHandlerThread = new Thread(WiimoteHandlerWorker);
            wiimoteHandlerThread.Priority = ThreadPriority.Highest;
            wiimoteHandlerThread.IsBackground = true;
            wiimoteHandlerThread.Start();

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
            Console.WriteLine("Start");
            TouchOutputFactory.getCurrentProviderHandler().connect();
            this.bRunning = true;
            wiimoteConnectorTimer.Change(0, Timeout.Infinite);
        }

        bool waitingToConnect = false;

        void wiimoteConnectorTimer_Elapsed(object sender)
        {
            wiimoteConnectorTimer.Change(Timeout.Infinite, Timeout.Infinite);
            Console.WriteLine("wiimoteConnectorTimer_Elapsed");
            if (this.bRunning)
            {
                Exception pError;
                if (!this.initialiseWiimoteConnections(out pError))
                {
                    Console.WriteLine("Could not establish connection to a Wiimote: " + pError.Message, pError);
                }
                wiimoteConnectorTimer.Change(CONNECTION_THREAD_SLEEP, Timeout.Infinite);
            }
        }

        /// <summary>
        /// Instructs this input provider to stop generating events.
        /// </summary>
        public void stop()
        {
            Console.WriteLine("stop");
            // Set the running flag.
            this.bRunning = false;

            this.wiimoteConnectorTimer.Change(Timeout.Infinite, Timeout.Infinite);

            this.teardownWiimoteConnections();
            if (Settings.Default.completelyDisconnect)
            {
                this.completelyDisconnectAll();
            }

            this.pWC.Clear();

            TouchOutputFactory.getCurrentProviderHandler().disconnect();

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
            this.connectionMutex.WaitOne();
            // If we have an existing device, teardown the connection.
            //this.teardownWiimoteConnection();

            pErrorReport = null;

            try
            {
                Dictionary<string, WiimoteControl> copy = new Dictionary<string, WiimoteControl>(pWiimoteMap);
                foreach (WiimoteControl control in copy.Values)
                {
                    Wiimote pDevice = control.Wiimote;
                    try
                    {
                        if (!control.Status.InPowerSave
                            && control.LastWiimoteEventTime != null
                            && DateTime.Now.Subtract(control.LastWiimoteEventTime).TotalMilliseconds > WIIMOTE_DISCONNECT_TIMEOUT)
                        {
                            Console.WriteLine("Teardown 1 " + pDevice.HIDDevicePath + " because of timeout with delta " + DateTime.Now.Subtract(pWiimoteMap[pDevice.HIDDevicePath].LastWiimoteEventTime).TotalMilliseconds);
                            teardownWiimoteConnection(control.Wiimote);
                        }
                        else if (!control.Status.InPowerSave
                            && control.LastSignificantWiimoteEventTime != null
                            && DateTime.Now.Subtract(control.LastSignificantWiimoteEventTime).TotalMilliseconds > WIIMOTE_SIGNIFICANT_DISCONNECT_TIMEOUT)
                        {
                            Console.WriteLine("Put " + pDevice.HIDDevicePath + " to power saver mode because of timeout with delta " + DateTime.Now.Subtract(control.LastSignificantWiimoteEventTime).TotalMilliseconds);
                            //teardownWiimoteConnection(pWiimoteMap[pDevice.HIDDevicePath].Wiimote);
                            putToPowerSave(control);
                        }
                        else if (control.Status.InPowerSave
                        && control.LastWiimoteEventTime != null
                        && DateTime.Now.Subtract(control.LastWiimoteEventTime).TotalMilliseconds > WIIMOTE_POWER_SAVE_DISCONNECT_TIMEOUT)
                        {
                            Console.WriteLine("Teardown 2 " + pDevice.HIDDevicePath + " because of timeout with delta " + DateTime.Now.Subtract(control.LastWiimoteEventTime).TotalMilliseconds);
                            teardownWiimoteConnection(control.Wiimote);
                        }
                        else if (control.Status.InPowerSave)
                        {
                            

                            if (CONNECTION_THREAD_SLEEP * statusWait >= POWER_SAVE_STATUS_INTERVAL)
                            {
                                statusWait = 0;
                                control.WiimoteMutex.WaitOne();
                                try
                                {
                                    control.Wiimote.GetStatus();
                                }
                                catch { }
                                control.WiimoteMutex.ReleaseMutex();
                            }
                            else
                            {
                                statusWait++;
                            }

                            if (CONNECTION_THREAD_SLEEP * blinkWait >= POWER_SAVE_BLINK_DELAY)
                            {
                                blinkWait = 0;
                                control.Wiimote.SetLEDs(true, true, true, true);
                                Thread.Sleep(100);
                                control.Wiimote.SetLEDs(false, false, false, false);
                            }
                            else
                            {
                                blinkWait++;
                            }
                        }
                    }
                    catch (Exception pError)
                    {
                        try
                        {
                            Console.WriteLine("Teardown 3 " + pDevice.HIDDevicePath + " because of " + pError.Message);
                            this.teardownWiimoteConnection(pDevice);
                        }
                        finally
                        {
                        }
                        pErrorReport = pError;
                    }
                }

                this.pWC.Clear();
                this.pWC.FindAllWiimotes();
                
                foreach (Wiimote pDevice in pWC)
                {
                    try
                    {
                        if (!pWiimoteMap.Keys.Contains(pDevice.HIDDevicePath))
                        {
                            this.connectWiimote(pDevice);
                        }
                    }
                    // If something went wrong - notify the user..
                    catch (Exception pError)
                    {
                        // Ensure we are ok.
                        try
                        {
                            Console.WriteLine("Teardown 4 "+ pDevice.HIDDevicePath +" because of " + pError.Message);
                            this.teardownWiimoteConnection(pDevice);
                        }
                        finally { }
                        // Say we screwed up.
                        pErrorReport = pError;
                        //throw new Exception("Error establishing connection: " + , pError);
                    
                    }
                
                }
            }
            catch (Exception e)
            {
                pErrorReport = e;
            }

            this.connectionMutex.ReleaseMutex();

            if(pErrorReport != null)
            {
                return false;
            }
            return true;
        }

        private void connectWiimote(Wiimote wiimote)
        {
            Console.WriteLine("Trying to connect " + wiimote.HIDDevicePath);
            // Try to establish a connection, enable the IR reader and flag some LEDs.
            wiimote.Connect();
            wiimote.SetReportType(InputReport.IRExtensionAccel, IRSensitivity.Maximum, true);

            new Timer(new TimerCallback(connectRumble), wiimote, 0, Timeout.Infinite);

            int id = this.getFirstFreeId();
            wiimote.SetLEDs(id == 1, id == 2, id == 3, id == 4);

            WiimoteControl control = new WiimoteControl(id, wiimote);

            pDeviceMutex.WaitOne(); //Don't mess with the list of wiimotes if it is enumerating in an update
            pWiimoteMap[wiimote.HIDDevicePath] = control;
            pDeviceMutex.ReleaseMutex();

            // Hook up device event handlers.
            wiimote.WiimoteChanged += this.wiimoteChangedEventHandler;
            wiimote.WiimoteExtensionChanged += this.wiimoteExtensionChangedEventHandler;

            OnConnect(id, this.pWiimoteMap.Count);

        }

        

        private int getFirstFreeId()
        {
            HashSet<int> usedIDs = new HashSet<int>();
            foreach (WiimoteControl control in pWiimoteMap.Values)
            {
                usedIDs.Add(control.Status.ID);
            }

            int id = 1;
            while (usedIDs.Contains(id))
            {
                id++;
            }
            return id;
        }

        private void putToPowerSave(WiimoteControl control)
        {
            if (Settings.Default.completelyDisconnect && this.pWiimoteMap.Count == 1) //If we want to completely disable the device
            {
                teardownWiimoteConnection(control.Wiimote);
                completelyDisconnectAll();
            }
            else
            {
                control.WiimoteMutex.WaitOne();
                try
                {
                    control.Wiimote.SetReportType(InputReport.Buttons, false);
                    control.Status.InPowerSave = true;
                    control.Wiimote.SetLEDs(false, false, false, false);
                    control.Wiimote.SetRumble(false);
                }
                catch { }
                finally
                {
                    control.WiimoteMutex.ReleaseMutex();
                }
            }
        }

        private void wakeFromPowerSave(WiimoteControl control)
        {
            control.WiimoteMutex.WaitOne();
            try
            {
                control.Wiimote.SetReportType(InputReport.IRExtensionAccel, IRSensitivity.Maximum, true);
                control.Status.InPowerSave = false;
                int id = control.Status.ID;
                control.Wiimote.SetLEDs(id == 1, id == 2, id == 3, id == 4);
                control.Wiimote.SetRumble(true);
                new Timer(connectRumble,control.Wiimote,0,Timeout.Infinite);
            }
            catch { }
            finally
            {
                control.WiimoteMutex.ReleaseMutex();
            }
        }

        private void completelyDisconnectAll()
        {
            Launcher.Launch("Driver", "devcon", " disable \"BTHENUM*_VID*57e*_PID&0306*\"", new Action(delegate()
            {
                Launcher.Launch("Driver", "devcon", " enable \"BTHENUM*_VID*57e*_PID&0306*\"", null);
            }));
        }

        /// <summary>
        /// This method destroys our connection to our class-global Wiimote device.
        /// </summary>
        private void teardownWiimoteConnections()
        {
            if (pWiimoteMap.Count > 0)
            {
                IEnumerable<WiimoteControl> controls = new Queue<WiimoteControl>(pWiimoteMap.Values);
                foreach (WiimoteControl control in controls)
                {
                    teardownWiimoteConnection(control.Wiimote);
                }
            }
            else
            {
                OnDisconnect(0, 0);
            }
        }

        private void teardownWiimoteConnection(Wiimote pDevice)
        {
            if (pDevice != null)
            {
                pDeviceMutex.WaitOne();
                pDevice.WiimoteChanged -= this.wiimoteChangedEventHandler;
                pDevice.WiimoteExtensionChanged -= this.wiimoteExtensionChangedEventHandler;
                int wiimoteid;
                if (pWiimoteMap.Keys.Contains(pDevice.HIDDevicePath))
                {
                    wiimoteid = this.pWiimoteMap[pDevice.HIDDevicePath].Status.ID;
                    this.pWiimoteMap[pDevice.HIDDevicePath].Teardown();
                    this.pWiimoteMap.Remove(pDevice.HIDDevicePath);
                }
                else
                {
                    wiimoteid = this.pWiimoteMap.Count + 1;
                }
                pDeviceMutex.ReleaseMutex();

                pDevice.SetReportType(InputReport.Status, false);

                pDevice.SetRumble(false);
                pDevice.SetLEDs(true, true, true, true);

                // Close the connection and dispose of the device.
                pDevice.Disconnect();
                pDevice.Dispose();

                OnDisconnect(wiimoteid, this.pWiimoteMap.Count);
            }
        }
        #endregion

        private void connectRumble(object device)
        {
            Wiimote wiimote = (Wiimote)device;
            Thread.Sleep(CONNECT_RUMBLE_TIME);
            wiimote.SetRumble(true);
            Thread.Sleep(CONNECT_RUMBLE_TIME);
            wiimote.SetRumble(false);
        }

        #region Wiimote Event Handlers
        /// <summary>
        /// This is called when an extension is attached or unplugged.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void handleWiimoteExtensionChanged(object sender, WiimoteExtensionChangedEventArgs e)
        {
            
            //pDeviceMutex.WaitOne();
            
            // Check we have a valid device.
            if (sender == null)
                return;

            Wiimote pDevice = ((Wiimote)sender);
            // If an extension is attached at runtime we want to enable it.
            if (e.Inserted)
            {
                Console.WriteLine("Enabling extension " + e.ExtensionType);
                pDevice.SetReportType(InputReport.IRExtensionAccel, true);
            }
            else
            {
                Console.WriteLine("Disabling extension " + e.ExtensionType);
                pDevice.SetReportType(InputReport.IRAccel, true);
            }
            
            //pDeviceMutex.ReleaseMutex();
            
        }


        private void WiimoteHandlerWorker()
        {
            
            double millisecondsForEachFrame = 1000 / Settings.Default.pointer_FPS;
            DateTime lastFrame = DateTime.Now;

            while (true)
            {
                double delay = DateTime.Now.Subtract(lastFrame).TotalMilliseconds;
                double wait = millisecondsForEachFrame - delay;
                if (wait > 0)
                {
                    Thread.Sleep((int)wait);
                }

                lastFrame = DateTime.Now;

                if (bRunning)
                {
                    //DateTime now = DateTime.Now;

                    pDeviceMutex.WaitOne();

                    try
                    {
                        Queue<WiiContact> allContacts = new Queue<WiiContact>();

                        foreach (WiimoteControl control in pWiimoteMap.Values)
                        {
                            if (eventBuffer.ContainsKey(control.Wiimote.HIDDevicePath))
                            {
                                WiimoteChangedEventArgs e = eventBuffer[control.Wiimote.HIDDevicePath];

                                if (control.handleWiimoteChanged(this, e) && control.Status.InPowerSave)
                                {
                                    this.wakeFromPowerSave(control);
                                }

                                if (this.OnStatusUpdate != null)
                                {
                                    this.OnStatusUpdate(control.Status);
                                }

                                /*
                                if (control.FrameQueue.Count > 0)
                                {
                                    FrameEventArgs frame = control.FrameQueue.Dequeue();

                                    ulong timeDelta = ((ulong)Stopwatch.GetTimestamp() / 10000) - (frame.Timestamp / 10000);
                                    if (timeDelta < OLD_FRAME_TIMEOUT) //Happens when the pointer is out of reach
                                    {
                                        foreach (WiiContact contact in frame.Contacts)
                                        {
                                            allContacts.Enqueue(contact);
                                        }
                                    }
                                }
                                */
                            }
                        }

                        //FrameEventArgs newFrame = new FrameEventArgs((ulong)Stopwatch.GetTimestamp(), allContacts);

                        //this.OnNewFrame(this, newFrame);

                        TouchOutputFactory.getCurrentProviderHandler().processEventFrame();

                        if (Settings.Default.pointer_customCursor)
                        {
                            D3DCursorWindow.Current.RefreshCursors();
                        }

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error handling Wiimote: " + ex.Message);
                    }

                    pDeviceMutex.ReleaseMutex();

                    //Console.WriteLine("handle wiimote time : " + DateTime.Now.Subtract(now).TotalMilliseconds);
                }

            }
        }

        /// <summary>
        /// This is called when the state of the wiimote changes and a new state report is available.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void handleWiimoteChanged(object sender, WiimoteChangedEventArgs e)
        {

            eventBuffer[((Wiimote)sender).HIDDevicePath] = e;

            pWiimoteMap[((Wiimote)sender).HIDDevicePath].LastWiimoteEventTime = DateTime.Now;

        }
        #endregion

        public static UserControl getSettingsControl()
        {
            return new WiiPointerProviderSettings();
        }
    }
}
