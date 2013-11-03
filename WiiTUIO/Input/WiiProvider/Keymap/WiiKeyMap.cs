using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiimoteLib;
using WiiTUIO.Output.Handlers;
using WiiTUIO.Properties;
using WindowsInput;
using WindowsInput.Native;

namespace WiiTUIO.Provider
{
    public class WiiKeyMap
    {
        private Dictionary<string, KeymapOutConfig> config;
        private Keymap keymap;

        public Action<WiiButtonEvent> OnButtonUp;
        public Action<WiiButtonEvent> OnButtonDown;
        public Action<WiiKeyMapConfigChangedEvent> OnConfigChanged;
        public Action<bool> OnRumble;

        private InputSimulator inputSimulator;

        private List<IOutputHandler> outputHandlers;

        public DateTime HomeButtonDown = DateTime.Now;

        private long id;

        private Dictionary<string, bool> PressedButtons = new Dictionary<string, bool>()
        {
            {"Nunchuk.StickUp",false},
            {"Nunchuk.StickDown",false},
            {"Nunchuk.StickLeft",false},
            {"Nunchuk.StickRight",false}
        };

        public WiiKeyMap(long id, Keymap keymap, List<IOutputHandler> outputHandlers)
        {
            this.id = id;

            this.SetKeymap(keymap);

            this.inputSimulator = new InputSimulator();

            this.outputHandlers = outputHandlers;
        }

        public void SetKeymap(Keymap keymap)
        {
            if (this.keymap == null || this.keymap.Equals(keymap))
            {
                this.config = new Dictionary<string, KeymapOutConfig>();

                foreach (KeymapInput input in KeymapDatabase.Current.getAvailableInputs())
                {
                    KeymapOutConfig outConfig = keymap.getConfigFor((int)id, input.Key);
                    if (outConfig != null)
                    {
                        this.config.Add(input.Key, outConfig);
                    }
                }

                KeymapOutConfig pointerConfig;
                if (this.config.TryGetValue("Pointer", out pointerConfig) && this.OnConfigChanged != null)
                {
                    this.OnConfigChanged(new WiiKeyMapConfigChangedEvent(keymap.getName(),keymap.getFilename(),pointerConfig.Stack.First().Key));
                }
            }
        }

        public void SendConfigChangedEvt()
        {
            KeymapOutConfig pointerConfig;
            if (this.keymap != null && this.config.TryGetValue("Pointer", out pointerConfig) && this.OnConfigChanged != null)
            {
                this.OnConfigChanged(new WiiKeyMapConfigChangedEvent(keymap.getName(), keymap.getFilename(), pointerConfig.Stack.First().Key));
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

        private string supportedSpecialCodes = "PointerToggle TouchMaster TouchSlave NextLayout disable";

        internal void updateAccelerometer(AccelState accelState)
        {
            KeymapOutConfig outConfig;

            if (this.config.TryGetValue("AccelX", out outConfig))
            {
                updateStickHandlers(outConfig, accelState.Values.X * -0.5 + 0.5);
            }
            if (this.config.TryGetValue("AccelY", out outConfig))
            {
                updateStickHandlers(outConfig, accelState.Values.Y * -0.5 + 0.5);
            }
            if (this.config.TryGetValue("AccelZ", out outConfig))
            {
                updateStickHandlers(outConfig, accelState.Values.Z * -0.5 + 0.5);
            }
        }

        public void updateNunchuk(NunchukState nunchuk)
        {
            KeymapOutConfig outConfig;

            if (this.config.TryGetValue("Nunchuk.StickRight", out outConfig))
            {
                if (nunchuk.Joystick.X > 0)
                {
                    updateStickHandlers(outConfig, nunchuk.Joystick.X * 2);
                }
                else if (nunchuk.Joystick.X == 0)
                {
                    updateStickHandlers(outConfig, 0);
                }

                if (nunchuk.Joystick.X * 2 > outConfig.Threshold && !PressedButtons["Nunchuk.StickRight"])
                {
                    PressedButtons["Nunchuk.StickRight"] = true;
                    this.executeButtonDown("Nunchuk.StickRight");
                }
                else if (nunchuk.Joystick.X * 2 < outConfig.Threshold && PressedButtons["Nunchuk.StickRight"])
                {
                    PressedButtons["Nunchuk.StickRight"] = false;
                    this.executeButtonUp("Nunchuk.StickRight");
                }
            }

            if (this.config.TryGetValue("Nunchuk.StickLeft", out outConfig))
            {
                if (nunchuk.Joystick.X < 0)
                {
                    updateStickHandlers(outConfig, nunchuk.Joystick.X * -2);
                }
                else if (nunchuk.Joystick.X == 0)
                {
                    updateStickHandlers(outConfig, 0);
                }

                if (nunchuk.Joystick.X * -2 > outConfig.Threshold && !PressedButtons["Nunchuk.StickLeft"])
                {
                    PressedButtons["Nunchuk.StickLeft"] = true;
                    this.executeButtonDown("Nunchuk.StickLeft");
                }
                else if (nunchuk.Joystick.X * -2 < outConfig.Threshold && PressedButtons["Nunchuk.StickLeft"])
                {
                    PressedButtons["Nunchuk.StickLeft"] = false;
                    this.executeButtonUp("Nunchuk.StickLeft");
                }
            }
            if (this.config.TryGetValue("Nunchuk.StickUp", out outConfig))
            {
                if (nunchuk.Joystick.Y > 0)
                {
                    updateStickHandlers(outConfig, nunchuk.Joystick.Y * 2);
                }
                else if (nunchuk.Joystick.Y == 0)
                {
                    updateStickHandlers(outConfig, 0);
                }

                if (nunchuk.Joystick.Y * 2 > outConfig.Threshold && !PressedButtons["Nunchuk.StickUp"])
                {
                    PressedButtons["Nunchuk.StickUp"] = true;
                    this.executeButtonDown("Nunchuk.StickUp");
                }
                else if (nunchuk.Joystick.Y * 2 < outConfig.Threshold && PressedButtons["Nunchuk.StickUp"])
                {
                    PressedButtons["Nunchuk.StickUp"] = false;
                    this.executeButtonUp("Nunchuk.StickUp");
                }

            }
            if (this.config.TryGetValue("Nunchuk.StickDown", out outConfig))
            {
                if (nunchuk.Joystick.Y < 0)
                {
                    updateStickHandlers(outConfig, nunchuk.Joystick.Y * -2);
                }
                else if (nunchuk.Joystick.Y == 0)
                {
                    updateStickHandlers(outConfig, 0);
                }

                if (nunchuk.Joystick.Y * -2 > outConfig.Threshold && !PressedButtons["Nunchuk.StickDown"])
                {
                    PressedButtons["Nunchuk.StickDown"] = true;
                    this.executeButtonDown("Nunchuk.StickDown");
                }
                else if (nunchuk.Joystick.Y * -2 < outConfig.Threshold && PressedButtons["Nunchuk.StickDown"])
                {
                    PressedButtons["Nunchuk.StickDown"] = false;
                    this.executeButtonUp("Nunchuk.StickDown");
                }
            }

        }

        public void updateClassicController(ClassicControllerState classic)
        {
            //JToken key = this.jsonObj.GetValue("Classic.StickLX");
            //if (key != null)
            //{
            //    string handle = key.ToString().ToLower();
            //    updateStickHandlers(handle, classic.JoystickL.X + 0.5);
            //}

            //key = this.jsonObj.GetValue("Classic.StickLY");
            //if (key != null)
            //{
            //    string handle = key.ToString().ToLower();
            //    updateStickHandlers(handle, classic.JoystickL.Y + 0.5);
            //}

            //key = this.jsonObj.GetValue("Classic.StickRX");
            //if (key != null)
            //{
            //    string handle = key.ToString().ToLower();
            //    updateStickHandlers(handle, classic.JoystickR.X + 0.5);
            //}

            //key = this.jsonObj.GetValue("Classic.StickRY");
            //if (key != null)
            //{
            //    string handle = key.ToString().ToLower();
            //    updateStickHandlers(handle, classic.JoystickR.Y + 0.5);
            //}

            //key = this.jsonObj.GetValue("Classic.TriggerL");
            //if (key != null)
            //{
            //    string handle = key.ToString().ToLower();
            //    updateStickHandlers(handle, classic.TriggerL);
            //}

            //key = this.jsonObj.GetValue("Classic.TriggerR");
            //if (key != null)
            //{
            //    string handle = key.ToString().ToLower();
            //    updateStickHandlers(handle, classic.TriggerR);
            //}
        }

        private bool updateStickHandlers(KeymapOutConfig outConfig, double value)
        {
            foreach (IOutputHandler handler in outputHandlers)
            {
                IStickHandler stickHandler = handler as IStickHandler;
                if (stickHandler != null)
                {
                    foreach(KeymapOutput output in outConfig.Stack)
                    {
                        if (output.Continous)
                        {
                            //Add the scaling from the config but never go outside 0-1
                            value = value * outConfig.Scale;
                            value = value > 1 ? 1 : value;
                            value = value < 0 ? 0 : value;
                            if (stickHandler.setValue(output.Key.ToString().ToLower(), value))
                            {
                                return true; // we will break for the first accepting handler
                            }
                        }
                    }
                }
            }
            return false;
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

            List<string> keyList = new List<string>();
            KeymapOutConfig outConfig;

            if (this.config.TryGetValue(button, out outConfig))
            {
                List<KeymapOutput> stack = new List<KeymapOutput>(outConfig.Stack);
                stack.Reverse();
                foreach (KeymapOutput output in stack)
                {
                    keyList.Add(output.Key);
                    if (!(output.Continous && KeymapDatabase.Current.getInput(button).Continous)) //Exclude the case when a stick is connected to a stick. It should not trigger the press action.
                    {
                        handled |= this.executeKeyUp(output.Key);
                    }
                }

            }

            if (OnButtonUp != null)
            {
                OnButtonUp(new WiiButtonEvent(keyList, button, handled));
            }
        }

        private bool executeKeyUp(string key)
        {
            foreach (IOutputHandler handler in outputHandlers)
            {
                IButtonHandler buttonHandler = handler as IButtonHandler;
                if (buttonHandler != null)
                {
                    if (buttonHandler.setButtonUp(key))
                    {
                        return true;
                    }
                }
            }
            return false;
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
            List<string> keyList = new List<string>();
            KeymapOutConfig outConfig;
            if (this.config.TryGetValue(button, out outConfig) && outConfig != null)
            {
                HashSet<string> handledKeys = new HashSet<string>();
                List<KeymapOutput> stack = new List<KeymapOutput>(outConfig.Stack);
                stack.Reverse();
                foreach (KeymapOutput output in stack)
                {
                    keyList.Add(output.Key);

                    if (!(output.Continous && KeymapDatabase.Current.getInput(button).Continous)) //Exclude the case when a continous output is connected to a continous output. It should not trigger the button action.
                    {
                        if (handledKeys.Contains(output.Key)) //Repeat a button that has already been pressed
                        {
                            this.executeKeyUp(output.Key);
                        }
                        handledKeys.Add(output.Key);
                    }

                    handled |= this.executeKeyDown(output.Key);
                }
            }

            if (OnButtonDown != null)
            {
                OnButtonDown(new WiiButtonEvent(keyList, button, handled));
            }
        }

        private bool executeKeyDown(string key)
        {
            foreach (IOutputHandler handler in outputHandlers)
            {
                IButtonHandler buttonHandler = handler as IButtonHandler;
                if (buttonHandler != null)
                {
                    if (buttonHandler.setButtonDown(key))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    
}

    public class WiiButtonEvent
    {
        public bool Handled;
        public List<string> Actions;
        public string Button;

        public WiiButtonEvent(List<string> actions, string button, bool handled = false)
        {
            this.Actions = actions;
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
