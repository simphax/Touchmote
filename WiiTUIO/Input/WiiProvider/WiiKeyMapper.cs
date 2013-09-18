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
        ZR
    }

    public enum NunchukButton
    {
        Z,
        C,
        JoyUp,
        JoyDown,
        JoyLeft,
        JoyRight,
    }

    public struct NunchukButtonState
    {
        public bool C;
        public bool Z;
        public bool JoyUp;
        public bool JoyDown;
        public bool JoyLeft;
        public bool JoyRight;
    }

    public class WiiKeyMapper
    {

        private string KEYMAPS_PATH = "Keymaps\\";
        private string CONFIG_JSON_FILENAME = "Keymaps.json";
        private string DEFAULT_JSON_FILENAME = "default.json";

        public WiiKeyMap KeyMap;

        public Dictionary<string, bool> PressedButtons = new Dictionary<string, bool>()
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
            {"Nunchuk.JoyUp",false},
            {"Nunchuk.JoyDown",false},
            {"Nunchuk.JoyLeft",false},
            {"Nunchuk.JoyRight",false},
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
            {"Classic.ZR",false}
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

        public WiiKeyMapper(int wiimoteID)
        {
            this.WiimoteID = wiimoteID;

            System.IO.Directory.CreateDirectory(KEYMAPS_PATH);
            this.applicationsJson = this.createDefaultApplicationsJSON();
            this.defaultKeymapJson = this.createDefaultKeymapJSON();

            this.defaultName =  this.defaultKeymapJson.GetValue("Title").ToString();
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

            this.KeyMap = new WiiKeyMap(this.defaultKeymapJson, this.fallbackName, this.fallbackFile, new XinputDevice(XinputBus.Default, wiimoteID), new XinputReport(wiimoteID));

            this.processMonitor = SystemProcessMonitor.Default;
            this.processMonitor.ProcessChanged += processChanged;
            this.processMonitor.Start();

            homeButtonTimer = new Timer();
            homeButtonTimer.Interval = 1000;
            homeButtonTimer.AutoReset = true;
            homeButtonTimer.Elapsed += homeButtonTimer_Elapsed;
        }

        public void Teardown()
        {
            this.KeyMap.XinputDevice.Remove();
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
            
        }

        private void keyMap_onButtonDown(WiiButtonEvent evt)
        {
            
        }

        private JObject createDefaultApplicationsJSON()
        {
            JArray layouts = new JArray();
            layouts.Add(new JObject(
                new JProperty("Name","Default"),
                new JProperty("Keymap",DEFAULT_JSON_FILENAME)
            ));

            JArray applications = new JArray();

            JObject applicationList =
                new JObject(
                    new JProperty("LayoutChooser", layouts),
                    new JProperty("Applications", applications),
                    new JProperty("Default", DEFAULT_JSON_FILENAME)
                );

            JObject union = applicationList;

            if (File.Exists(KEYMAPS_PATH + CONFIG_JSON_FILENAME))
            {
                StreamReader reader = File.OpenText(KEYMAPS_PATH + CONFIG_JSON_FILENAME);
                try
                {
                    JObject existingConfig = (JObject)JToken.ReadFrom(new JsonTextReader(reader));
                    reader.Close();

                    MergeJSON(union, existingConfig);
                }
                catch (Exception e) 
                {
                    throw new Exception(KEYMAPS_PATH + CONFIG_JSON_FILENAME + " is not valid JSON");
                }
            }

            File.WriteAllText(KEYMAPS_PATH + CONFIG_JSON_FILENAME, union.ToString());
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

            JArray buttonOne = new JArray();
            buttonOne.Add(new JValue("LWin"));
            buttonOne.Add(new JValue("VK_C"));
            buttons.Add(new JProperty("One", buttonOne));

            JArray buttonTwo = new JArray();
            buttonTwo.Add(new JValue("LWin"));
            buttonTwo.Add(new JValue("Tab"));
            buttons.Add(new JProperty("Two", buttonTwo));

            buttons.Add(new JProperty("Nunchuk.StickX", "360.StickLX"));
            buttons.Add(new JProperty("Nunchuk.StickY", "360.StickLY"));
            buttons.Add(new JProperty("Nunchuk.C", "360.TriggerL"));
            buttons.Add(new JProperty("Nunchuk.Z", "360.TriggerR"));
            
            buttons.Add(new JProperty("Nunchuk.JoyUp", "VK_W"));
            buttons.Add(new JProperty("Nunchuk.JoyDown", "VK_S"));
            buttons.Add(new JProperty("Nunchuk.JoyLeft", "VK_A"));
            buttons.Add(new JProperty("Nunchuk.JoyRight", "VK_D"));

            buttons.Add(new JProperty("Classic.Left", "360.Left"));
            buttons.Add(new JProperty("Classic.Right", "360.Right"));
            buttons.Add(new JProperty("Classic.Up", "360.Up"));
            buttons.Add(new JProperty("Classic.Down", "360.Down"));
            buttons.Add(new JProperty("Classic.StickLX", "360.StickLX"));
            buttons.Add(new JProperty("Classic.StickLY", "360.StickLY"));
            buttons.Add(new JProperty("Classic.StickRX", "360.StickRX"));
            buttons.Add(new JProperty("Classic.StickRY", "360.StickRY"));
            buttons.Add(new JProperty("Classic.Minus", "360.Back"));
            buttons.Add(new JProperty("Classic.Plus", "360.Start"));
            buttons.Add(new JProperty("Classic.Home", "360.Guide"));
            buttons.Add(new JProperty("Classic.Y", "360.Y"));
            buttons.Add(new JProperty("Classic.X", "360.X"));
            buttons.Add(new JProperty("Classic.A", "360.A"));
            buttons.Add(new JProperty("Classic.B", "360.B"));
            buttons.Add(new JProperty("Classic.TriggerL", "360.TriggerL"));
            buttons.Add(new JProperty("Classic.TriggerR", "360.TriggerR"));
            buttons.Add(new JProperty("Classic.L", "360.L"));
            buttons.Add(new JProperty("Classic.R", "360.R"));
            buttons.Add(new JProperty("Classic.ZL", "360.BumperL"));
            buttons.Add(new JProperty("Classic.ZR", "360.BumperR"));

            JObject union = new JObject();

            union.Add(new JProperty("Title", "Default"));

            union.Add(new JProperty("All", buttons));

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

        public void loadKeyMap(string filename)
        {

            string name = "";

            JObject union = (JObject)this.defaultKeymapJson.DeepClone();

            if (File.Exists(KEYMAPS_PATH + filename))
            {
                StreamReader reader = File.OpenText(KEYMAPS_PATH + filename);
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
                
                 if (wiimoteState.NunchukState.Y < -0.3 && !PressedButtons["Nunchuk.JoyUp"])
                {
                    PressedButtons["Nunchuk.JoyUp"] = true;
                    significant = true;
                    this.KeyMap.executeButtonDown(NunchukButton.JoyUp);
                }
                else if (wiimoteState.NunchukState.Y > -0.3 && PressedButtons["Nunchuk.JoyUp"])
                {
                    PressedButtons["Nunchuk.JoyUp"] = false;
                    significant = true;
                    this.KeyMap.executeButtonUp(NunchukButton.JoyUp);
                }
                
                 if (wiimoteState.NunchukState.Y > 0.3 && !PressedButtons["Nunchuk.JoyDown"])
                {
                    PressedButtons["Nunchuk.JoyDown"] = true;
                    significant = true;
                    this.KeyMap.executeButtonDown(NunchukButton.JoyDown);
                }
                else if (wiimoteState.NunchukState.Y < 0.3 && PressedButtons["Nunchuk.JoyDown"])
                {
                    PressedButtons["Nunchuk.JoyDown"] = false;
                    significant = true;
                    this.KeyMap.executeButtonUp(NunchukButton.JoyDown);
                }
                
                if (wiimoteState.NunchukState.X < -0.3 && !PressedButtons["Nunchuk.JoyLeft"])
                {
                    PressedButtons["Nunchuk.JoyLeft"] = true;
                    significant = true;
                    this.KeyMap.executeButtonDown(NunchukButton.JoyLeft);
                }
                else if (wiimoteState.NunchukState.X > -0.3 && PressedButtons["Nunchuk.JoyLeft"])
                {
                    PressedButtons["Nunchuk.JoyLeft"] = false;
                    significant = true;
                    this.KeyMap.executeButtonUp(NunchukButton.JoyLeft);
                }
                if (wiimoteState.NunchukState.X > 0.3 && !PressedButtons["Nunchuk.JoyRight"])
                {
                    PressedButtons["Nunchuk.JoyRight"] = true;
                    significant = true;
                    this.KeyMap.executeButtonDown(NunchukButton.JoyRight);
                }
                else if (wiimoteState.NunchukState.X < 0.3 && PressedButtons["Nunchuk.JoyRight"])
                {
                    PressedButtons["Nunchuk.JoyRight"] = false;
                    significant = true;
                    this.KeyMap.executeButtonUp(NunchukButton.JoyRight);
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

            this.KeyMap.XinputDevice.Update(this.KeyMap.XinputReport);

            return significant;
        }
    }

    public enum MouseCode
    {
        MOUSELEFT,
        MOUSERIGHT
    }

}
