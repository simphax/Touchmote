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
    class TouchHandler : IButtonHandler, ICursorHandler
    {

        IProviderHandler handler;
        DuoTouch duoTouch;

        private bool touchDownMaster = false;
        private bool touchDownSlave = false;
        private bool useCustomCursor = false;

        private long id;

        private D3DCursor masterCursor;
        private D3DCursor slaveCursor;

        public TouchHandler(IProviderHandler handler, long id)
        {
            this.id = id;
            this.handler = handler;
            ulong touchStartID = (ulong)(id - 1) * 4 + 1;//This'll make sure the touch point IDs won't be the same. DuoTouch uses a span of 4 IDs.
            this.duoTouch = new DuoTouch(Screen.PrimaryScreen.Bounds, Settings.Default.pointer_positionSmoothing, touchStartID);
        }

        public bool setPosition(string key, Provider.CursorPos cursorPos)
        {
            if (key.ToLower().Equals("touch"))
            {
                if(!cursorPos.OutOfReach)
                {
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

                    //lastpoint = newpoint;

                    lFrame = duoTouch.getFrame();
                    if (this.usingCursors())
                    {
                        WiiContact master = null;
                        WiiContact slave = null;
                        foreach (WiiContact contact in lFrame)
                        {
                            this.handler.queueContact(contact);

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
            return false;
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
            this.duoTouch.screenBounds = Screen.PrimaryScreen.Bounds;
            return true;
        }

        public bool endUpdate()
        {
            return true;
        }


        private bool usingCursors()
        {
            return this.useCustomCursor && this.masterCursor != null && this.slaveCursor != null;
        }
    }
}
