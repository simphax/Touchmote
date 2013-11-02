using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using WiimoteLib;
using WiiTUIO.Output.Handlers;
using WiiTUIO.Properties;
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

    public enum ClassicControllerButton
    {
        Up,
        Down,
        Left,
        Right,
        Home,
        Plus,
        Minus,
        X,
        Y,
        A,
        B,
        TriggerL,
        TriggerR,
        ZL,
        ZR,
        StickLUp,
        StickLDown,
        StickLLeft,
        StickLRight,
        StickRUp,
        StickRDown,
        StickRLeft,
        StickRRight
        
    }

    public enum NunchukButton
    {
        Z,
        C,
        StickUp,
        StickDown,
        StickLeft,
        StickRight,
    }

    public struct NunchukButtonState
    {
        public bool C;
        public bool Z;
        public bool StickUp;
        public bool StickDown;
        public bool StickLeft;
        public bool StickRight;
    }

    public class WiiKeyMapper
    {
        public Action<WiiButtonEvent> OnButtonUp;
        public Action<WiiButtonEvent> OnButtonDown;
        public Action<WiiKeyMapConfigChangedEvent> OnConfigChanged;
        public Action<bool> OnRumble;

        private string DEFAULT_JSON_FILENAME = "default.json";

        private WiiKeyMap KeyMap;

        private Dictionary<string, bool> PressedButtons = new Dictionary<string, bool>()
        {
            {"A",false},
            {"B",false},
            {"Up",false},
            {"Down",false},
            {"Left",false},
            {"Right",false},
            {"Minus",false},
            {"Plus",false},
            {"Home",false},
            {"One",false},
            {"Two",false},
            {"Nunchuk.C",false},
            {"Nunchuk.Z",false},
            {"Nunchuk.StickUp",false},
            {"Nunchuk.StickDown",false},
            {"Nunchuk.StickLeft",false},
            {"Nunchuk.StickRight",false},
            {"Classic.A",false},
            {"Classic.B",false},
            {"Classic.X",false},
            {"Classic.Y",false},
            {"Classic.Up",false},
            {"Classic.Down",false},
            {"Classic.Left",false},
            {"Classic.Right",false},
            {"Classic.Home",false},
            {"Classic.Plus",false},
            {"Classic.Minus",false},
            {"Classic.L",false},
            {"Classic.R",false},
            {"Classic.ZL",false},
            {"Classic.ZR",false},
            {"Classic.StickLUp",false},
            {"Classic.StickLDown",false},
            {"Classic.StickLLeft",false},
            {"Classic.StickLRight",false},
            {"Classic.StickRUp",false},
            {"Classic.StickRDown",false},
            {"Classic.StickRLeft",false},
            {"Classic.StickRRight",false}
        };

        private SystemProcessMonitor processMonitor;

        private JObject applicationsJson;
        private JObject defaultKeymapJson; //Always default.json
        private JObject fallbackKeymapJson; //Decided by the layout chooser

        private Timer homeButtonTimer;

        private string defaultName;
        private string fallbackName;
        private string fallbackFile;

        public int WiimoteID;
        private bool hideOverlayOnUp = false;

        private List<IButtonHandler> buttonHandlers;

        public WiiKeyMapper(int wiimoteID, HandlerFactory handlerFactory)
        {
            this.WiimoteID = wiimoteID;
            this.buttonHandlers = handlerFactory.getButtonHandlers(this.WiimoteID);
            foreach (IButtonHandler handler in buttonHandlers)
            {
                handler.connect();
            }

            this.initialize();

            this.processMonitor = SystemProcessMonitor.Default;
            this.processMonitor.ProcessChanged += processChanged;
            this.processMonitor.Start();

            homeButtonTimer = new Timer();
            homeButtonTimer.Interval = 1000;
            homeButtonTimer.AutoReset = true;
            homeButtonTimer.Elapsed += homeButtonTimer_Elapsed;

            KeymapConfigWindow.Instance.OnConfigChanged += keymapConfigWindow_OnConfigChanged;
        }

        private void initialize()
        {
            System.IO.Directory.CreateDirectory(Settings.Default.keymaps_path);
            this.applicationsJson = this.loadApplicationsJSON();
            this.defaultKeymapJson = this.loadDefaultKeymapJSON();

            this.defaultName = this.defaultKeymapJson.GetValue("Title").ToString();
            this.fallbackName = this.defaultName;
            this.fallbackFile = DEFAULT_JSON_FILENAME;

            JObject specificKeymap = new JObject();
            JObject commonKeymap = new JObject();

            if (this.defaultKeymapJson.GetValue(this.WiimoteID.ToString()) != null)
            {
                specificKeymap = (JObject)this.defaultKeymapJson.GetValue(this.WiimoteID.ToString());
            }
            if (this.defaultKeymapJson.GetValue("All") != null)
            {
                commonKeymap = (JObject)this.defaultKeymapJson.GetValue("All");
            }

            MergeJSON(commonKeymap, specificKeymap);
            this.defaultKeymapJson = commonKeymap;
            this.fallbackKeymapJson = commonKeymap;

            this.KeyMap = new WiiKeyMap(this.WiimoteID, this.defaultKeymapJson, this.fallbackName, this.fallbackFile, this.buttonHandlers);
            this.KeyMap.OnButtonDown += keyMap_onButtonDown;
            this.KeyMap.OnButtonUp += keyMap_onButtonUp;
            this.KeyMap.OnConfigChanged += keyMap_onConfigChanged;
            this.KeyMap.OnRumble += keyMap_onRumble;

            this.SendConfigChangedEvt();
        }

        private JObject loadApplicationsJSON()
        {
            JObject result = null;
            if (File.Exists(Settings.Default.keymaps_path + Settings.Default.keymaps_config))
            {
                StreamReader reader = File.OpenText(Settings.Default.keymaps_path + Settings.Default.keymaps_config);
                try
                {
                    result = (JObject)JToken.ReadFrom(new JsonTextReader(reader));
                    reader.Close();
                }
                catch (Exception e)
                {
                    throw new Exception(Settings.Default.keymaps_path + Settings.Default.keymaps_config + " is not valid JSON");
                }
            }
            return result;
        }

        private JObject loadDefaultKeymapJSON()
        {
            JObject result = null;
            if (File.Exists(Settings.Default.keymaps_path + DEFAULT_JSON_FILENAME))
            {
                StreamReader reader = File.OpenText(Settings.Default.keymaps_path + DEFAULT_JSON_FILENAME);
                try
                {
                    result = (JObject)JToken.ReadFrom(new JsonTextReader(reader));
                    reader.Close();
                }
                catch (Exception e)
                {
                    throw new Exception(Settings.Default.keymaps_path + DEFAULT_JSON_FILENAME + " is not valid JSON");
                }
            }
            return result;
        }

        private void keymapConfigWindow_OnConfigChanged()
        {
            this.initialize();
        }

        public void Teardown()
        {
            foreach (IButtonHandler handler in buttonHandlers)
            {
                handler.disconnect();
            }
            this.processMonitor.ProcessChanged -= processChanged;
        }

        public IEnumerable<JObject> GetLayoutList()
        {
            return this.applicationsJson.GetValue("LayoutChooser").Children<JObject>();
        }

        public void SetFallbackKeymap(string filename)
        {
            this.loadKeyMap(filename);
            this.fallbackKeymapJson = this.KeyMap.JsonObj;
            this.fallbackName = this.KeyMap.Name;
            this.fallbackFile = this.KeyMap.Filename;
        }

        public void SwitchToDefault()
        {
            this.KeyMap.SetConfig(this.defaultKeymapJson, this.defaultName, DEFAULT_JSON_FILENAME); //Switch to fallback even if we did not choose anything in the chooser.
        }

        public void SwitchToFallback()
        {
            this.KeyMap.SetConfig(this.fallbackKeymapJson, this.fallbackName, this.fallbackFile); //Switch to fallback even if we did not choose anything in the chooser.
        }

        void homeButtonTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (this.PressedButtons["Home"])
            {
                this.KeyMap.SetConfig(this.defaultKeymapJson, "Default", DEFAULT_JSON_FILENAME);
                OverlayWindow.Current.ShowLayoutOverlay(this);
                this.PressedButtons["Home"] = true;
            }
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
                    string search = configuration.GetValue("Search").ToString();

                    if (appStringToMatch.ToLower().Replace(" ", "").Contains(search.ToLower().Replace(" ", "")))
                    {
                        this.loadKeyMap(configuration.GetValue("Keymap").ToString());
                        keymapFound = true;
                    }
                    
                }
                if (!keymapFound)
                {
                    this.KeyMap.SetConfig(this.fallbackKeymapJson,this.fallbackName,this.fallbackFile);
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Could not change keymap config for " + evt.Process);
            }
        }

        private void keyMap_onButtonUp(WiiButtonEvent evt)
        {
            if (this.OnButtonUp != null)
            {
                this.OnButtonUp(evt);
            }
        }

        private void keyMap_onButtonDown(WiiButtonEvent evt)
        {
            if (this.OnButtonDown != null)
            {
                this.OnButtonDown(evt);
            }
        }

        private void keyMap_onConfigChanged(WiiKeyMapConfigChangedEvent evt)
        {
            if (this.OnConfigChanged != null)
            {
                this.OnConfigChanged(evt);
            }
        }

        private void keyMap_onRumble(bool rumble)
        {
            if (this.OnRumble != null)
            {
                this.OnRumble(rumble);
            }
        }

        public void SendConfigChangedEvt()
        {
            this.KeyMap.SendConfigChangedEvt();
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

        public void loadKeyMap(string filename)
        {

            string name = "";

            JObject union = (JObject)this.defaultKeymapJson.DeepClone();

            if (File.Exists(Settings.Default.keymaps_path + filename))
            {
                StreamReader reader = File.OpenText(Settings.Default.keymaps_path + filename);
                try
                {
                    JObject newKeymap = (JObject)JToken.ReadFrom(new JsonTextReader(reader));
                    reader.Close();

                    name = newKeymap.GetValue("Title").ToString();

                    JObject specificKeymap = new JObject();
                    JObject commonKeymap = new JObject();

                    if (newKeymap.GetValue(this.WiimoteID.ToString()) != null)
                    {
                        specificKeymap = (JObject)newKeymap.GetValue(this.WiimoteID.ToString());
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
                    throw new Exception(filename + " is not valid JSON");
                }
            }

            this.KeyMap.SetConfig(union, name, filename);

            this.processWiimoteState(new WiimoteState()); //Sets all buttons to "not pressed"

            Console.WriteLine("Loaded new keymap " + filename);
        }

        public bool processWiimoteState(WiimoteState wiimoteState) //Returns true if anything has changed from last report.
        {
            ButtonState buttonState = wiimoteState.ButtonState;
            bool significant = false;

            this.KeyMap.startUpdate();

            this.KeyMap.updateAccelerometer(wiimoteState.AccelState);

            if(wiimoteState.Extension && wiimoteState.ExtensionType == ExtensionType.Nunchuk)
            {
                this.KeyMap.updateNunchuk(wiimoteState.NunchukState);

                if (wiimoteState.NunchukState.C && !PressedButtons["Nunchuk.C"])
                {
                    PressedButtons["Nunchuk.C"] = true;
                    significant = true;
                    this.KeyMap.executeButtonDown(NunchukButton.C);
                }
                else if (!wiimoteState.NunchukState.C && PressedButtons["Nunchuk.C"])
                {
                    PressedButtons["Nunchuk.C"] = false;
                    significant = true;
                    this.KeyMap.executeButtonUp(NunchukButton.C);
                }

                if (wiimoteState.NunchukState.Z && !PressedButtons["Nunchuk.Z"])
                {
                    PressedButtons["Nunchuk.Z"] = true;
                    significant = true;
                    this.KeyMap.executeButtonDown(NunchukButton.Z);
                }
                else if (!wiimoteState.NunchukState.Z && PressedButtons["Nunchuk.Z"])
                {
                    PressedButtons["Nunchuk.Z"] = false;
                    significant = true;
                    this.KeyMap.executeButtonUp(NunchukButton.Z);
                }

                if (wiimoteState.NunchukState.Joystick.Y > 0.3 && !PressedButtons["Nunchuk.StickUp"])
                {
                    PressedButtons["Nunchuk.StickUp"] = true;
                    significant = true;
                    this.KeyMap.executeButtonDown(NunchukButton.StickUp);
                }
                 else if (wiimoteState.NunchukState.Joystick.Y < 0.3 && PressedButtons["Nunchuk.StickUp"])
                {
                    PressedButtons["Nunchuk.StickUp"] = false;
                    significant = true;
                    this.KeyMap.executeButtonUp(NunchukButton.StickUp);
                }

                 if (wiimoteState.NunchukState.Joystick.Y < -0.3 && !PressedButtons["Nunchuk.StickDown"])
                {
                    PressedButtons["Nunchuk.StickDown"] = true;
                    significant = true;
                    this.KeyMap.executeButtonDown(NunchukButton.StickDown);
                }
                 else if (wiimoteState.NunchukState.Joystick.Y > -0.3 && PressedButtons["Nunchuk.StickDown"])
                {
                    PressedButtons["Nunchuk.StickDown"] = false;
                    significant = true;
                    this.KeyMap.executeButtonUp(NunchukButton.StickDown);
                }

                 if (wiimoteState.NunchukState.Joystick.X < -0.3 && !PressedButtons["Nunchuk.StickLeft"])
                {
                    PressedButtons["Nunchuk.StickLeft"] = true;
                    significant = true;
                    this.KeyMap.executeButtonDown(NunchukButton.StickLeft);
                }
                else if (wiimoteState.NunchukState.Joystick.X > -0.3 && PressedButtons["Nunchuk.StickLeft"])
                {
                    PressedButtons["Nunchuk.StickLeft"] = false;
                    significant = true;
                    this.KeyMap.executeButtonUp(NunchukButton.StickLeft);
                }
                if (wiimoteState.NunchukState.Joystick.X > 0.3 && !PressedButtons["Nunchuk.StickRight"])
                {
                    PressedButtons["Nunchuk.StickRight"] = true;
                    significant = true;
                    this.KeyMap.executeButtonDown(NunchukButton.StickRight);
                }
                else if (wiimoteState.NunchukState.Joystick.X < 0.3 && PressedButtons["Nunchuk.StickRight"])
                {
                    PressedButtons["Nunchuk.StickRight"] = false;
                    significant = true;
                    this.KeyMap.executeButtonUp(NunchukButton.StickRight);
                }
                
            }

            if (wiimoteState.Extension && wiimoteState.ExtensionType == ExtensionType.ClassicController)
            {
                this.KeyMap.updateClassicController(wiimoteState.ClassicControllerState);

                ClassicControllerButtonState classicButtonState = wiimoteState.ClassicControllerState.ButtonState;

                FieldInfo[] cbuttons = classicButtonState.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
                foreach (FieldInfo button in cbuttons)
                {
                    string buttonName = "Classic." + button.Name;
                    if (button.Name == "TriggerL")
                    {
                        buttonName = "Classic.L";
                    }
                    else if (button.Name == "TriggerR")
                    {
                        buttonName = "Classic.R";
                    }

                    bool pressedNow = (bool)button.GetValue(classicButtonState);
                    bool pressedBefore = PressedButtons[buttonName];

                    if (pressedNow && !pressedBefore)
                    {
                        PressedButtons[buttonName] = true;
                        significant = true;
                        this.KeyMap.executeButtonDown(buttonName);
                    }
                    else if (!pressedNow && pressedBefore)
                    {
                        PressedButtons[buttonName] = false;
                        significant = true;
                        this.KeyMap.executeButtonUp(buttonName);
                    }
                }
                if (wiimoteState.ClassicControllerState.JoystickL.Y > 0.3 && !PressedButtons["Classic.StickLUp"])
                {
                    PressedButtons["Classic.StickLUp"] = true;
                    significant = true;
                    this.KeyMap.executeButtonDown(ClassicControllerButton.StickLUp);
                }
                else if (wiimoteState.ClassicControllerState.JoystickL.Y < 0.3 && PressedButtons["Classic.StickLUp"])
                {
                    PressedButtons["Classic.StickLUp"] = false;
                    significant = true;
                    this.KeyMap.executeButtonUp(ClassicControllerButton.StickLUp);
                }

                if (wiimoteState.ClassicControllerState.JoystickL.Y < -0.3 && !PressedButtons["Classic.StickLDown"])
                {
                    PressedButtons["Classic.StickLDown"] = true;
                    significant = true;
                    this.KeyMap.executeButtonDown(ClassicControllerButton.StickLDown);
                }
                else if (wiimoteState.ClassicControllerState.JoystickL.Y > -0.3 && PressedButtons["Classic.StickLDown"])
                {
                    PressedButtons["Classic.StickLDown"] = false;
                    significant = true;
                    this.KeyMap.executeButtonUp(ClassicControllerButton.StickLDown);
                }

                if (wiimoteState.ClassicControllerState.JoystickL.X < -0.3 && !PressedButtons["Classic.StickLLeft"])
                {
                    PressedButtons["Classic.StickLLeft"] = true;
                    significant = true;
                    this.KeyMap.executeButtonDown(ClassicControllerButton.StickLLeft);
                }
                else if (wiimoteState.ClassicControllerState.JoystickL.X > -0.3 && PressedButtons["Classic.StickLLeft"])
                {
                    PressedButtons["Classic.StickLLeft"] = false;
                    significant = true;
                    this.KeyMap.executeButtonUp(ClassicControllerButton.StickLLeft);
                }
                if (wiimoteState.ClassicControllerState.JoystickL.X > 0.3 && !PressedButtons["Classic.StickLRight"])
                {
                    PressedButtons["Classic.StickLRight"] = true;
                    significant = true;
                    this.KeyMap.executeButtonDown(ClassicControllerButton.StickLRight);
                }
                else if (wiimoteState.ClassicControllerState.JoystickL.X < 0.3 && PressedButtons["Classic.StickLRight"])
                {
                    PressedButtons["Classic.StickLRight"] = false;
                    significant = true;
                    this.KeyMap.executeButtonUp(ClassicControllerButton.StickLRight);
                }
                if (wiimoteState.ClassicControllerState.JoystickL.Y > 0.3 && !PressedButtons["Classic.StickLUp"])
                {
                    PressedButtons["Classic.StickRUp"] = true;
                    significant = true;
                    this.KeyMap.executeButtonDown(ClassicControllerButton.StickRUp);
                }
                else if (wiimoteState.ClassicControllerState.JoystickL.Y < 0.3 && PressedButtons["Classic.StickRUp"])
                {
                    PressedButtons["Classic.StickRUp"] = false;
                    significant = true;
                    this.KeyMap.executeButtonUp(ClassicControllerButton.StickRUp);
                }

                if (wiimoteState.ClassicControllerState.JoystickL.Y < -0.3 && !PressedButtons["Classic.StickRDown"])
                {
                    PressedButtons["Classic.StickRDown"] = true;
                    significant = true;
                    this.KeyMap.executeButtonDown(ClassicControllerButton.StickRDown);
                }
                else if (wiimoteState.ClassicControllerState.JoystickL.Y > -0.3 && PressedButtons["Classic.StickRDown"])
                {
                    PressedButtons["Classic.StickRDown"] = false;
                    significant = true;
                    this.KeyMap.executeButtonUp(ClassicControllerButton.StickRDown);
                }

                if (wiimoteState.ClassicControllerState.JoystickL.X < -0.3 && !PressedButtons["Classic.StickRLeft"])
                {
                    PressedButtons["Classic.StickRLeft"] = true;
                    significant = true;
                    this.KeyMap.executeButtonDown(ClassicControllerButton.StickRLeft);
                }
                else if (wiimoteState.ClassicControllerState.JoystickL.X > -0.3 && PressedButtons["Classic.StickRLeft"])
                {
                    PressedButtons["Classic.StickRLeft"] = false;
                    significant = true;
                    this.KeyMap.executeButtonUp(ClassicControllerButton.StickRLeft);
                }
                if (wiimoteState.ClassicControllerState.JoystickL.X > 0.3 && !PressedButtons["Classic.StickRRight"])
                {
                    PressedButtons["Classic.StickRRight"] = true;
                    significant = true;
                    this.KeyMap.executeButtonDown(ClassicControllerButton.StickRRight);
                }
                else if (wiimoteState.ClassicControllerState.JoystickL.X < 0.3 && PressedButtons["Classic.StickRRight"])
                {
                    PressedButtons["Classic.StickRRight"] = false;
                    significant = true;
                    this.KeyMap.executeButtonUp(ClassicControllerButton.StickRRight);
                }
            }

            FieldInfo[] buttons = buttonState.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (FieldInfo button in buttons) {

                bool pressedNow = (bool)button.GetValue(buttonState);
                bool pressedBefore = PressedButtons[button.Name];

                if(pressedNow && !pressedBefore)
                {
                    PressedButtons[button.Name] = true;
                    significant = true;
                    if (button.Name == "Home")
                    {
                        this.homeButtonTimer.Start();
                        if (OverlayWindow.Current.OverlayIsOn())
                        {
                            this.hideOverlayOnUp = true;
                        }
                    }
                    else
                    {
                        this.KeyMap.executeButtonDown(button.Name);
                    }
                }
                else if (!pressedNow && pressedBefore)
                {
                    PressedButtons[button.Name] = false;
                    significant = true;
                    if(button.Name == "Home")
                    {
                        this.homeButtonTimer.Stop();

                        if (this.hideOverlayOnUp)
                        {
                            this.hideOverlayOnUp = false;
                            OverlayWindow.Current.HideOverlay();
                        }
                        else if (OverlayWindow.Current.OverlayIsOn())
                        {
                        }
                        else
                        {
                            this.KeyMap.executeButtonDown("Home");
                            this.KeyMap.executeButtonUp("Home");
                        }
                    }
                    else
                    {
                        this.KeyMap.executeButtonUp(button.Name);
                    }
                }
            }

            this.KeyMap.endUpdate();

            return significant;
        }
    }

    public enum MouseCode
    {
        MOUSELEFT,
        MOUSERIGHT
    }

}
