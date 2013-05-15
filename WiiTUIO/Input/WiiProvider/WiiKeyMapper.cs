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
using VJoylib;
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

    class WiiKeyMapper
    {

        private string KEYMAPS_PATH = "Keymaps\\";
        private string APPLICATIONS_JSON_FILENAME = "Applications.json";
        private string DEFAULT_JSON_FILENAME = "default.json";

        public WiiKeyMap KeyMap;
        public ButtonState PressedButtons;

        private SystemProcessMonitor processMonitor;

        private JObject applicationsJson;
        private JObject defaultKeymapJson;

        private int wiimoteID;

        public WiiKeyMapper(int wiimoteID)
        {
            this.wiimoteID = wiimoteID;

            PressedButtons = new ButtonState();

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

            this.KeyMap = new WiiKeyMap(this.defaultKeymapJson);

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

            if(wiimoteState.Extension && wiimoteState.ExtensionType == ExtensionType.Nunchuk)
            {
                this.KeyMap.updateNunchuck(wiimoteState.NunchukState);
                significant = true;
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

        public string Pointer;

        private InputSimulator inputSimulator;

        private VJoy vjoycontrol;

        public WiiKeyMap(JObject jsonObj)
        {
            this.jsonObj = jsonObj;

            this.Pointer = this.jsonObj.GetValue("Pointer").ToString();

            this.inputSimulator = new InputSimulator();

            this.vjoycontrol = new VJoy();
            this.vjoycontrol.Initialize();
            this.vjoycontrol.Reset();
        }

        private string supportedSpecialCodes = "PointerToggle TouchMaster TouchSlave";

        public void updateNunchuck(NunchukState nunchuk)
        {
            double axisx = nunchuk.Joystick.X * 32768 * 2;
            short joyx = (short)axisx;
            joyx = axisx > 32767 ? (short)32767 : joyx;
            joyx = axisx < -32768 ? (short)-32768 : joyx;
            double axisy = nunchuk.Joystick.Y * -32768 * 2;
            short joyy = (short)axisy;
            joyy = axisy > 32767 ? (short)32767 : joyy;
            joyy = axisy < -32768 ? (short)-32768 : joyy;
            this.vjoycontrol.SetXAxis(0,joyx);
            this.vjoycontrol.SetYAxis(0,joyy);
            this.vjoycontrol.Update(0);
            //this.inputSimulator.Mouse.MoveMouseBy((int)(nunchuk.Joystick.X*10),-(int)(nunchuk.Joystick.Y*10));
            //Console.WriteLine("Nunchuk RAW : " + nunchuk.RawJoystick);
            //Console.WriteLine("Nunchuk : " + nunchuk.Joystick);
        }

        public void executeButtonUp(WiimoteButton button)
        {
            bool handled = false;

            JToken key = this.jsonObj.GetValue(button.ToString()); //ToString converts WiimoteButton.A to "A" for instance

            if (key != null)
            {
                if (Enum.IsDefined(typeof(VirtualKeyCode), key.ToString().ToUpper())) //Enum.Parse does the opposite...
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
            bool handled = false;

            JToken key = this.jsonObj.GetValue(button.ToString());
            if (key != null)
            {
                if (Enum.IsDefined(typeof(VirtualKeyCode), key.ToString().ToUpper()))
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
    }

    public class WiiButtonEvent
    {
        public bool Handled = false;
        public string Action = "";
        public WiimoteButton Button;

        public WiiButtonEvent(string action, WiimoteButton button, bool handled = false)
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
