using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiiTUIO.Output.Handlers.Xinput
{
    public class XinputHandler : IButtonHandler, IStickHandler, IRumbleFeedback
    {
        private static string PREFIX = "360.";

        private XinputBus xinputBus;
        private XinputDevice device;
        private XinputReport report;

        private long id;

        public Action<Byte, Byte> OnRumble { get; set; }

        public XinputHandler(long id)
        {
            this.id = id;
            xinputBus = XinputBus.Default;
        }

        public bool connect()
        {
            this.disconnect();
            device = new XinputDevice(xinputBus, (int)id);
            report = new XinputReport((int)id);
            device.OnRumble += device_OnRumble;
            return device.Connect();
        }

        private void device_OnRumble(byte big, byte small)
        {
            if (OnRumble != null)
            {
                OnRumble(big, small);
            }
        }

        public bool disconnect()
        {
            if (device != null)
            {
                return device.Remove();
            }
            return false;
        }

        public bool setButtonUp(string key)
        {
            if (key.Length > 4 && key.ToLower().Substring(0, 4).Equals(PREFIX))
            {
                string button = key.ToLower().Substring(4);
                switch (button)
                {
                    case "triggerr":
                        report.TriggerR = 0.0;
                        break;
                    case "triggerl":
                        report.TriggerL = 0.0;
                        break;
                    case "a":
                        report.A = false;
                        break;
                    case "b":
                        report.B = false;
                        break;
                    case "x":
                        report.X = false;
                        break;
                    case "y":
                        report.Y = false;
                        break;
                    case "back":
                        report.Back = false;
                        break;
                    case "start":
                        report.Start = false;
                        break;
                    case "stickpressl":
                        report.StickPressL = false;
                        break;
                    case "stickpressr":
                        report.StickPressR = false;
                        break;
                    case "up":
                        report.Up = false;
                        break;
                    case "down":
                        report.Down = false;
                        break;
                    case "right":
                        report.Right = false;
                        break;
                    case "left":
                        report.Left = false;
                        break;
                    case "guide":
                        report.Guide = false;
                        break;
                    case "bumperl":
                        report.BumperL = false;
                        break;
                    case "bumperr":
                        report.BumperR = false;
                        break;
                    case "stickrright":
                        report.StickRX = 0.5;
                        break;
                    case "stickrup":
                        report.StickRY = 0.5;
                        break;
                    case "sticklright":
                        report.StickLX = 0.5;
                        break;
                    case "sticklup":
                        report.StickLY = 0.5;
                        break;
                    case "stickrleft":
                        report.StickRX = 0.5;
                        break;
                    case "stickrdown":
                        report.StickRY = 0.5;
                        break;
                    case "sticklleft":
                        report.StickLX = 0.5;
                        break;
                    case "stickldown":
                        report.StickLY = 0.5;
                        break;
                    default:
                        return false; //No valid key code was found
                }
                return true;
            }
            return false;
        }

        public bool setButtonDown(string key)
        {
            if (key.Length > 4 && key.ToLower().Substring(0, 4).Equals(PREFIX))
            {
                string button = key.ToLower().Substring(4);
                switch (button)
                {
                    case "triggerr":
                        report.TriggerR = 1.0;
                        break;
                    case "triggerl":
                        report.TriggerL = 1.0;
                        break;
                    case "a":
                        report.A = true;
                        break;
                    case "b":
                        report.B = true;
                        break;
                    case "x":
                        report.X = true;
                        break;
                    case "y":
                        report.Y = true;
                        break;
                    case "back":
                        report.Back = true;
                        break;
                    case "start":
                        report.Start = true;
                        break;
                    case "stickpressl":
                        report.StickPressL = true;
                        break;
                    case "stickpressr":
                        report.StickPressR = true;
                        break;
                    case "up":
                        report.Up = true;
                        break;
                    case "down":
                        report.Down = true;
                        break;
                    case "right":
                        report.Right = true;
                        break;
                    case "left":
                        report.Left = true;
                        break;
                    case "guide":
                        report.Guide = true;
                        break;
                    case "bumperl":
                        report.BumperL = true;
                        break;
                    case "bumperr":
                        report.BumperR = true;
                        break;
                    case "stickrright":
                        report.StickRX = 1.0;
                        break;
                    case "stickrup":
                        report.StickRY = 0.0;
                        break;
                    case "sticklright":
                        report.StickLX = 1.0;
                        break;
                    case "sticklup":
                        report.StickLY = 0.0;
                        break;
                    case "stickrleft":
                        report.StickRX = 0.0;
                        break;
                    case "stickrdown":
                        report.StickRY = 1.0;
                        break;
                    case "sticklleft":
                        report.StickLX = 0.0;
                        break;
                    case "stickldown":
                        report.StickLY = 1.0;
                        break;
                    default:
                        return false; //No valid key code was found
                }
                return true;
            }
            return false;
        }

        public bool setValue(string key, double value)
        {
            if (key.Length > 4 && key.ToLower().Substring(0, 4).Equals(PREFIX))
            {
                string handle = key.ToLower().Substring(4);
                switch (handle)
                {
                    case "sticklx":
                        report.StickLX = value;
                        break;
                    case "stickly":
                        report.StickLY = value;
                        break;
                    case "stickrx":
                        report.StickRX = value;
                        break;
                    case "stickry":
                        report.StickRY = value;
                        break;
                    case "triggerr":
                        report.TriggerR = value;
                        break;
                    case "triggerl":
                        report.TriggerL = value;
                        break;
                    default:
                        return false; //No valid handle was found
                }
                return true;
            }
            return false;
        }

        public bool startUpdate()
        {
            return true;
        }

        public bool endUpdate()
        {
            if (device != null && report != null)
            {
                return device.Update(report);
            }
            return false;
        }
    }
}
