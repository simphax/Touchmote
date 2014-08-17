using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;
using WiimoteLib;
using WiiTUIO.DeviceUtils;
using WiiTUIO.Properties;
using WiiTUIO.Provider;

namespace WiiTUIO.Output.Handlers.Touch
{
    class PhoneTouchHandler : IButtonHandler, ICursorHandler
    {
        private ITouchProviderHandler handler;

        private Dictionary<int, CursorPos> cursorPositions;
        private Dictionary<int, bool> touchDown;
        private Dictionary<int, bool> wasTouchDown;

        private bool useCustomCursor = false;

        private long id;

        private Dictionary<int, D3DCursor> cursors;

        private Screen primaryScreen;

        public PhoneTouchHandler(ITouchProviderHandler handler, long id)
        {
            this.id = id;
            this.handler = handler;
            ulong touchStartID = (ulong)(id - 1) * 4 + 1;//This'll make sure the touch point IDs won't be the same. DuoTouch uses a span of 4 IDs.

            this.cursorPositions = new Dictionary<int, CursorPos>();
            this.touchDown = new Dictionary<int, bool>();
            this.wasTouchDown = new Dictionary<int, bool>();
            this.cursors = new Dictionary<int, D3DCursor>();

            this.primaryScreen = DeviceUtil.GetScreen(Settings.Default.primaryMonitor);

            Settings.Default.PropertyChanged += Settings_PropertyChanged;
        }

        void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(e.PropertyName == "primaryMonitor")
            {
                this.primaryScreen = DeviceUtil.GetScreen(Settings.Default.primaryMonitor);
            }
        }

        public bool reset()
        {
            return true;
        }

        public bool setPosition(string key, Provider.CursorPos cursorPos)
        {
            if (key.Length > 5 && key.Substring(0,5).ToLower().Equals("touch"))
            {
                int id = int.Parse(key.Substring(5));

                cursorPositions.Remove(id);
                cursorPositions.Add(id, cursorPos);

                return true;
            }
            return false;
        }

        private void commitAll()
        {
            Queue<WiiContact> lFrame = new Queue<WiiContact>(1);

            foreach(int key in cursorPositions.Keys)
            {
                CursorPos cursorPos;
                if(cursorPositions.TryGetValue(key,out cursorPos))
                {
                        D3DCursor cursor;
                        if (cursors.TryGetValue(key, out cursor))
                        {
                            bool pressed = false;
                            touchDown.TryGetValue(key, out pressed);

                            bool wasPressed = false;
                            wasTouchDown.TryGetValue(key, out wasPressed);

                            if (!cursorPos.OutOfReach)
                            {
                                // Store the state.

                                cursor.Show();

                                //significant = true;
                                if(pressed)
                                {
                                    cursor.SetPressed();
                                }
                                else
                                {
                                    cursor.SetReleased();
                                }

                                cursor.SetPosition(new System.Windows.Point(cursorPos.X, cursorPos.Y));

                                ContactType contactType;

                                if(pressed != wasPressed)
                                {
                                    if(pressed)
                                    {
                                        contactType = ContactType.Start;
                                    }
                                    else
                                    {
                                        contactType = ContactType.EndToHover;
                                    }
                                    wasTouchDown.Remove(key);
                                    wasTouchDown.Add(key, pressed);
                                }
                                else
                                {
                                    if(pressed)
                                    {
                                        contactType = ContactType.Move;
                                    }
                                    else
                                    {
                                        contactType = ContactType.Hover;
                                    }
                                }


                                lFrame.Enqueue(new WiiContact((ulong)key, contactType, new System.Windows.Point(cursorPos.X, cursorPos.Y), 0, new System.Windows.Vector(primaryScreen.Bounds.Width, primaryScreen.Bounds.Height)));
                            }
                            else //pointer out of reach
                            {
                                cursor.Hide();
                                cursor.SetPosition(new System.Windows.Point(cursorPos.X, cursorPos.Y));
                            }
                        }
                        
                 
                    }

                }
                foreach (WiiContact contact in lFrame)
                {
                    this.handler.queueContact(contact);
                }
            }

        public bool setButtonDown(string key)
        {
            if (key.Length > 5 && key.Substring(0, 5).ToLower().Equals("touch"))
            {
                int id = int.Parse(key.Substring(5));

                this.touchDown.Remove(id);
                this.touchDown.Add(id, true);

                return true;
            }

            return false;
        }

        public bool setButtonUp(string key)
        {
            if (key.Length > 5 && key.Substring(0, 5).ToLower().Equals("touch"))
            {
                int id = int.Parse(key.Substring(5));

                this.touchDown.Remove(id);
                this.touchDown.Add(id, false);

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
                this.cursors.Add(0, new D3DCursor(((int)this.id - 1) * 2, myColor));
                this.cursors.Add(1, new D3DCursor(((int)this.id - 1) * 2 + 1, myColor));

                foreach(D3DCursor cursor in cursors.Values)
                {
                    cursor.Show();
                    D3DCursorWindow.Current.AddCursor(cursor);
                }
            }

            return true;
        }

        public bool disconnect()
        {
            App.Current.Dispatcher.BeginInvoke(new Action(delegate()
            {
                foreach (D3DCursor cursor in cursors.Values)
                {
                    D3DCursorWindow.Current.RemoveCursor(cursor);
                }
            }), null);

            return true;
        }

        public bool startUpdate()
        {
            return true;
        }

        public bool endUpdate()
        {
            this.commitAll();

            return true;
        }
    }
}
