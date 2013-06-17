using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using WiimoteLib;
using WiiTUIO.Properties;
using WindowsInput;

namespace WiiTUIO.Provider
{
    class WiimoteControl
    {
        public FrameEventArgs LastFrameEvent;
        public Queue<FrameEventArgs> FrameQueue = new Queue<FrameEventArgs>(1);
        public DateTime LastWiimoteEventTime = DateTime.Now; //Last time recieved an update
        public DateTime LastSignificantWiimoteEventTime = DateTime.Now; //Last time when updated the cursor or button config. Used for power saving features.
        //public bool InPowerSave = false;

        public Wiimote Wiimote;

        public WiimoteStatus Status;

        /// <summary>
        /// Used to obtain mutual exlusion over Wiimote updates.
        /// </summary>
        public Mutex WiimoteMutex = new Mutex();

        private InputSimulator inputSimulator;

        private ScreenPositionCalculator screenPositionCalculator;

        private DuoTouch duoTouch;

        private WiiKeyMapper keyMapper;

        private bool touchDownMaster = false;

        private bool touchDownSlave = false;

        private bool useCustomCursor = false;

        private bool showPointer = true;

        private bool mouseMode = false;

        private bool gamingMouse = false;

        private bool firstConfig = true;

        private CursorPos lastpoint;

        private System.Drawing.Rectangle screenBounds;

        private WiimoteState lastWiimoteState;

        private D3DCursor masterCursor;
        private D3DCursor slaveCursor;

        private string currentKeymap;

        public WiimoteControl(int id, Wiimote wiimote)
        {
            this.Wiimote = wiimote;
            this.Status = new WiimoteStatus();
            this.Status.ID = id;

            lastpoint = new CursorPos(0,0,0);

            this.screenBounds = Util.ScreenBounds;

            ulong touchStartID = (ulong)(id - 1) * 4 + 1; //This'll make sure the touch point IDs won't be the same. DuoTouch uses a span of 4 IDs.
            this.duoTouch = new DuoTouch(this.screenBounds, Properties.Settings.Default.pointer_positionSmoothing, touchStartID);
            this.keyMapper = new WiiKeyMapper(id);

            this.keyMapper.KeyMap.OnButtonDown += WiiButton_Down;
            this.keyMapper.KeyMap.OnButtonUp += WiiButton_Up;
            this.keyMapper.KeyMap.OnConfigChanged += WiiKeyMap_ConfigChanged;
            this.keyMapper.KeyMap.OnRumble += WiiKeyMap_OnRumble;

            this.inputSimulator = new InputSimulator();
            this.screenPositionCalculator = new ScreenPositionCalculator();
            this.useCustomCursor = Settings.Default.pointer_customCursor;
            if (this.useCustomCursor)
            {
                Color myColor = CursorColor.getColor(this.Status.ID);
                this.masterCursor = new D3DCursor((this.Status.ID-1)*2,myColor);
                this.slaveCursor = new D3DCursor((this.Status.ID-1)*2+1,myColor);

                masterCursor.Hide();
                slaveCursor.Hide();

                D3DCursorWindow.Current.AddCursor(masterCursor);
                D3DCursorWindow.Current.AddCursor(slaveCursor);

                this.keyMapper.KeyMap.SendConfigChangedEvt();
            }


        }

        private void WiiKeyMap_OnRumble(bool rumble)
        {
            Console.WriteLine("Set rumble to: "+rumble);
            WiimoteMutex.WaitOne();
            this.Wiimote.SetRumble(rumble);
            WiimoteMutex.ReleaseMutex();
        }

        private bool usingCursors()
        {
            return this.useCustomCursor && this.masterCursor != null && this.slaveCursor != null;
        }

        private void WiiKeyMap_ConfigChanged(WiiKeyMapConfigChangedEvent evt)
        {
            currentKeymap = evt.Filename;
            if (firstConfig)
            {
                firstConfig = false;
            }
            else
            {
                OverlayWindow.Current.ShowNotice("Layout for Wiimote " + this.Status.ID + " changed to \"" + evt.Name + "\"", this.Status.ID);
            }
            if (evt.Pointer.ToLower() == "touch")
            {
                this.showPointer = true;
                this.mouseMode = false;
                this.duoTouch.enableHover();
                if (this.usingCursors())
                {
                    this.masterCursor.Show();
                }
            }
            else if (evt.Pointer.ToLower() == "mouse")
            {
                this.mouseMode = true;
                this.gamingMouse = false;
                this.duoTouch.disableHover();
                if (this.usingCursors())
                {
                    this.masterCursor.Hide();
                    this.slaveCursor.Hide();
                }
                MouseSimulator.WakeCursor();
            }
            else if (evt.Pointer.ToLower() == "gamingmouse")
            {
                this.mouseMode = true;
                this.gamingMouse = true;
                this.duoTouch.disableHover();
                if (this.usingCursors())
                {
                    this.masterCursor.Hide();
                    this.slaveCursor.Hide();
                }
                MouseSimulator.WakeCursor();
            }
            else
            {
                this.showPointer = false;
                this.mouseMode = false;
                this.duoTouch.disableHover();
                if (this.usingCursors())
                {
                    this.masterCursor.Hide();
                    this.slaveCursor.Hide();
                }
            }
        }

        private void WiiButton_Up(WiiButtonEvent evt)
        {
            if (evt.Action.ToLower() == "nextlayout" && !evt.Handled)
            {
                IEnumerable<JObject> layoutList = this.keyMapper.GetLayoutList();
                int curpos = 0;
                int foundpos = 0;
                foreach (JObject obj in layoutList)
                {
                    JToken token = obj.GetValue("Keymap");
                    if (token != null)
                    {
                        if(token.ToString() == this.currentKeymap)
                        {
                            foundpos = curpos;
                        }
                    }
                    curpos++;
                }
                JObject nextLayout = layoutList.ElementAt(++foundpos % (layoutList.Count()-1));
                if (nextLayout.GetValue("Keymap") != null)
                {
                    this.keyMapper.SetFallbackKeymap(nextLayout.GetValue("Keymap").ToString());
                    evt.Handled = true;
                }
            }
            if (evt.Action.ToLower() == "pointertoggle" && !evt.Handled)
            {
                this.showPointer = this.showPointer ? false : true;
                if (this.showPointer)
                {
                    this.duoTouch.enableHover();
                    if (this.usingCursors() && !mouseMode)
                    {
                        this.masterCursor.Show();
                    }
                }
                else
                {
                    this.duoTouch.disableHover();
                    if (this.usingCursors())
                    {
                        this.masterCursor.Hide();
                        this.slaveCursor.Hide();
                    }
                }
            }
            if (evt.Action.ToLower() == "touchmaster" && !evt.Handled)
            {
                if (this.usingCursors())
                {
                    this.masterCursor.SetReleased();
                }
                touchDownMaster = false;
            }
            if (evt.Action.ToLower() == "touchslave" && !evt.Handled)
            {
                if (this.usingCursors())
                {
                    this.slaveCursor.SetReleased();
                }
                touchDownSlave = false;
            }
        }

        private void WiiButton_Down(WiiButtonEvent evt)
        {
            if (evt.Action.ToLower() == "touchmaster" && !evt.Handled)
            {
                if (this.usingCursors())
                {
                    this.masterCursor.SetPressed();
                }
                touchDownMaster = true;
            }
            if (evt.Action.ToLower() == "touchslave" && !evt.Handled)
            {
                if (this.usingCursors())
                {
                    this.slaveCursor.SetPressed();
                }
                touchDownSlave = true;
            }
        }

        double deltaXBuffer = 0.0;
        double deltaYBuffer = 0.0;

        public bool handleWiimoteChanged(object sender, WiimoteChangedEventArgs e)
        {
            // Obtain mutual excluseion.
            WiimoteMutex.WaitOne();

            bool significant = false;

            try
            {
                this.screenBounds = Util.ScreenBounds;
                this.duoTouch.screenBounds = Util.ScreenBounds;

                Queue<WiiContact> lFrame = new Queue<WiiContact>(1);
                // Store the state.
                WiimoteState pState = e.WiimoteState;

                this.Status.Battery = (pState.Battery > 0xc8 ? 0xc8 : (int)pState.Battery);

                bool pointerOutOfReach = false;

                CursorPos newpoint = lastpoint;

                newpoint = screenPositionCalculator.CalculateCursorPos(e);

                if (newpoint.X < 0 || newpoint.Y < 0)
                {
                    newpoint = lastpoint;
                    pointerOutOfReach = true;
                }

                WiimoteState ws = e.WiimoteState;
                if (keyMapper.processWiimoteState(ws))
                {
                    significant = true;
                    this.lastWiimoteState = ws;
                }


                if (!pointerOutOfReach)
                {
                    if (this.usingCursors() && !mouseMode && showPointer)
                    {
                        this.masterCursor.Show();
                    }
                    significant = true;
                    if (this.touchDownMaster)
                    {
                        duoTouch.setContactMaster();
                    }
                    else
                    {
                        duoTouch.releaseContactMaster();
                    }

                    duoTouch.setMasterPosition(new System.Windows.Point(newpoint.X, newpoint.Y));

                    if (this.touchDownSlave)
                    {
                        if (this.usingCursors() && !mouseMode && showPointer)
                        {
                            this.slaveCursor.Show();
                        }
                        duoTouch.setSlavePosition(new System.Windows.Point(newpoint.X, newpoint.Y));
                        duoTouch.setContactSlave();
                    }
                    else
                    {
                        duoTouch.releaseContactSlave();
                        if (this.usingCursors() && !mouseMode)
                        {
                            this.slaveCursor.Hide();
                        }
                    }

                    lastpoint = newpoint;

                    lFrame = duoTouch.getFrame();
                    if (this.usingCursors() && !mouseMode)
                    {
                        WiiContact master = null;
                        WiiContact slave = null;
                        foreach (WiiContact contact in lFrame)
                        {
                            if (master == null)
                            {
                                master = contact;
                            }
                            else if (master.Priority > contact.Priority)
                            {
                                slave = master;
                                master = contact;
                            }
                            else
                            {
                                slave = contact;
                            }
                        }
                        if(master != null)
                        {
                            this.masterCursor.SetPosition(master.Position);
                            this.masterCursor.SetRotation(newpoint.Rotation);
                        }
                        if(slave != null)
                        {
                            this.slaveCursor.SetPosition(slave.Position);
                            this.slaveCursor.SetRotation(newpoint.Rotation);
                        }
                    }

                    FrameEventArgs pFrame = new FrameEventArgs((ulong)Stopwatch.GetTimestamp(), lFrame);

                    this.FrameQueue.Enqueue(pFrame);
                    this.LastFrameEvent = pFrame;

                    if (mouseMode && !this.touchDownMaster && !this.touchDownSlave && this.showPointer) //Mouse mode
                    {
                        if (gamingMouse)
                        {
                            double deltaX = (newpoint.X - ((double)this.screenBounds.Width / 2.0)) / (double)this.screenBounds.Width;
                            double deltaY = (newpoint.Y - ((double)this.screenBounds.Height / 2.0)) / (double)this.screenBounds.Height;
                            deltaX = Math.Sign(deltaX) * deltaX * deltaX * 50;
                            deltaY = Math.Sign(deltaY) * deltaY * deltaY * 50 * ((double)this.screenBounds.Width / (double)this.screenBounds.Height);
                            deltaXBuffer += deltaX % 1;
                            deltaYBuffer += deltaY % 1;
                            int roundDeltaX = (int)deltaX;
                            int roundDeltaY = (int)deltaY;
                            if (deltaXBuffer > 1 || deltaXBuffer < -1)
                            {
                                roundDeltaX += Math.Sign(deltaXBuffer);
                                deltaXBuffer -= Math.Sign(deltaXBuffer);
                            }
                            if (deltaYBuffer > 1 || deltaYBuffer < -1)
                            {
                                roundDeltaY += Math.Sign(deltaYBuffer);
                                deltaYBuffer -= Math.Sign(deltaYBuffer);
                            }
                            this.inputSimulator.Mouse.MoveMouseBy(roundDeltaX, roundDeltaY);
                        }
                        else
                        {
                            this.inputSimulator.Mouse.MoveMouseToPositionOnVirtualDesktop((65535 * newpoint.X) / this.screenBounds.Width, (65535 * newpoint.Y) / this.screenBounds.Height);
                        }
                        MouseSimulator.WakeCursor();
                    }
                }
                else //pointer out of reach
                {
                    if (this.usingCursors() && !mouseMode)
                    {
                        this.masterCursor.Hide();
                        this.masterCursor.SetPosition(new System.Windows.Point(lastpoint.X, lastpoint.Y));
                    }
                }

                LastWiimoteEventTime = DateTime.Now;
                if (significant)
                {
                    this.LastSignificantWiimoteEventTime = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error handling Wiimote in WiimoteControl: " + ex.Message);
                return significant;
            }
            //this.BatteryState = (pState.Battery > 0xc8 ? 0xc8 : (int)pState.Battery);
            

            // Release mutual exclusion.
            WiimoteMutex.ReleaseMutex();
            return significant;
        }

        public void Teardown()
        {
            this.keyMapper.Teardown();
            App.Current.Dispatcher.BeginInvoke(new Action(delegate()
            {
                D3DCursorWindow.Current.RemoveCursor(this.masterCursor);
                D3DCursorWindow.Current.RemoveCursor(this.slaveCursor);
            }), null);
        }
    }
}
