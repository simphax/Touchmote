using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WiimoteLib;
using WindowsInput;
using WindowsInput.Native;

namespace WiiTUIO.Provider
{
    public enum WiimoteButton
    {
        Up,
        Down,
        Left,
        Right,
        Home,
        Plus,
        Minus,
        One,
        Two,
        A,
        B
    }

    public struct NunchukButtonState
    {
        public bool C;
        public bool Z;
    }

    class WiiKeyMapper
    {

        private string KEYMAPS_PATH = "Keymaps\\";
        private string APPLICATIONS_JSON_FILENAME = "Applications.json";
        private string DEFAULT_JSON_FILENAME = "default.json";

        public WiiKeyMap KeyMap;
        public ButtonState PressedButtons;
        public NunchukButtonState NunchukPressedButtons;

        private SystemProcessMonitor processMonitor;

        private JObject applicationsJson;
        private JObject defaultKeymapJson;

        private int wiimoteID;

        public WiiKeyMapper(int wiimoteID)
        {
            this.wiimoteID = wiimoteID;

            PressedButtons = new ButtonState();
            NunchukPressedButtons = new NunchukButtonState();

            System.IO.Directory.CreateDirectory(KEYMAPS_PATH);
            this.applicationsJson = this.createDefaultApplicationsJSON();
            this.defaultKeymapJson = this.createDefaultKeymapJSON();

            JObject specificKeymap = new JObject();
            JObject commonKeymap = new JObject();

            if (this.defaultKeymapJson.GetValue(this.wiimoteID.ToString()) != null)
            {
                specificKeymap = (JObject)this.defaultKeymapJson.GetValue(this.wiimoteID.ToString());
            }
            if (this.defaultKeymapJson.GetValue("All") != null)
            {
                commonKeymap = (JObject)this.defaultKeymapJson.GetValue("All");
            }

            MergeJSON(commonKeymap, specificKeymap);
            this.defaultKeymapJson = commonKeymap;

            this.KeyMap = new WiiKeyMap(this.defaultKeymapJson, new XinputDevice(XinputBus.Default, wiimoteID), new XinputReport(wiimoteID));

            this.processMonitor = SystemProcessMonitor.getInstance();

            this.processMonitor.ProcessChanged += processChanged;
        }

        private void processChanged(ProcessChangedEvent evt)
        {
            try
            {
                string appStringToMatch = evt.Process.MainModule.FileVersionInfo.FileDescription + evt.Process.MainModule.FileVersionInfo.OriginalFilename + evt.Process.MainModule.FileVersionInfo.FileName;

                bool keymapFound = false;

                IEnumerable<JObject> applicationConfigurations = this.applicationsJson.GetValue("Applications").Children<JObject>();
                foreach (JObject configuration in applicationConfigurations)
                {
                    string appName = configuration.GetValue("Name").ToString();

                    if (appStringToMatch.ToLower().Replace(" ", "").Contains(appName.ToLower().Replace(" ", "")))
                    {
                        this.loadKeyMap(KEYMAPS_PATH + configuration.GetValue("Keymap").ToString());
                        keymapFound = true;
                    }
                    
                }
                if (!keymapFound)
                {
                    this.KeyMap.JsonObj = this.defaultKeymapJson;
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Could not change keymap config for " + evt.Process);
            }
        }

        private void keyMap_onButtonUp(WiiButtonEvent evt)
        {
            
        }

        private void keyMap_onButtonDown(WiiButtonEvent evt)
        {
            
        }


        private JObject createDefaultApplicationsJSON()
        {
            JArray applications = new JArray();

            JObject applicationList =
                new JObject(
                    new JProperty("Applications",
                        applications),
                    new JProperty("Default", DEFAULT_JSON_FILENAME)
            );

            JObject union = applicationList;

            if (File.Exists(KEYMAPS_PATH + APPLICATIONS_JSON_FILENAME))
            {
                StreamReader reader = File.OpenText(KEYMAPS_PATH + APPLICATIONS_JSON_FILENAME);
                try
                {
                    JObject existingConfig = (JObject)JToken.ReadFrom(new JsonTextReader(reader));
                    reader.Close();

                    MergeJSON(union, existingConfig);
                }
                catch (Exception e) 
                {
                    throw new Exception(KEYMAPS_PATH + APPLICATIONS_JSON_FILENAME + " is not valid JSON");
                }
            }
            
            File.WriteAllText(KEYMAPS_PATH + APPLICATIONS_JSON_FILENAME, union.ToString());
            return union;
        }

        private JObject createDefaultKeymapJSON()
        {
            JObject buttons = new JObject();

            buttons.Add(new JProperty("Pointer", "Touch"));

            buttons.Add(new JProperty("A", "TouchMaster"));

            buttons.Add(new JProperty("B", "TouchSlave"));

            buttons.Add(new JProperty("Home", "LWin"));

            buttons.Add(new JProperty("Left", "Left"));
            buttons.Add(new JProperty("Right", "Right"));
            buttons.Add(new JProperty("Up", "Up"));
            buttons.Add(new JProperty("Down", "Down"));

            buttons.Add(new JProperty("Plus", "Volume_Up"));

            buttons.Add(new JProperty("Minus", "Volume_Down"));

            buttons.Add(new JProperty("One", "PointerToggle"));

            JArray buttonTwo = new JArray();
            buttonTwo.Add(new JValue("LWin"));
            buttonTwo.Add(new JValue("Tab"));
            buttons.Add(new JProperty("Two", buttonTwo));

            JObject allButtons = new JObject();
            allButtons.Add(new JProperty("All", buttons));

            JObject union = allButtons;

            if (File.Exists(KEYMAPS_PATH + DEFAULT_JSON_FILENAME))
            {
                StreamReader reader = File.OpenText(KEYMAPS_PATH + DEFAULT_JSON_FILENAME);
                try
                {
                    JObject existingConfig = (JObject)JToken.ReadFrom(new JsonTextReader(reader));
                    reader.Close();

                    MergeJSON(union, existingConfig);
                }
                catch (Exception e)
                {
                    throw new Exception(KEYMAPS_PATH + DEFAULT_JSON_FILENAME + " is not valid JSON");
                }
            }
            File.WriteAllText(KEYMAPS_PATH + DEFAULT_JSON_FILENAME, union.ToString());
            return union;
        }

        private static void MergeJSON(JObject receiver, JObject donor)
        {
            foreach (var property in donor)
            {
                JObject receiverValue = receiver[property.Key] as JObject;
                JObject donorValue = property.Value as JObject;
                if (receiverValue != null && donorValue != null)
                    MergeJSON(receiverValue, donorValue);
                else
                    receiver[property.Key] = property.Value;
            }
        }

        public void loadKeyMap(string path)
        {

            JObject union = (JObject)this.defaultKeymapJson.DeepClone();

            if (File.Exists(path))
            {
                StreamReader reader = File.OpenText(path);
                try
                {
                    JObject newKeymap = (JObject)JToken.ReadFrom(new JsonTextReader(reader));
                    reader.Close();

                    JObject specificKeymap = new JObject();
                    JObject commonKeymap = new JObject();

                    if (newKeymap.GetValue(this.wiimoteID.ToString()) != null)
                    {
                        specificKeymap = (JObject)newKeymap.GetValue(this.wiimoteID.ToString());
                    }
                    if (newKeymap.GetValue("All") != null)
                    {
                        commonKeymap = (JObject)newKeymap.GetValue("All");
                    }

                    MergeJSON(commonKeymap, specificKeymap);

                    MergeJSON(union, commonKeymap);
                }
                catch (Exception e)
                {
                    throw new Exception(path + " is not valid JSON");
                }
            }

            this.KeyMap.JsonObj = union;

            this.processWiimoteState(new WiimoteState()); //Sets all buttons to "not pressed"

            Console.WriteLine("Loaded new keymap on " + path);
        }

        public bool processWiimoteState(WiimoteState wiimoteState) //Returns true if anything happened.
        {
            ButtonState buttonState = wiimoteState.ButtonState;
            bool significant = false;

            this.KeyMap.updateAccelerometer(wiimoteState.AccelState);

            if(wiimoteState.Extension && wiimoteState.ExtensionType == ExtensionType.Nunchuk)
            {
                this.KeyMap.updateNunchuk(wiimoteState.NunchukState);

                if (wiimoteState.NunchukState.C && !NunchukPressedButtons.C)
                {
                    this.KeyMap.executeButtonDown("Nunchuk.C");
                    NunchukPressedButtons.C = true;
                    significant = true;
                }
                else if (!wiimoteState.NunchukState.C && NunchukPressedButtons.C)
                {
                    this.KeyMap.executeButtonUp("Nunchuk.C");
                    NunchukPressedButtons.C = false;
                    significant = true;
                }

                if (wiimoteState.NunchukState.Z && !NunchukPressedButtons.Z)
                {
                    this.KeyMap.executeButtonDown("Nunchuk.Z");
                    NunchukPressedButtons.Z = true;
                    significant = true;
                }
                else if (!wiimoteState.NunchukState.Z && NunchukPressedButtons.Z)
                {
                    this.KeyMap.executeButtonUp("Nunchuk.Z");
                    NunchukPressedButtons.Z = false;
                    significant = true;
                }
            }

            if (buttonState.A && !PressedButtons.A)
            {
                this.KeyMap.executeButtonDown(WiimoteButton.A);
                PressedButtons.A = true;
                significant = true;
            }
            else if (!buttonState.A && PressedButtons.A)
            {
                this.KeyMap.executeButtonUp(WiimoteButton.A);
                PressedButtons.A = false;
                significant = true;
            }

            if (buttonState.B && !PressedButtons.B)
            {
                this.KeyMap.executeButtonDown(WiimoteButton.B);
                PressedButtons.B = true;
                significant = true;
            }
            else if (!buttonState.B && PressedButtons.B)
            {
                this.KeyMap.executeButtonUp(WiimoteButton.B);
                PressedButtons.B = false;
                significant = true;
            }

            if (buttonState.Up && !PressedButtons.Up)
            {
                this.KeyMap.executeButtonDown(WiimoteButton.Up);
                PressedButtons.Up = true;
                significant = true;
            }
            else if (!buttonState.Up && PressedButtons.Up)
            {
                this.KeyMap.executeButtonUp(WiimoteButton.Up);
                PressedButtons.Up = false;
                significant = true;
            }

            if (buttonState.Down && !PressedButtons.Down)
            {
                this.KeyMap.executeButtonDown(WiimoteButton.Down);
                PressedButtons.Down = true;
                significant = true;
            }
            else if (!buttonState.Down && PressedButtons.Down)
            {
                this.KeyMap.executeButtonUp(WiimoteButton.Down);
                PressedButtons.Down = false;
                significant = true;
            }

            if (buttonState.Left && !PressedButtons.Left)
            {
                this.KeyMap.executeButtonDown(WiimoteButton.Left);
                PressedButtons.Left = true;
                significant = true;
            }
            else if (!buttonState.Left && PressedButtons.Left)
            {
                this.KeyMap.executeButtonUp(WiimoteButton.Left);
                PressedButtons.Left = false;
                significant = true;
            }

            if (buttonState.Right && !PressedButtons.Right)
            {
                this.KeyMap.executeButtonDown(WiimoteButton.Right);
                PressedButtons.Right = true;
                significant = true;
            }
            else if (!buttonState.Right && PressedButtons.Right)
            {
                this.KeyMap.executeButtonUp(WiimoteButton.Right);
                PressedButtons.Right = false;
                significant = true;
            }

            if (buttonState.Home && !PressedButtons.Home)
            {
                this.KeyMap.executeButtonDown(WiimoteButton.Home);
                PressedButtons.Home = true;
                significant = true;
            }
            else if (!buttonState.Home && PressedButtons.Home)
            {
                this.KeyMap.executeButtonUp(WiimoteButton.Home);
                PressedButtons.Home = false;
                significant = true;
            }

            if (buttonState.Plus && !PressedButtons.Plus)
            {
                this.KeyMap.executeButtonDown(WiimoteButton.Plus);
                PressedButtons.Plus = true;
                significant = true;
            }
            else if (PressedButtons.Plus && !buttonState.Plus)
            {
                this.KeyMap.executeButtonUp(WiimoteButton.Plus);
                PressedButtons.Plus = false;
                significant = true;
            }

            if (buttonState.Minus && !PressedButtons.Minus)
            {
                this.KeyMap.executeButtonDown(WiimoteButton.Minus);
                PressedButtons.Minus = true;
                significant = true;
            }
            else if (PressedButtons.Minus && !buttonState.Minus)
            {
                this.KeyMap.executeButtonUp(WiimoteButton.Minus);
                PressedButtons.Minus = false;
                significant = true;
            }

            if (buttonState.One && !PressedButtons.One)
            {
                this.KeyMap.executeButtonDown(WiimoteButton.One);
                PressedButtons.One = true;
                significant = true;
            }
            else if (PressedButtons.One && !buttonState.One)
            {
                this.KeyMap.executeButtonUp(WiimoteButton.One);
                PressedButtons.One = false;
                significant = true;
            }

            if (buttonState.Two && !PressedButtons.Two)
            {
                this.KeyMap.executeButtonDown(WiimoteButton.Two);
                PressedButtons.Two = true;
                significant = true;
            }
            else if (PressedButtons.Two && !buttonState.Two)
            {
                this.KeyMap.executeButtonUp(WiimoteButton.Two);
                PressedButtons.Two = false;
                significant = true;
            }

            this.KeyMap.XinputDevice.Update(this.KeyMap.XinputReport);

            return significant;
        }
    }

    public enum MouseCode
    {
        MOUSELEFT,
        MOUSERIGHT
    }

    

    public class WiiKeyMap
    {
        private JObject jsonObj;

        public JObject JsonObj
        {
            get { return this.jsonObj; }
            set
            {
                if (this.JsonObj != value)
                {
                    this.jsonObj = value;
                    this.Pointer = this.jsonObj.GetValue("Pointer").ToString();
                    if (this.Pointer != null && this.OnConfigChanged != null)
                    {
                        this.OnConfigChanged(new WiiKeyMapConfigChangedEvent(this.Pointer));
                    }
                }
            }
        }

        public Action<WiiButtonEvent> OnButtonUp;
        public Action<WiiButtonEvent> OnButtonDown;
        public Action<WiiKeyMapConfigChangedEvent> OnConfigChanged;
        public Action<double> OnRumble;

        public string Pointer;

        private InputSimulator inputSimulator;

        public XinputDevice XinputDevice;
        public XinputReport XinputReport;

        public WiiKeyMap(JObject jsonObj, XinputDevice xinput, XinputReport xinputReport)
        {
            this.jsonObj = jsonObj;

            this.Pointer = this.jsonObj.GetValue("Pointer").ToString();

            this.inputSimulator = new InputSimulator();
            this.XinputDevice = xinput;
            this.XinputReport = xinputReport;
            xinput.OnRumble += Xinput_OnRumble;
        }

        private void Xinput_OnRumble(byte big, byte small)
        {
            Console.WriteLine("Xinput rumble: big=" + big + " small=" + small);
            if (this.OnRumble != null)
            {
                OnRumble(((double)big)/255.0);
            }
        }

        private string supportedSpecialCodes = "PointerToggle TouchMaster TouchSlave";

        internal void updateAccelerometer(AccelState accelState)
        {
            JToken key = this.jsonObj.GetValue("SteeringWheel");
            if (key != null)
            {
                switch (key.ToString().ToLower())
                {
                    case "360.sticklx":
                        XinputReport.StickLX = accelState.Values.Y * -0.5 + 0.5;
                        break;
                    case "360.stickly":
                        XinputReport.StickLY = accelState.Values.Y * -0.5 + 0.5;
                        break;
                    case "360.stickrx":
                        XinputReport.StickRX = accelState.Values.Y * -0.5 + 0.5;
                        break;
                    case "360.stickry":
                        XinputReport.StickRY = accelState.Values.Y * -0.5 + 0.5;
                        break;
                }
            }
        }

        public void updateNunchuk(NunchukState nunchuk)
        {
            JToken key = this.jsonObj.GetValue("Nunchuk.StickX");
            if (key != null)
            {
                switch (key.ToString().ToLower())
                {
                    case "360.sticklx":
                        XinputReport.StickLX = nunchuk.Joystick.X + 0.5;
                        break;
                    case "360.stickly":
                        XinputReport.StickLY = nunchuk.Joystick.X + 0.5;
                        break;
                    case "360.stickrx":
                        XinputReport.StickRX = nunchuk.Joystick.X + 0.5;
                        break;
                    case "360.stickry":
                        XinputReport.StickRY = nunchuk.Joystick.X + 0.5;
                        break;
                }
            }
            
            key = this.jsonObj.GetValue("Nunchuk.StickY");
            if (key != null)
            {
                switch (key.ToString().ToLower())
                {
                    case "360.sticklx":
                        XinputReport.StickLX = -nunchuk.Joystick.Y + 0.5;
                        break;
                    case "360.stickly":
                        XinputReport.StickLY = -nunchuk.Joystick.Y + 0.5;
                        break;
                    case "360.stickrx":
                        XinputReport.StickRX = -nunchuk.Joystick.Y + 0.5;
                        break;
                    case "360.stickry":
                        XinputReport.StickRY = -nunchuk.Joystick.Y + 0.5;
                        break;
                }
            }
            
            //this.inputSimulator.Mouse.MoveMouseBy((int)(nunchuk.Joystick.X*10),-(int)(nunchuk.Joystick.Y*10));
            //Console.WriteLine("Nunchuk RAW : " + nunchuk.RawJoystick);
            //Console.WriteLine("Nunchuk : " + nunchuk.Joystick);
        }

        public void executeButtonUp(WiimoteButton button)
        {
            this.executeButtonUp(button.ToString());//ToString converts WiimoteButton.A to "A" for instance
        }

        public void executeButtonUp(string button)
        {

            Console.WriteLine("button up"+button);
            bool handled = false;

            JToken key = this.jsonObj.GetValue(button); 

            if (key != null)
            {
                if (key.ToString().Length > 4 && key.ToString().ToLower().Substring(0, 4).Equals("360."))
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
                else if (key.Values().Count() > 0)
                {
                    IEnumerable<JToken> array = key.Values<JToken>();

                    List<VirtualKeyCode> modifiers = new List<VirtualKeyCode>();

                    for (int i = 0; i < array.Count()-1; i++)
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
                else if(!supportedSpecialCodes.ToLower().Contains(key.ToString().ToLower())) //If we can not find any valid key code, just treat it as a string to type :P (Good if the user writes X instead of VK_X)
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

        public void executeButtonDown(string button)
        {
            Console.WriteLine("button down" + button);
            bool handled = false;

            JToken key = this.jsonObj.GetValue(button);
            if (key != null)
            {
                if (key.ToString().Length > 4 && key.ToString().ToLower().Substring(0, 4).Equals("360."))
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
        public string NewPointer;

        public WiiKeyMapConfigChangedEvent(string newPointer)
        {
            this.NewPointer = newPointer;
        }
    }
}
