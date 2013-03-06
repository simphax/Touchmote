using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WiimoteLib;
using WindowsInput;

namespace WiiTUIO.Provider
{
    class WiimoteControl
    {
        public FrameEventArgs LastFrameEvent;
        public Queue<FrameEventArgs> FrameQueue = new Queue<FrameEventArgs>(1);
        public DateTime LastWiimoteEventTime = DateTime.Now;

        public Wiimote Wiimote;

        public int ID;

        /// <summary>
        /// Used to obtain mutual exlusion over Wiimote updates.
        /// </summary>
        private Mutex pDeviceMutex = new Mutex();

        private InputSimulator inputSimulator;

        private ScreenPositionCalculator screenPositionCalculator;

        private DuoTouch duoTouch;

        private WiiKeyMapper keyMapper;

        private bool touchDownMaster = false;

        private bool touchDownSlave = false;

        private bool showPointer = true;

        private bool mouseMode = false;

        private bool gamingMouse = false;

        private WiimoteLib.Point lastpoint;

        private Rectangle screenBounds;

        public WiimoteControl(int id, Wiimote wiimote)
        {
            this.Wiimote = wiimote;
            this.ID = id;

            lastpoint = new WiimoteLib.Point();
            lastpoint.X = 0;
            lastpoint.Y = 0;

            this.screenBounds = Util.ScreenBounds;

            ulong touchStartID = (ulong)(id - 1) * 4 + 1; //This'll make sure the touch point IDs won't be the same. DuoTouch uses a span of 4 IDs.
            this.duoTouch = new DuoTouch(this.screenBounds, Properties.Settings.Default.pointer_smoothingSize, touchStartID);
            this.keyMapper = new WiiKeyMapper(id);

            this.keyMapper.KeyMap.OnButtonDown += WiiButton_Down;
            this.keyMapper.KeyMap.OnButtonUp += WiiButton_Up;
            this.keyMapper.KeyMap.OnConfigChanged += WiiKeyMap_ConfigChanged;

            this.WiiKeyMap_ConfigChanged(new WiiKeyMapConfigChangedEvent(this.keyMapper.KeyMap.Pointer));

            this.inputSimulator = new InputSimulator();
            this.screenPositionCalculator = new ScreenPositionCalculator();
        }

        private void WiiKeyMap_ConfigChanged(WiiKeyMapConfigChangedEvent evt)
        {
            if (evt.NewPointer.ToLower() == "touch")
            {
                this.mouseMode = false;
                if (this.showPointer)
                {
                    this.duoTouch.enableHover();
                }
            }
            else if (evt.NewPointer.ToLower() == "mouse")
            {
                this.mouseMode = true;
                this.gamingMouse = false;
                this.duoTouch.disableHover();
                MouseSimulator.WakeCursor();
            }
            else if (evt.NewPointer.ToLower() == "gamingmouse")
            {
                this.mouseMode = true;
                this.gamingMouse = true;
                this.duoTouch.disableHover();
                MouseSimulator.WakeCursor();
            }
        }

        private void WiiButton_Up(WiiButtonEvent evt)
        {
            if (evt.Action.ToLower() == "pointertoggle" && !evt.Handled)
            {
                this.showPointer = this.showPointer ? false : true;
                if (this.showPointer)
                {
                    this.duoTouch.enableHover();
                }
                else
                {
                    this.duoTouch.disableHover();
                }
            }
            if (evt.Action.ToLower() == "touchmaster" && !evt.Handled)
            {
                touchDownMaster = false;
            }
            if (evt.Action.ToLower() == "touchslave" && !evt.Handled)
            {
                touchDownSlave = false;
            }
        }

        private void WiiButton_Down(WiiButtonEvent evt)
        {
            if (evt.Action.ToLower() == "touchmaster" && !evt.Handled)
            {
                touchDownMaster = true;
            }
            if (evt.Action.ToLower() == "touchslave" && !evt.Handled)
            {
                touchDownSlave = true;
            }
        }

        double deltaXBuffer = 0.0;
        double deltaYBuffer = 0.0;

        public void handleWiimoteChanged(object sender, WiimoteChangedEventArgs e)
        {
            // Obtain mutual excluseion.
            pDeviceMutex.WaitOne();

            this.screenBounds = Util.ScreenBounds;
            this.duoTouch.screenBounds = Util.ScreenBounds;

            LastWiimoteEventTime = DateTime.Now;

            Queue<WiiContact> lFrame = new Queue<WiiContact>(1);
            // Store the state.
            WiimoteState pState = e.WiimoteState;

            bool pointerOutOfReach = false;

            WiimoteLib.Point newpoint = lastpoint;

            newpoint = screenPositionCalculator.GetPosition(e);

            if (newpoint.X < 0 || newpoint.Y < 0)
            {
                newpoint = lastpoint;
                pointerOutOfReach = true;
            }

            WiimoteState ws = e.WiimoteState;

            keyMapper.processButtonState(ws.ButtonState);

            if (!pointerOutOfReach)
            {

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
                    duoTouch.setSlavePosition(new System.Windows.Point(newpoint.X, newpoint.Y));
                    duoTouch.setContactSlave();
                }
                else
                {
                    duoTouch.releaseContactSlave();
                }

                lastpoint = newpoint;

                lFrame = duoTouch.getFrame();

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
            //this.BatteryState = (pState.Battery > 0xc8 ? 0xc8 : (int)pState.Battery);

            // Release mutual exclusion.
            pDeviceMutex.ReleaseMutex();
        }
    }
}
