using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsInput;
using WindowsInput.Native;

namespace WiiTUIO.Output.Handlers
{
    public class KeyboardHandler : IButtonHandler
    {
        private InputSimulator inputSimulator;

        private HashSet<VirtualKeyCode> keysDown;

        public KeyboardHandler()
        {
            this.inputSimulator = new InputSimulator();

            this.keysDown = new HashSet<VirtualKeyCode>();
        }

        public bool reset()
        {
            foreach(VirtualKeyCode keyCode in keysDown)
            {
                this.inputSimulator.Keyboard.KeyUp(keyCode);
            }
            return true;
        }

        public bool setButtonDown(string key)
        {
            if (Enum.IsDefined(typeof(VirtualKeyCode), key.ToUpper()))
            {
                VirtualKeyCode theKeyCode = (VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), key, true);
                this.inputSimulator.Keyboard.KeyDown(theKeyCode);
                this.keysDown.Add(theKeyCode);
                return true;
            }
            return false;
        }

        public bool setButtonUp(string key)
        {
            if (Enum.IsDefined(typeof(VirtualKeyCode), key.ToUpper()))
            {
                VirtualKeyCode theKeyCode = (VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), key, true);
                this.inputSimulator.Keyboard.KeyUp(theKeyCode);
                this.keysDown.Remove(theKeyCode);
                return true;
            }
            return false;
        }

        public bool connect()
        {
            return true;
        }

        public bool disconnect()
        {
            return true;
        }

        public bool startUpdate()
        {
            return true;
        }

        public bool endUpdate()
        {
            return true;
        }
    }
}
