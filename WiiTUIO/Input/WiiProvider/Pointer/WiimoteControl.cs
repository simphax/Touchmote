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
        }

        private void WiiButton_Up(WiiButtonEvent evt)
        {
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
