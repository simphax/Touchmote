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

        public KeyboardHandler()
        {
            this.inputSimulator = new InputSimulator();
        }

        public bool setButtonDown(string key)
        {
            if (Enum.IsDefined(typeof(VirtualKeyCode), key.ToUpper()))
            {
                VirtualKeyCode theKeyCode = (VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), key, true);
                this.inputSimulator.Keyboard.KeyDown(theKeyCode);
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
