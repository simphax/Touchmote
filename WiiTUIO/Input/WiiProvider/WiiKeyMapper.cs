using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using WiimoteLib;
using WindowsInput;

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

        public Action<WiiButtonEvent> OnButtonDown;
        public Action<WiiButtonEvent> OnButtonUp;

        WiiKeyMap keyMap;
        ButtonState PressedButtons;

        public WiiKeyMapper()
        {
            PressedButtons = new ButtonState();

            if (!File.Exists(KEYMAPS_PATH + APPLICATIONS_JSON_FILENAME))
            {
                this.createDefaultApplicationsJSON();
            }

            if (!File.Exists(KEYMAPS_PATH + DEFAULT_JSON_FILENAME))
            {
                this.createDefaultKeymapJSON();
            }

            StreamReader reader = File.OpenText(KEYMAPS_PATH + APPLICATIONS_JSON_FILENAME);
            JObject applicationsJson = (JObject)JToken.ReadFrom(new JsonTextReader(reader));

            this.keyMap = new WiiKeyMap(KEYMAPS_PATH + applicationsJson.GetValue("Default").ToString());


            this.keyMap.OnButtonDown += keyMap_onButtonDown;
            this.keyMap.OnButtonUp += keyMap_onButtonUp;

        }

        private void keyMap_onButtonUp(WiiButtonEvent evt)
        {
            this.OnButtonUp(evt);
        }

        private void keyMap_onButtonDown(WiiButtonEvent evt)
        {
            this.OnButtonDown(evt);
        }


        private void createDefaultApplicationsJSON()
        {
            JArray applications = new JArray();
            JObject paint = new JObject(
                new JProperty("Name","Paint"),
                new JProperty("Keymap","paint.json")
            );
            applications.Add(paint);


            JObject applicationList =
                new JObject(
                    new JProperty("Applications",
                        applications),
                    new JProperty("Default", DEFAULT_JSON_FILENAME)
            );

            File.WriteAllText(KEYMAPS_PATH + APPLICATIONS_JSON_FILENAME, applicationList.ToString());
        }

        private void createDefaultKeymapJSON()
        {
            JObject buttons = new JObject();

            buttons.Add(new JProperty("A", "Touch"));

            buttons.Add(new JProperty("B", "Return"));

            buttons.Add(new JProperty("Home", "LWin"));

            buttons.Add(new JProperty("Left", "Left"));
            buttons.Add(new JProperty("Right", "Right"));
            buttons.Add(new JProperty("Up", "Up"));
            buttons.Add(new JProperty("Down", "Down"));

            JArray buttonPlus = new JArray();
            buttonPlus.Add(new JValue("LControl"));
            buttonPlus.Add(new JValue("OEM_Plus"));
            buttons.Add(new JProperty("Plus", buttonPlus));

            JArray buttonMinus = new JArray();
            buttonMinus.Add(new JValue("LControl"));
            buttonMinus.Add(new JValue("OEM_Minus"));
            buttons.Add(new JProperty("Minus", buttonMinus));

            File.WriteAllText(KEYMAPS_PATH + DEFAULT_JSON_FILENAME, buttons.ToString());
        }

        public void setKeyMap(WiiKeyMap keyMap)
        {
            this.keyMap = keyMap;
        }

        public void processButtonState(ButtonState buttonState)
        {
            if (buttonState.A && !PressedButtons.A)
            {
                this.keyMap.executeButtonDown(WiimoteButton.A);
                PressedButtons.A = true;
            }
            else if (!buttonState.A && PressedButtons.A)
            {
                this.keyMap.executeButtonUp(WiimoteButton.A);
                PressedButtons.A = false;
            }

            if (buttonState.B && !PressedButtons.B)
            {
                this.keyMap.executeButtonDown(WiimoteButton.B);
                PressedButtons.B = true;
            }
            else if (!buttonState.B && PressedButtons.B)
            {
                this.keyMap.executeButtonUp(WiimoteButton.B);
                PressedButtons.B = false;
            }

            if (buttonState.Up && !PressedButtons.Up)
            {
                this.keyMap.executeButtonDown(WiimoteButton.Up);
                PressedButtons.Up = true;
            }
            else if (!buttonState.Up && PressedButtons.Up)
            {
                this.keyMap.executeButtonUp(WiimoteButton.Up);
                PressedButtons.Up = false;
            }

            if (buttonState.Down && !PressedButtons.Down)
            {
                this.keyMap.executeButtonDown(WiimoteButton.Down);
                PressedButtons.Down = true;
            }
            else if (!buttonState.Down && PressedButtons.Down)
            {
                this.keyMap.executeButtonUp(WiimoteButton.Down);
                PressedButtons.Down = false;
            }

            if (buttonState.Left && !PressedButtons.Left)
            {
                this.keyMap.executeButtonDown(WiimoteButton.Left);
                PressedButtons.Left = true;
            }
            else if (!buttonState.Left && PressedButtons.Left)
            {
                this.keyMap.executeButtonUp(WiimoteButton.Left);
                PressedButtons.Left = false;
            }

            if (buttonState.Right && !PressedButtons.Right)
            {
                this.keyMap.executeButtonDown(WiimoteButton.Right);
                PressedButtons.Right = true;
            }
            else if (!buttonState.Right && PressedButtons.Right)
            {
                this.keyMap.executeButtonUp(WiimoteButton.Right);
                PressedButtons.Right = false;
            }

            if (buttonState.Home && !PressedButtons.Home)
            {
                this.keyMap.executeButtonDown(WiimoteButton.Home);
                PressedButtons.Home = true;
            }
            else if (!buttonState.Home && PressedButtons.Home)
            {
                this.keyMap.executeButtonUp(WiimoteButton.Home);
                PressedButtons.Home = false;
            }

            if (buttonState.Plus && !PressedButtons.Plus)
            {
                this.keyMap.executeButtonDown(WiimoteButton.Plus);
                PressedButtons.Plus = true;
            }
            else if (PressedButtons.Plus && !buttonState.Plus)
            {
                this.keyMap.executeButtonUp(WiimoteButton.Plus);
                PressedButtons.Plus = false;
            }

            if (buttonState.Minus && !PressedButtons.Minus)
            {
                this.keyMap.executeButtonDown(WiimoteButton.Minus);
                PressedButtons.Minus = true;
            }
            else if (PressedButtons.Minus && !buttonState.Minus)
            {
                this.keyMap.executeButtonUp(WiimoteButton.Minus);
                PressedButtons.Minus = false;
            }

            if (buttonState.One && !PressedButtons.One)
            {
                this.keyMap.executeButtonDown(WiimoteButton.One);
                PressedButtons.One = true;
            }
            else if (PressedButtons.One && !buttonState.One)
            {
                this.keyMap.executeButtonUp(WiimoteButton.One);
                PressedButtons.One = false;
            }

            if (buttonState.Two && !PressedButtons.Two)
            {
                this.keyMap.executeButtonDown(WiimoteButton.Two);
                PressedButtons.Two = true;
            }
            else if (PressedButtons.Two && !buttonState.Two)
            {
                this.keyMap.executeButtonUp(WiimoteButton.One);
                PressedButtons.Two = false;
            }
        }
    }

    public class WiiKeyMap
    {
        private JObject jsonobj;

        public Action<WiiButtonEvent> OnButtonUp;
        public Action<WiiButtonEvent> OnButtonDown;

        public WiiKeyMap(string path)
        {
            StreamReader reader = File.OpenText(path);
            this.jsonobj = (JObject)JToken.ReadFrom(new JsonTextReader(reader));
        }

        public void executeButtonUp(WiimoteButton button)
        {
            bool handled = false;

            JToken key = this.jsonobj.GetValue(button.ToString()); //ToString converts WiimoteButton.A to "A" for instance

            if (Enum.IsDefined(typeof(VirtualKeyCode), key.ToString().ToUpper())) //Enum.Parse does the opposite...
            {
                InputSimulator.SimulateKeyUp((VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), key.ToString(), true));
                handled = true;
            }
            else if (key.Values().Count() > 0)
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
                List<VirtualKeyCode> keys = new List<VirtualKeyCode>();
                if (Enum.IsDefined(typeof(VirtualKeyCode), array.Last().ToString().ToUpper()))
                {
                    keys.Add((VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), array.Last().ToString(), true));
                }


                if (modifiers.Count() > 0 && key.Count() > 0)
                {
                    InputSimulator.SimulateModifiedKeyStroke(modifiers, keys);
                    handled = true;
                }
            }
            else
            {
                Console.WriteLine("Could not fire key " + key.ToString());
            }

            OnButtonUp(new WiiButtonEvent(key.ToString(), button, handled));
        }

        public void executeButtonDown(WiimoteButton button)
        {
            bool handled = false;

            JToken key = this.jsonobj.GetValue(button.ToString());
            if (Enum.IsDefined(typeof(VirtualKeyCode), key.ToString().ToUpper()))
            {
                InputSimulator.SimulateKeyDown((VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), key.ToString(), true));
                handled = true;
            }


            OnButtonDown(new WiiButtonEvent(key.ToString(), button, handled));
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
}
