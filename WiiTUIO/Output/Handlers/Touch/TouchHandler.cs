using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;
using WiimoteLib;
using WiiTUIO.Properties;
using WiiTUIO.Provider;

namespace WiiTUIO.Output.Handlers.Touch
{
    class TouchHandler : IButtonHandler, ICursorHandler, IStickHandler
    {
        private ITouchProviderHandler handler;
        private DuoTouch duoTouch;
        private CursorPos lastCursorPos;
        private CursorPos positionToPush;

        private bool touchDownMaster = false;
        private bool touchDownSlave = false;
        private bool useCustomCursor = false;

        private CursorPos timeoutCursorPos;
        private System.Timers.Timer timeoutTimer;
        private bool mightTimeOut = false;
        private bool timedOut = false;

        private long id;

        private D3DCursor masterCursor;
        private D3DCursor slaveCursor;

        public TouchHandler(ITouchProviderHandler handler, long id)
        {
            this.id = id;
            this.handler = handler;
            ulong touchStartID = (ulong)(id - 1) * 4 + 1;//This'll make sure the touch point IDs won't be the same. DuoTouch uses a span of 4 IDs.
            this.duoTouch = new DuoTouch(Settings.Default.pointer_positionSmoothing, touchStartID);
            this.lastCursorPos = new CursorPos(0, 0,0,0, 0);

            this.timeoutTimer = new System.Timers.Timer();
            this.timeoutTimer.Interval = Settings.Default.pointer_cursorStillHideTimeout;
            this.timeoutTimer.Elapsed += timeoutTimer_Elapsed;
            this.timeoutTimer.Enabled = true;
            this.timeoutTimer.Start();
        }

        public bool reset()
        {
            this.masterCursor.SetReleased();
            this.slaveCursor.SetReleased();
            return true;
        }

        void timeoutTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            this.timeoutTimer.Stop();
            this.timedOut = true;
            if (this.usingCursors())
            {
                this.masterCursor.Hide();
            }
        }

        void timeoutTimer_Tick(object sender, EventArgs e)
        {
        }

        public bool setPosition(string key, Provider.CursorPos cursorPos)
        {
            if (key.ToLower().Equals("touch"))
            {
                positionToPush = cursorPos;
                return true;
            }
            return false;
        }

        private void setPosition(Provider.CursorPos cursorPos)
        {
            if(!mightTimeOut)
            {
                timeoutCursorPos = cursorPos;
                this.timeoutTimer.Start();
            }
            if (!this.touchDownMaster && !this.touchDownSlave && Math.Abs(this.timeoutCursorPos.X - cursorPos.X) < Settings.Default.pointer_cursorStillThreshold && Math.Abs(this.timeoutCursorPos.Y - cursorPos.Y) < Settings.Default.pointer_cursorStillThreshold)
            {
                this.mightTimeOut = true;
            }
            else
            {
                this.mightTimeOut = false;
                this.timedOut = false;
                this.timeoutTimer.Stop();
            }
            if (!cursorPos.OutOfReach && !timedOut)
            {
                if(timedOut)
                {
                    this.timedOut = false;
                }
                Queue<WiiContact> lFrame = new Queue<WiiContact>(1);
                // Store the state.

                if (this.usingCursors())
                {
                    this.masterCursor.Show();
                }
                //significant = true;
                if (this.touchDownMaster)
                {
                    duoTouch.setContactMaster();
                }
                else
                {
                    duoTouch.releaseContactMaster();
                }

                duoTouch.setMasterPosition(new System.Windows.Point(cursorPos.X, cursorPos.Y));

                if (this.touchDownSlave)
                {
                    if (this.usingCursors())
                    {
                        this.slaveCursor.Show();
                    }
                    duoTouch.setSlavePosition(new System.Windows.Point(cursorPos.X, cursorPos.Y));
                    duoTouch.setContactSlave();
                }
                else
                {
                    duoTouch.releaseContactSlave();
                    if (this.usingCursors())
                    {
                        this.slaveCursor.Hide();
                    }
                }

                lastCursorPos = cursorPos;

                lFrame = duoTouch.getFrame();
                foreach (WiiContact contact in lFrame)
                {
                    this.handler.queueContact(contact);
                }
                if (this.usingCursors())
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
                    if (master != null)
                    {
                        this.masterCursor.SetPosition(master.Position);
                        this.masterCursor.SetRotation(cursorPos.Rotation);
                    }
                    if (slave != null)
                    {
                        this.slaveCursor.SetPosition(slave.Position);
                        this.slaveCursor.SetRotation(cursorPos.Rotation);
                    }
                }
            }
            else //pointer out of reach
            {
                if (this.usingCursors())
                {
                    this.masterCursor.Hide();
                    this.masterCursor.SetPosition(new System.Windows.Point(cursorPos.X, cursorPos.Y));
                }
            }
        }

        public bool setButtonDown(string key)
        {
            if (key.ToLower() == "touchmaster")
            {
                if (this.usingCursors())
                {
                    this.masterCursor.SetPressed();
                }
                touchDownMaster = true;

                return true;
            }

            if (key.ToLower() == "touchslave")
            {
                if (this.usingCursors())
                {
                    this.slaveCursor.SetPressed();
                }
                touchDownSlave = true;

                return true;
            }

            return false;
        }

        public bool setButtonUp(string key)
        {
            if (key.ToLower() == "touchmaster")
            {
                if (this.usingCursors())
                {
                    this.masterCursor.SetReleased();
                }
                touchDownMaster = false;
                return true;
            }
            
            if (key.ToLower() == "touchslave")
            {
                if (this.usingCursors())
                {
                    this.slaveCursor.SetReleased();
                }
                touchDownSlave = false;
                return true;
            }

            return false;
        }

        public bool connect()
        {

            this.useCustomCursor = Settings.Default.pointer_customCursor;
            if (this.useCustomCursor)
            {
                Color myColor = CursorColor.getColor((int)this.id);
                this.masterCursor = new D3DCursor(((int)this.id - 1) * 2, myColor);
                this.slaveCursor = new D3DCursor(((int)this.id - 1) * 2 + 1, myColor);

                masterCursor.Hide();
                slaveCursor.Hide();

                D3DCursorWindow.Current.AddCursor(masterCursor);
                D3DCursorWindow.Current.AddCursor(slaveCursor);
            }

            this.duoTouch.enableHover();
            if (this.usingCursors())
            {
                this.masterCursor.Show();
            }

            return true;
        }

        public bool disconnect()
        {

            App.Current.Dispatcher.BeginInvoke(new Action(delegate()
            {
                D3DCursorWindow.Current.RemoveCursor(this.masterCursor);
                D3DCursorWindow.Current.RemoveCursor(this.slaveCursor);
            }), null);

            return true;
        }

        public bool startUpdate()
        {
            return true;
        }

        public bool endUpdate()
        {
            if (positionToPush != null)
            {

                this.setPosition(positionToPush);

                positionToPush = null;
            }
            return true;
        }

        public bool setValue(string key, double value)
        {
            bool alternativeMode = Settings.Default.alternativeStickToCursorMapping;

            if (alternativeMode)
            {
                return alternativeStickCursor(key, value);
            }
            else
            {
                return normalStickCursor(key, value);
            }
        }

        private bool normalStickCursor(string key, double value)
        {
            int step = (int)Math.Round(30 * value);
            CursorPos fromPos;
            if (positionToPush != null)
            {
                fromPos = positionToPush;
            }
            else
            {
                fromPos = lastCursorPos;
            }
            if (key.ToLower().Equals("touchx-"))
            {
                int x = fromPos.X - step < 0 ? 0 : fromPos.X - step;
                positionToPush = new CursorPos(x, fromPos.Y,0,0, 0);
                return true;
            }
            else if (key.ToLower().Equals("touchx+"))
            {
                int x = fromPos.X + step > this.duoTouch.screenBounds.Width - 1 ? this.duoTouch.screenBounds.Width - 1 : fromPos.X + step;
                positionToPush = new CursorPos(x, fromPos.Y,0,0, 0);
                return true;
            }
            else if (key.ToLower().Equals("touchy-"))
            {
                int y = fromPos.Y + step > this.duoTouch.screenBounds.Height - 1 ? this.duoTouch.screenBounds.Height - 1 : fromPos.Y + step;
                positionToPush = new CursorPos(fromPos.X, y,0,0, 0);
                return true;
            }
            else if (key.ToLower().Equals("touchy+"))
            {
                int y = fromPos.Y - step < 0 ? 0 : fromPos.Y - step;
                positionToPush = new CursorPos(fromPos.X, y,0,0, 0);
                return true;
            }
            return false;
        }

        private bool alternativeStickCursor(string key, double value)
        {
            value = value * 1.4; //We give it a default upscale so we can reach the edges of the screen.
            CursorPos fromPos;
            if (positionToPush != null)
            {
                fromPos = positionToPush;
            }
            else
            {
                fromPos = lastCursorPos;
            }
            if (key.ToLower().Equals("touchx-"))
            {
                int x = (int)((this.duoTouch.screenBounds.Width / 2) - value * (this.duoTouch.screenBounds.Width / 2) + 0.5);
                x = x < 0 ? 0 : x;
                positionToPush = new CursorPos(x, fromPos.Y,0,0, 0);
                return true;
            }
            else if (key.ToLower().Equals("touchx+"))
            {
                int x = (int)((this.duoTouch.screenBounds.Width / 2) + value * (this.duoTouch.screenBounds.Width / 2) + 0.5);
                x = x > this.duoTouch.screenBounds.Width - 1 ? this.duoTouch.screenBounds.Width - 1 : x;
                positionToPush = new CursorPos(x, fromPos.Y,0,0, 0);
                return true;
            }
            else if (key.ToLower().Equals("touchy-"))
            {
                int y = (int)((this.duoTouch.screenBounds.Height / 2) + value * (this.duoTouch.screenBounds.Height / 2) + 0.5);
                y = y > this.duoTouch.screenBounds.Height - 1 ? this.duoTouch.screenBounds.Height - 1 : y;
                positionToPush = new CursorPos(fromPos.X, y,0,0, 0);
                return true;
            }
            else if (key.ToLower().Equals("touchy+"))
            {
                int y = (int)((this.duoTouch.screenBounds.Height / 2) - value * (this.duoTouch.screenBounds.Height / 2) + 0.5);
                y = y < 0 ? 0 : y;
                positionToPush = new CursorPos(fromPos.X, y,0,0, 0);
                return true;
            }
            return false;
        }


        private bool usingCursors()
        {
            return this.useCustomCursor && this.masterCursor != null && this.slaveCursor != null;
        }
    }
}
