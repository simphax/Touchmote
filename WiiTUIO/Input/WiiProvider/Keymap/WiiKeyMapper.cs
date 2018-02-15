using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        [Flags]
        private enum ButtonFlag
        {
            A = (1 << 0),
            B = (1 << 1),
            Up = (1 << 2),
            Down = (1 << 3),
            Left = (1 << 4),
            Right = (1 << 5),
            Minus = (1 << 6),
            Plus = (1 << 7),
            Home = (1 << 8),
            One = (1 << 9),
            Two = (1 << 10),
            NunchukC = (1 << 11),
            NunchukZ = (1 << 12),
            ClassicA = (1 << 13),
            ClassicB = (1 << 14),
            ClassicX = (1 << 15),
            ClassicY = (1 << 16),
            ClassicUp = (1 << 17),
            ClassicDown = (1 << 18),
            ClassicLeft = (1 << 19),
            ClassicRight = (1 << 20),
            ClassicHome = (1 << 21),
            ClassicPlus = (1 << 22),
            ClassicMinus = (1 << 23),
            ClassicL = (1 << 24),
            ClassicR = (1 << 25),
            ClassicZL = (1 << 26),
            ClassicZR = (1 << 27)
        }

        private ButtonFlag PressedButtons;

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
            if (isButtonPressed(ButtonFlag.Home))
            {
                this.setKeymap(this.defaultKeymap);
                OverlayWindow.Current.ShowLayoutOverlay(this);
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

            //this.processWiimoteState(new WiimoteState()); //Sets all buttons to "not pressed"
            PressedButtons = PressedButtons & ButtonFlag.Home;

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

                significant |= checkButtonState(wiimoteState.NunchukState.C, "Nunchuk.C");
                significant |= checkButtonState(wiimoteState.NunchukState.Z, "Nunchuk.Z");
            }

            if (wiimoteState.Extension && wiimoteState.ExtensionType == ExtensionType.ClassicController)
            {
                this.KeyMap.updateClassicController(wiimoteState.ClassicControllerState);

                ClassicControllerButtonState classicButtonState = wiimoteState.ClassicControllerState.ButtonState;

                significant |= checkButtonState(classicButtonState.A , "Classic.A");
                significant |= checkButtonState(classicButtonState.B, "Classic.B");
                significant |= checkButtonState(classicButtonState.Down, "Classic.Down");
                significant |= checkButtonState(classicButtonState.Home, "Classic.Home");
                significant |= checkButtonState(classicButtonState.Left, "Classic.Left");
                significant |= checkButtonState(classicButtonState.Minus, "Classic.Minus");
                significant |= checkButtonState(classicButtonState.Plus, "Classic.Plus");
                significant |= checkButtonState(classicButtonState.Right, "Classic.Right");
                significant |= checkButtonState(classicButtonState.TriggerL, "Classic.L");
                significant |= checkButtonState(classicButtonState.TriggerR, "Classic.R");
                significant |= checkButtonState(classicButtonState.Up, "Classic.Up");
                significant |= checkButtonState(classicButtonState.X, "Classic.X");
                significant |= checkButtonState(classicButtonState.Y, "Classic.Y");
                significant |= checkButtonState(classicButtonState.ZL, "Classic.ZL");
                significant |= checkButtonState(classicButtonState.ZR, "Classic.ZR");
            }

            if (this.releaseHomeOnNextUpdate)
            {
                this.releaseHomeOnNextUpdate = false;
                this.KeyMap.executeButtonUp("Home");
            }

            significant |= checkButtonState(buttonState.A, "A");
            significant |= checkButtonState(buttonState.B, "B");
            significant |= checkButtonState(buttonState.Down, "Down");
            significant |= checkButtonState(buttonState.Home, "Home");
            significant |= checkButtonState(buttonState.Left, "Left");
            significant |= checkButtonState(buttonState.Minus, "Minus");
            significant |= checkButtonState(buttonState.One, "One");
            significant |= checkButtonState(buttonState.Plus, "Plus");
            significant |= checkButtonState(buttonState.Right, "Right");
            significant |= checkButtonState(buttonState.Two, "Two");
            significant |= checkButtonState(buttonState.Up, "Up");

            foreach (IOutputHandler handler in outputHandlers)
            {
                handler.endUpdate();
            }

            if (significant)
            {
                Console.WriteLine("********************************significant");
            }

            return significant;
        }

        private bool isButtonPressed(ButtonFlag button)
        {
            return (PressedButtons & button) != 0;
        }

        private void setPressedButton(ButtonFlag button, bool value)
        {
            if (value)
                PressedButtons |= button;
            else
                PressedButtons &= ~button;
        }

        private void setPressedButton(string name, bool value)
        {
            setPressedButton(getButtonFlag(name), value);
        }

        private ButtonFlag getButtonFlag(string buttonName)
        {
            switch (buttonName)
            {
                case "A": return ButtonFlag.A;
                case "B": return ButtonFlag.B;
                case "Up": return ButtonFlag.Up;
                case "Down": return ButtonFlag.Down;
                case "Left": return ButtonFlag.Left;
                case "Right": return ButtonFlag.Right;
                case "Minus": return ButtonFlag.Minus;
                case "Plus": return ButtonFlag.Plus;
                case "Home": return ButtonFlag.Home;
                case "One": return ButtonFlag.One;
                case "Two": return ButtonFlag.Two;
                case "Nunchuk.C": return ButtonFlag.NunchukC;
                case "Nunchuk.Z": return ButtonFlag.NunchukZ;
                case "Classic.A": return ButtonFlag.ClassicA;
                case "Classic.B": return ButtonFlag.ClassicB;
                case "Classic.X": return ButtonFlag.ClassicX;
                case "Classic.Y": return ButtonFlag.ClassicY;
                case "Classic.Up": return ButtonFlag.ClassicUp;
                case "Classic.Down": return ButtonFlag.ClassicDown;
                case "Classic.Left": return ButtonFlag.ClassicLeft;
                case "Classic.Right": return ButtonFlag.ClassicRight;
                case "Classic.Home": return ButtonFlag.ClassicHome;
                case "Classic.Plus": return ButtonFlag.ClassicPlus;
                case "Classic.Minus": return ButtonFlag.ClassicMinus;
                case "Classic.L": return ButtonFlag.ClassicL;
                case "Classic.R": return ButtonFlag.ClassicR;
                case "Classic.ZL": return ButtonFlag.ClassicZL;
                case "Classic.ZR": return ButtonFlag.ClassicZR;
                default:
                    throw new NotImplementedException("Unknown button name:" + buttonName);
            }
        }

        private bool checkButtonState(bool pressedNow, string buttonName)
        {
            bool significant = false;
            ButtonFlag buttonFlag = getButtonFlag(buttonName);
            bool pressedBefore = isButtonPressed(buttonFlag);

            if (pressedNow && !pressedBefore) //On down
            {
                setPressedButton(buttonFlag, true);
                significant = true;
                if (buttonName == "Home")
                {
                    Console.WriteLine("home down");
                    if (OverlayWindow.Current.OverlayIsOn())
                    {
                        this.hideOverlayOnUp = true;
                        Console.WriteLine("hide overlay on up");
                    }
                    else
                    {
                        this.homeButtonTimer.Start();
                    }
                }
                else
                {
                    this.KeyMap.executeButtonDown(buttonName);
                }
            }
            else if (!pressedNow && pressedBefore) //On up
            {
                setPressedButton(buttonFlag, false);
                significant = true;
                if (buttonName == "Home")
                {
                    Console.WriteLine("home up");
                    this.homeButtonTimer.Stop();

                    if (this.hideOverlayOnUp)
                    {
                        this.hideOverlayOnUp = false;
                        OverlayWindow.Current.HideOverlay();
                    }
                    else if (OverlayWindow.Current.OverlayIsOn()) //We opened the overlay on this down
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
                    this.KeyMap.executeButtonUp(buttonName);
                }
            }

            return significant;
        }
    }

}
