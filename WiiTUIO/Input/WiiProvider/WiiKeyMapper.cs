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

    public struct NunchukButtonState
    {
        public bool C;
        public bool Z;
    }

    public class WiiKeyMapper
    {

        private string KEYMAPS_PATH = "Keymaps\\";
        private string APPLICATIONS_JSON_FILENAME = "Keymaps.json";
        private string DEFAULT_JSON_FILENAME = "default.json";

        public WiiKeyMap KeyMap;
        public ButtonState PressedButtons;
        public NunchukButtonState NunchukPressedButtons;

        private SystemProcessMonitor processMonitor;

        private JObject applicationsJson;
        private JObject defaultKeymapJson; //Always default.json
        private JObject fallbackKeymapJson; //Decided by the layout chooser

        private Timer homeButtonTimer;

        public int WiimoteID;
        private bool hideOverlayOnUp;

        public WiiKeyMapper(int wiimoteID)
        {
            this.WiimoteID = wiimoteID;

            PressedButtons = new ButtonState();
            NunchukPressedButtons = new NunchukButtonState();

            System.IO.Directory.CreateDirectory(KEYMAPS_PATH);
            this.applicationsJson = this.createDefaultApplicationsJSON();
            this.defaultKeymapJson = this.createDefaultKeymapJSON();

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

            this.KeyMap = new WiiKeyMap(this.defaultKeymapJson, new XinputDevice(XinputBus.Default, wiimoteID), new XinputReport(wiimoteID));

            this.processMonitor = SystemProcessMonitor.getInstance();

            this.processMonitor.ProcessChanged += processChanged;

            homeButtonTimer = new Timer();
            homeButtonTimer.Interval = 1000;
            homeButtonTimer.AutoReset = true;
            homeButtonTimer.Elapsed += homeButtonTimer_Elapsed;
        }

        public IEnumerable<JObject> GetLayoutList()
        {
            return this.applicationsJson.GetValue("LayoutChooser").Children<JObject>();
        }

        public void SetDefaultKeymap(string filename)
        {
            this.loadKeyMap(KEYMAPS_PATH + filename);
            this.fallbackKeymapJson = this.KeyMap.JsonObj;
        }

        void homeButtonTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (this.PressedButtons.Home)
            {
                OverlayWindow.Current.ShowLayoutOverlay(this);
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
                    string appName = configuration.GetValue("Name").ToString();

                    if (appStringToMatch.ToLower().Replace(" ", "").Contains(appName.ToLower().Replace(" ", "")))
                    {
                        this.loadKeyMap(KEYMAPS_PATH + configuration.GetValue("Keymap").ToString());
                        keymapFound = true;
                    }
                    
                }
                if (!keymapFound)
                {
                    this.KeyMap.JsonObj = this.fallbackKeymapJson;
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
                this.homeButtonTimer.Start();
                if (OverlayWindow.Current.OverlayIsOn())
                {
                    this.hideOverlayOnUp = true;
                }
                PressedButtons.Home = true;
                significant = true;
            }
            else if (!buttonState.Home && PressedButtons.Home)
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
                    this.KeyMap.executeButtonDown(WiimoteButton.Home);
                    this.KeyMap.executeButtonUp(WiimoteButton.Home);
                }
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

}
