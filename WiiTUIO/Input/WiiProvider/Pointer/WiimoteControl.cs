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
using WiiTUIO.Output.Handlers;
using WiiTUIO.Properties;
using WindowsInput;

namespace WiiTUIO.Provider
{
    class WiimoteControl
    {
        public DateTime LastWiimoteEventTime = DateTime.Now; //Last time recieved an update
        public DateTime LastSignificantWiimoteEventTime = DateTime.Now; //Last time when updated the cursor or button config. Used for power saving features.

        public Wiimote Wiimote;
        public WiimoteStatus Status;

        /// <summary>
        /// Used to obtain mutual exlusion over Wiimote updates.
        /// </summary>
        public Mutex WiimoteMutex = new Mutex();
        private WiiKeyMapper keyMapper;
        private bool firstConfig = true;
        private string currentKeymap;
        private HandlerFactory handlerFactory;

        public WiimoteControl(int id, Wiimote wiimote)
        {
            this.Wiimote = wiimote;
            this.Status = new WiimoteStatus();
            this.Status.ID = id;

            this.handlerFactory = new HandlerFactory();

            this.keyMapper = new WiiKeyMapper(id,handlerFactory);

            this.keyMapper.OnButtonDown += WiiButton_Down;
            this.keyMapper.OnButtonUp += WiiButton_Up;
            this.keyMapper.OnConfigChanged += WiiKeyMap_ConfigChanged;
            this.keyMapper.OnRumble += WiiKeyMap_OnRumble;
        }

        private void WiiKeyMap_OnRumble(bool rumble)
        {
            Console.WriteLine("Set rumble to: "+rumble);
            WiimoteMutex.WaitOne();
            this.Wiimote.SetRumble(rumble);
            WiimoteMutex.ReleaseMutex();
        }


        private void WiiKeyMap_ConfigChanged(WiiKeyMapConfigChangedEvent evt)
        {
            if (firstConfig)
            {
                currentKeymap = evt.Filename;
                firstConfig = false;
            }
            else if(evt.Filename != currentKeymap)
            {
                currentKeymap = evt.Filename;
                OverlayWindow.Current.ShowNotice("Layout for Wiimote " + this.Status.ID + " changed to \"" + evt.Name + "\"", this.Status.ID);
            }
            /*
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
            */
        }

        private void WiiButton_Up(WiiButtonEvent evt)
        {
            /*
            foreach (string action in evt.Actions)
            {
                if (action.ToLower() == "nextlayout" && !evt.Handled)
                {
                    List<LayoutChooserSetting> layoutList = this.keyMapper.GetLayoutList();
                    int curpos = 0;
                    int foundpos = 0;
                    foreach (LayoutChooserSetting setting in layoutList)
                    {
                        JToken token = setting.Keymap;
                        if (token != null)
                        {
                            if (token.ToString() == this.currentKeymap)
                            {
                                foundpos = curpos;
                            }
                        }
                        curpos++;
                    }
                    LayoutChooserSetting nextLayout = layoutList.ElementAt(++foundpos % (layoutList.Count() - 1));
                    if (nextLayout.Keymap != null)
                    {
                        this.keyMapper.SetFallbackKeymap(nextLayout.Keymap);
                        evt.Handled = true;
                    }
                }
                if (action.ToLower() == "pointertoggle" && !evt.Handled)
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
            }
            */
        }

        private void WiiButton_Down(WiiButtonEvent evt)
        {
        }


        public bool handleWiimoteChanged(object sender, WiimoteChangedEventArgs e)
        {
            // Obtain mutual excluseion.
            WiimoteMutex.WaitOne();

            bool significant = false;

            try
            {
                WiimoteState ws = e.WiimoteState;
                this.Status.Battery = (ws.Battery > 0xc8 ? 0xc8 : (int)ws.Battery);

                significant = keyMapper.processWiimoteState(ws);

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
        }
    }
}
