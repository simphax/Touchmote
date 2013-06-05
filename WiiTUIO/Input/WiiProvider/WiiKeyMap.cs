using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiimoteLib;
using WiiTUIO.Properties;
using WindowsInput;
using WindowsInput.Native;

namespace WiiTUIO.Provider
{
    public class WiiKeyMap
    {
        private JObject jsonObj;

        public JObject JsonObj
        {
            get { return this.jsonObj; }
        }

        public Action<WiiButtonEvent> OnButtonUp;
        public Action<WiiButtonEvent> OnButtonDown;
        public Action<WiiKeyMapConfigChangedEvent> OnConfigChanged;
        public Action<bool> OnRumble;

        private InputSimulator inputSimulator;

        public XinputDevice XinputDevice;
        public XinputReport XinputReport;

        public DateTime HomeButtonDown = DateTime.Now;

        public string Name;
        public string Filename;

        public WiiKeyMap(JObject jsonObj, string configName, string configFilename, XinputDevice xinput, XinputReport xinputReport)
        {
            this.Name = configName;
            this.Filename = configFilename;
            this.jsonObj = jsonObj;

            this.inputSimulator = new InputSimulator();
            this.XinputDevice = xinput;
            this.XinputReport = xinputReport;
            xinput.OnRumble += Xinput_OnRumble;
        }

        public void SetConfig(JObject newConfig, string name, string filename)
        {
            if (this.jsonObj != newConfig && this.Filename != filename)
            {
                this.jsonObj = newConfig;
                this.Name = name;
                this.Filename = filename;
                string pointer = this.jsonObj.GetValue("Pointer").ToString();
                if (this.OnConfigChanged != null)
                {
                    this.OnConfigChanged(new WiiKeyMapConfigChangedEvent(name,filename,pointer));
                }
            }
        }

        public void SendConfigChangedEvt()
        {
            if (this.OnConfigChanged != null)
            {
                this.OnConfigChanged(new WiiKeyMapConfigChangedEvent(this.Name, this.Filename, this.jsonObj.GetValue("Pointer").ToString()));
            }
        }

        private void Xinput_OnRumble(byte big, byte small)
        {
            Console.WriteLine("Xinput rumble: big=" + big + " small=" + small);
            if (this.OnRumble != null)
            {
                OnRumble(big > Settings.Default.xinput_rumbleThreshold_big || small > Settings.Default.xinput_rumbleThreshold_small);
            }
        }

        private string supportedSpecialCodes = "PointerToggle TouchMaster TouchSlave NextLayout";

        internal void updateAccelerometer(AccelState accelState)
        {

            JToken key = this.jsonObj.GetValue("AccelX");
            if (key != null)
            {
                string handle = key.ToString().ToLower();

                if (handle.Length > 4 && handle.Substring(0, 4).Equals("360."))
                {
                    this.xinputUpdateAnalog(handle.Substring(4), accelState.Values.X * -0.5 + 0.5);
                }
            }

            key = this.jsonObj.GetValue("AccelY");
            if (key != null)
            {
                string handle = key.ToString().ToLower();

                if (handle.Length > 4 && handle.Substring(0, 4).Equals("360."))
                {
                    this.xinputUpdateAnalog(handle.Substring(4), accelState.Values.Y * -0.5 + 0.5);
                }
            }

            key = this.jsonObj.GetValue("AccelZ");
            if (key != null)
            {
                string handle = key.ToString().ToLower();

                if (handle.Length > 4 && handle.Substring(0, 4).Equals("360."))
                {
                    this.xinputUpdateAnalog(handle.Substring(4), accelState.Values.Z * -0.5 + 0.5);
                }
            }
        }

        public void updateNunchuk(NunchukState nunchuk)
        {
            JToken key = this.jsonObj.GetValue("Nunchuk.StickX");
            if (key != null)
            {
                string handle = key.ToString().ToLower();

                if (handle.Length > 4 && handle.Substring(0, 4).Equals("360."))
                {
                    this.xinputUpdateAnalog(handle.Substring(4), nunchuk.Joystick.X + 0.5);
                }
            }

            key = this.jsonObj.GetValue("Nunchuk.StickY");
            if (key != null)
            {
                string handle = key.ToString().ToLower();

                if (handle.Length > 4 && handle.Substring(0, 4).Equals("360."))
                {
                    this.xinputUpdateAnalog(handle.Substring(4), nunchuk.Joystick.Y + 0.5);
                }
            }
        }

        public void updateClassicController(ClassicControllerState classic)
        {
            JToken key = this.jsonObj.GetValue("Classic.StickLX");
            if (key != null)
            {
                string handle = key.ToString().ToLower();

                if (handle.Length > 4 && handle.Substring(0, 4).Equals("360."))
                {
                    this.xinputUpdateAnalog(handle.Substring(4), classic.JoystickL.X + 0.5);
                }
            }

            key = this.jsonObj.GetValue("Classic.StickLY");
            if (key != null)
            {
                string handle = key.ToString().ToLower();

                if (handle.Length > 4 && handle.Substring(0, 4).Equals("360."))
                {
                    this.xinputUpdateAnalog(handle.Substring(4), classic.JoystickL.Y + 0.5);
                }
            }

            key = this.jsonObj.GetValue("Classic.StickRX");
            if (key != null)
            {
                string handle = key.ToString().ToLower();

                if (handle.Length > 4 && handle.Substring(0, 4).Equals("360."))
                {
                    this.xinputUpdateAnalog(handle.Substring(4), classic.JoystickR.X + 0.5);
                }
            }

            key = this.jsonObj.GetValue("Classic.StickRY");
            if (key != null)
            {
                string handle = key.ToString().ToLower();

                if (handle.Length > 4 && handle.Substring(0, 4).Equals("360."))
                {
                    this.xinputUpdateAnalog(handle.Substring(4), classic.JoystickR.Y + 0.5);
                }
            }

            key = this.jsonObj.GetValue("Classic.TriggerL");
            if (key != null)
            {
                string handle = key.ToString().ToLower();

                if (handle.Length > 4 && handle.Substring(0, 4).Equals("360."))
                {
                    this.xinputUpdateAnalog(handle.Substring(4), classic.TriggerL);
                }
            }

            key = this.jsonObj.GetValue("Classic.TriggerR");
            if (key != null)
            {
                string handle = key.ToString().ToLower();

                if (handle.Length > 4 && handle.Substring(0, 4).Equals("360."))
                {
                    this.xinputUpdateAnalog(handle.Substring(4), classic.TriggerR);
                }
            }
        }

        public void executeButtonUp(WiimoteButton button)
        {
            this.executeButtonUp(button.ToString());//ToString converts WiimoteButton.A to "A" for instance
        }

        public void executeButtonUp(NunchukButton button)
        {
            this.executeButtonUp("Nunchuk." + button.ToString());
        }

        public void executeButtonUp(ClassicControllerButton button)
        {
            this.executeButtonUp("Classic."+button.ToString());//ToString converts WiimoteButton.A to "A" for instance
        }

        public void executeButtonUp(string button)
        {
            bool handled = false;

            JToken key = this.jsonObj.GetValue(button);

            if (key != null)
            {
                if (key.Values().Count() > 0)
                {

                    foreach (JToken token in key.Values<JToken>())
                    {
                        if (token.ToString().Length > 4 && token.ToString().ToLower().Substring(0, 4).Equals("360."))
                        {
                            this.xinputButtonUp(key.ToString().ToLower().Substring(4));
                            handled = true;
                        }
                    }

                    if (!handled)
                    {
                        IEnumerable<JToken> array = key.Values<JToken>();

                        List<VirtualKeyCode> modifiers = new List<VirtualKeyCode>();

                        for (int i = 0; i < array.Count() - 1; i++)
                        {
                            if (Enum.IsDefined(typeof(VirtualKeyCode), array.ElementAt(i).ToString().ToUpper()))
                            {
                                modifiers.Add((VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), array.ElementAt(i).ToString(), true));
                            }
                        }
                        VirtualKeyCode actionKey = 0;
                        if (Enum.IsDefined(typeof(VirtualKeyCode), array.Last().ToString().ToUpper()))
                        {
                            actionKey = (VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), array.Last().ToString(), true);
                        }

                        if (modifiers.Count() > 0 && actionKey != 0)
                        {
                            this.inputSimulator.Keyboard.ModifiedKeyStroke(modifiers, actionKey);
                            handled = true;
                        }
                    }
                }
                else if (key.ToString().Length > 4 && key.ToString().ToLower().Substring(0, 4).Equals("360."))
                {
                    this.xinputButtonUp(key.ToString().ToLower().Substring(4));
                    handled = true;
                }
                else if (Enum.IsDefined(typeof(VirtualKeyCode), key.ToString().ToUpper())) //Enum.Parse does the opposite...
                {
                    this.inputSimulator.Keyboard.KeyUp((VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), key.ToString(), true));
                    handled = true;
                }
                else if (Enum.IsDefined(typeof(MouseCode), key.ToString().ToUpper()))
                {
                    MouseCode mouseCode = (MouseCode)Enum.Parse(typeof(MouseCode), key.ToString(), true);
                    switch (mouseCode)
                    {
                        case MouseCode.MOUSELEFT:
                            this.inputSimulator.Mouse.LeftButtonUp();
                            handled = true;
                            break;
                        case MouseCode.MOUSERIGHT:
                            this.inputSimulator.Mouse.RightButtonUp();
                            handled = true;
                            break;
                    }
                }
                else if (!supportedSpecialCodes.ToLower().Contains(key.ToString().ToLower())) //If we can not find any valid key code, just treat it as a string to type :P (Good if the user writes X instead of VK_X)
                {
                    this.inputSimulator.Keyboard.TextEntry(key.ToString());
                }

                OnButtonUp(new WiiButtonEvent(key.ToString(), button, handled));
            }

        }

        public void executeButtonDown(WiimoteButton button)
        {
            this.executeButtonDown(button.ToString());
        }

        public void executeButtonDown(NunchukButton button)
        {
            this.executeButtonDown("Nunchuk." + button.ToString());
        }

        public void executeButtonDown(ClassicControllerButton button)
        {
            this.executeButtonDown("Classic." + button.ToString());
        }

        public void executeButtonDown(string button)
        {
            bool handled = false;

            JToken key = this.jsonObj.GetValue(button);
            if (key != null)
            {
                if (key.Values().Count() > 1)
                {
                    foreach (JToken token in key.Values<JToken>())
                    {
                        if (token.ToString().Length > 4 && token.ToString().ToLower().Substring(0, 4).Equals("360."))
                        {
                            this.xinputButtonDown(key.ToString().ToLower().Substring(4));
                            handled = true;
                        }
                    }
                }
                else if (key.ToString().Length > 4 && key.ToString().ToLower().Substring(0, 4).Equals("360."))
                {
                    this.xinputButtonDown(key.ToString().ToLower().Substring(4));
                    handled = true;
                }
                else if (Enum.IsDefined(typeof(VirtualKeyCode), key.ToString().ToUpper()))
                {
                    VirtualKeyCode theKeyCode = (VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), key.ToString(), true);
                    if (theKeyCode == VirtualKeyCode.VOLUME_UP || theKeyCode == VirtualKeyCode.VOLUME_DOWN)
                    {
                        this.inputSimulator.Keyboard.KeyDown(theKeyCode);
                        this.inputSimulator.Keyboard.KeyUp(theKeyCode);
                        this.inputSimulator.Keyboard.KeyDown(theKeyCode);
                        this.inputSimulator.Keyboard.KeyUp(theKeyCode);
                        this.inputSimulator.Keyboard.KeyDown(theKeyCode);
                    }
                    else
                    {
                        this.inputSimulator.Keyboard.KeyDown(theKeyCode);
                    }
                    handled = true;
                }
                else if (Enum.IsDefined(typeof(MouseCode), key.ToString().ToUpper()))
                {
                    MouseCode mouseCode = (MouseCode)Enum.Parse(typeof(MouseCode), key.ToString(), true);
                    switch (mouseCode)
                    {
                        case MouseCode.MOUSELEFT:
                            this.inputSimulator.Mouse.LeftButtonDown();
                            handled = true;
                            break;
                        case MouseCode.MOUSERIGHT:
                            this.inputSimulator.Mouse.RightButtonDown();
                            handled = true;
                            break;
                    }

                }

                OnButtonDown(new WiiButtonEvent(key.ToString(), button, handled));
            }
        }

        public void xinputUpdateAnalog(string handle, double value)
        {
            switch (handle)
            {
                case "sticklx":
                    XinputReport.StickLX = value;
                    break;
                case "stickly":
                    XinputReport.StickLY = value;
                    break;
                case "stickrx":
                    XinputReport.StickRX = value;
                    break;
                case "stickry":
                    XinputReport.StickRY = value;
                    break;
                case "triggerr":
                    this.XinputReport.TriggerR = value;
                    break;
                case "triggerl":
                    this.XinputReport.TriggerL = value;
                    break;
            }
        }

        public void xinputButtonUp(string button)
        {
            switch (button)
            {
                case "triggerr":
                    this.XinputReport.TriggerR = 0.0;
                    break;
                case "triggerl":
                    this.XinputReport.TriggerL = 0.0;
                    break;
                case "a":
                    this.XinputReport.A = false;
                    break;
                case "b":
                    this.XinputReport.B = false;
                    break;
                case "x":
                    this.XinputReport.X = false;
                    break;
                case "y":
                    this.XinputReport.Y = false;
                    break;
                case "back":
                    this.XinputReport.Back = false;
                    break;
                case "start":
                    this.XinputReport.Start = false;
                    break;
                case "stickpressl":
                    this.XinputReport.StickPressL = false;
                    break;
                case "stickpressr":
                    this.XinputReport.StickPressR = false;
                    break;
                case "up":
                    this.XinputReport.Up = false;
                    break;
                case "down":
                    this.XinputReport.Down = false;
                    break;
                case "right":
                    this.XinputReport.Right = false;
                    break;
                case "left":
                    this.XinputReport.Left = false;
                    break;
                case "guide":
                    this.XinputReport.Guide = false;
                    break;
                case "bumperl":
                    this.XinputReport.BumperL = false;
                    break;
                case "bumperr":
                    this.XinputReport.BumperR = false;
                    break;
                case "stickrx+":
                    this.XinputReport.StickRX = 0.5;
                    break;
                case "stickry+":
                    this.XinputReport.StickRY = 0.5;
                    break;
                case "sticklx+":
                    this.XinputReport.StickLX = 0.5;
                    break;
                case "stickly+":
                    this.XinputReport.StickLY = 0.5;
                    break;
                case "stickrx-":
                    this.XinputReport.StickRX = 0.5;
                    break;
                case "stickry-":
                    this.XinputReport.StickRY = 0.5;
                    break;
                case "sticklx-":
                    this.XinputReport.StickLX = 0.5;
                    break;
                case "stickly-":
                    this.XinputReport.StickLY = 0.5;
                    break;
            }
        }

        public void xinputButtonDown(string button)
        {
            switch (button)
            {
                case "triggerr":
                    this.XinputReport.TriggerR = 1.0;
                    break;
                case "triggerl":
                    this.XinputReport.TriggerL = 1.0;
                    break;
                case "a":
                    this.XinputReport.A = true;
                    break;
                case "b":
                    this.XinputReport.B = true;
                    break;
                case "x":
                    this.XinputReport.X = true;
                    break;
                case "y":
                    this.XinputReport.Y = true;
                    break;
                case "back":
                    this.XinputReport.Back = true;
                    break;
                case "start":
                    this.XinputReport.Start = true;
                    break;
                case "stickpressl":
                    this.XinputReport.StickPressL = true;
                    break;
                case "stickpressr":
                    this.XinputReport.StickPressR = true;
                    break;
                case "up":
                    this.XinputReport.Up = true;
                    break;
                case "down":
                    this.XinputReport.Down = true;
                    break;
                case "right":
                    this.XinputReport.Right = true;
                    break;
                case "left":
                    this.XinputReport.Left = true;
                    break;
                case "guide":
                    this.XinputReport.Guide = true;
                    break;
                case "bumperl":
                    this.XinputReport.BumperL = true;
                    break;
                case "bumperr":
                    this.XinputReport.BumperR = true;
                    break;
                case "stickrx+":
                    this.XinputReport.StickRX = 1.0;
                    break;
                case "stickry+":
                    this.XinputReport.StickRY = 0.0;
                    break;
                case "sticklx+":
                    this.XinputReport.StickLX = 1.0;
                    break;
                case "stickly+":
                    this.XinputReport.StickLY = 0.0;
                    break;
                case "stickrx-":
                    this.XinputReport.StickRX = 0.0;
                    break;
                case "stickry-":
                    this.XinputReport.StickRY = 1.0;
                    break;
                case "sticklx-":
                    this.XinputReport.StickLX = 0.0;
                    break;
                case "stickly-":
                    this.XinputReport.StickLY = 1.0;
                    break;
            }
        }
    }

    public class WiiButtonEvent
    {
        public bool Handled = false;
        public string Action = "";
        public string Button;

        public WiiButtonEvent(string action, string button, bool handled = false)
        {
            this.Action = action;
            this.Button = button;
            this.Handled = handled;
        }

    }

    public class WiiKeyMapConfigChangedEvent
    {
        public string Name;
        public string Filename;
        public string Pointer;

        public WiiKeyMapConfigChangedEvent(string name, string filename, string pointer)
        {
            this.Name = name;
            this.Filename = filename;
            this.Pointer = pointer;
        }
    }
}
