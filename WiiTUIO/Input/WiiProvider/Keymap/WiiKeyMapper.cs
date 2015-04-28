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

        private Keymap defaultKeymap; //Always default.json
        private Keymap fallbackKeymap; //Decided by the layout chooser

        private Timer homeButtonTimer;

        public int WiimoteID;
        private bool hideOverlayOnUp = false;
        private bool releaseHomeOnNextUpdate = false;

        private List<IOutputHandler> outputHandlers;

        private ScreenPositionCalculator screenPositionCalculator;

        public WiiKeyMapper(int wiimoteID, HandlerFactory handlerFactory)
        {
            this.WiimoteID = wiimoteID;
            this.outputHandlers = handlerFactory.getOutputHandlers(this.WiimoteID);
            foreach (IOutputHandler handler in outputHandlers)
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

            this.screenPositionCalculator = new ScreenPositionCalculator();

            KeymapConfigWindow.Instance.OnConfigChanged += keymapConfigWindow_OnConfigChanged;
        }

        private void initialize()
        {
            this.defaultKeymap = KeymapDatabase.Current.getDefaultKeymap();

            JObject specificKeymap = new JObject();
            JObject commonKeymap = new JObject();

            this.fallbackKeymap = defaultKeymap;

            this.KeyMap = new WiiKeyMap(this.WiimoteID, this.defaultKeymap, this.outputHandlers);
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

        private void keymapConfigWindow_OnConfigChanged()
        {
            this.initialize();
        }

        public void Teardown()
        {
            foreach (IOutputHandler handler in outputHandlers)
            {
                handler.disconnect();
            }
            this.processMonitor.ProcessChanged -= processChanged;
        }

        public List<LayoutChooserSetting> GetLayoutList()
        {
            return KeymapDatabase.Current.getKeymapSettings().getLayoutChooserSettings();
        }

        private void setKeymap(Keymap keymap)
        {
            foreach (IOutputHandler handler in outputHandlers)
            {
                handler.startUpdate();
                handler.reset();
                handler.endUpdate();
            }
            this.KeyMap.SetKeymap(keymap);
        }

        public void SetFallbackKeymap(string filename)
        {
            this.fallbackKeymap = this.loadKeyMap(filename);
        }
        public string GetFallbackKeymap()
        {
            return this.fallbackKeymap.Filename;
        }

        public void SwitchToDefault()
        {
            this.setKeymap(this.defaultKeymap); //Switch to fallback even if we did not choose anything in the chooser.
        }

        public void SwitchToFallback()
        {
            this.setKeymap(this.fallbackKeymap); //Switch to fallback even if we did not choose anything in the chooser.
        }

        void homeButtonTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (this.PressedButtons["Home"])
            {
                this.setKeymap(this.defaultKeymap);
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

                List<ApplicationSearchSetting> applicationConfigurations = KeymapDatabase.Current.getKeymapSettings().getApplicationSearchSettings();
                foreach (ApplicationSearchSetting searchSetting in applicationConfigurations)
                {
                    string search = searchSetting.Search;

                    if (appStringToMatch.ToLower().Replace(" ", "").Contains(search.ToLower().Replace(" ", "")))
                    {
                        this.loadKeyMap(searchSetting.Keymap);
                        keymapFound = true;
                    }
                    
                }
                if (!keymapFound)
                {
                    this.setKeymap(this.fallbackKeymap);
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

        public Keymap loadKeyMap(string filename)
        {
            Keymap keymap = KeymapDatabase.Current.getKeymap(filename);

            this.setKeymap(keymap);

            this.processWiimoteState(new WiimoteState()); //Sets all buttons to "not pressed"

            Console.WriteLine("Loaded new keymap " + filename);
            return keymap;
        }

        public bool processWiimoteState(WiimoteState wiimoteState) //Returns true if anything has changed from last report.
        {
            ButtonState buttonState = wiimoteState.ButtonState;
            bool significant = false;

            foreach (IOutputHandler handler in outputHandlers)
            {
                handler.startUpdate();
            }

            CursorPos cursorPos = this.screenPositionCalculator.CalculateCursorPos(wiimoteState);

            this.KeyMap.updateCursorPosition(cursorPos);
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

            if (this.releaseHomeOnNextUpdate)
            {
                this.releaseHomeOnNextUpdate = false;
                this.KeyMap.executeButtonUp("Home");
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
                            this.releaseHomeOnNextUpdate = true;
                        }
                    }
                    else
                    {
                        this.KeyMap.executeButtonUp(button.Name);
                    }
                }
            }

            foreach (IOutputHandler handler in outputHandlers)
            {
                handler.endUpdate();
            }


            return significant;
        }
    }

}
