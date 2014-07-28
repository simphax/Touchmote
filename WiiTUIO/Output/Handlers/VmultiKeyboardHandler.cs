using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsInput;
using WindowsInput.Native;
using VMultiDllWrapper;

namespace WiiTUIO.Output.Handlers
{
    public class VmultiKeyboardHandler : IButtonHandler
    {
        private InputSimulator inputSimulator;
        private KeyboardReport report;

        private static VmultiKeyboardHandler defaultInstance;

        public static VmultiKeyboardHandler Default
        {
            get
            {
                if (defaultInstance == null)
                {
                    defaultInstance = new VmultiKeyboardHandler();
                }
                return defaultInstance;
            }
        }

        private VmultiKeyboardHandler()
        {
            this.report = new KeyboardReport();
            this.inputSimulator = new InputSimulator();
        }

        public bool setButtonDown(string key)
        {
            if (Enum.IsDefined(typeof(KeyboardKey), key.ToUpper()))
            {
                KeyboardKey theKeyCode = (KeyboardKey)Enum.Parse(typeof(KeyboardKey), key, true);
                report.keyDown(theKeyCode);
                return true;
            }
            else if (Enum.IsDefined(typeof(KeyboardModifier), key.ToUpper()))
            {
                KeyboardModifier theKeyCode = (KeyboardModifier)Enum.Parse(typeof(KeyboardModifier), key, true);
                report.keyDown(theKeyCode);
                return true;
            }
            else if (Enum.IsDefined(typeof(VirtualKeyCode), key.ToUpper()))
            {
                VirtualKeyCode theKeyCode = (VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), key, true);
                
                Enum vmultiKey = VmultiKeycodeAdapter.ConvertVirtualKeyCode(theKeyCode);
                
                if(vmultiKey == null)
                {
                    this.inputSimulator.Keyboard.KeyDown(theKeyCode);
                    return true;
                }

                if (vmultiKey is KeyboardKey)
                {
                    KeyboardKey keyboardKey = (KeyboardKey)vmultiKey;
                    report.keyDown(keyboardKey);
                    return true;
                }

                if (vmultiKey is KeyboardModifier)
                {
                    KeyboardModifier keyboardModifier = (KeyboardModifier)vmultiKey;
                    report.keyDown(keyboardModifier);
                    return true;
                }
            }
            return false;
        }

        public bool setButtonUp(string key)
        {
            if (Enum.IsDefined(typeof(KeyboardKey), key.ToUpper()))
            {
                KeyboardKey theKeyCode = (KeyboardKey)Enum.Parse(typeof(KeyboardKey), key, true);
                report.keyUp(theKeyCode);
                return true;
            }
            else if (Enum.IsDefined(typeof(KeyboardModifier), key.ToUpper()))
            {
                KeyboardModifier theKeyCode = (KeyboardModifier)Enum.Parse(typeof(KeyboardModifier), key, true);
                report.keyUp(theKeyCode);
                return true;
            }
            else if (Enum.IsDefined(typeof(VirtualKeyCode), key.ToUpper()))
            {
                VirtualKeyCode theKeyCode = (VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), key, true);

                Enum vmultiKey = VmultiKeycodeAdapter.ConvertVirtualKeyCode(theKeyCode);

                if (vmultiKey == null)
                {
                    this.inputSimulator.Keyboard.KeyUp(theKeyCode);
                    return true;
                }

                if (vmultiKey is KeyboardKey)
                {
                    KeyboardKey keyboardKey = (KeyboardKey)vmultiKey;
                    report.keyUp(keyboardKey);
                    return true;
                }

                if (vmultiKey is KeyboardModifier)
                {
                    KeyboardModifier keyboardModifier = (KeyboardModifier)vmultiKey;
                    report.keyUp(keyboardModifier);
                    return true;
                }
            }
            return false;
        }


        public bool connect()
        {
            /* should always be connected
            if(!VmultiDevice.Current.isConnected())
            {
                return VmultiDevice.Current.connect();
            }
             * */
            return true;
        }

        public bool disconnect()
        {
            VmultiDevice.Current.updateKeyboard(new KeyboardReport()); //Sets all keys to up state.
            //VmultiDevice.Current.disconnect();
            return true;
        }

        public bool startUpdate()
        {
            return true;
        }

        public bool endUpdate()
        {
            return VmultiDevice.Current.updateKeyboard(report);
        }
    }

    public class VmultiKeycodeAdapter {
        public static Enum ConvertVirtualKeyCode(VirtualKeyCode vkey)
        {
            switch (vkey)
            {
                case VirtualKeyCode.TAB:
                    return KeyboardKey.Tab;
                case VirtualKeyCode.BACK:
                    return KeyboardKey.Backspace;
                case VirtualKeyCode.RETURN:
                    return KeyboardKey.Enter;
                case VirtualKeyCode.SHIFT:
                    return KeyboardModifier.LShift;
                case VirtualKeyCode.CONTROL:
                    return KeyboardModifier.LControl;
                case VirtualKeyCode.MENU:
                    return KeyboardModifier.LAlt;
                case VirtualKeyCode.PAUSE:
                    return KeyboardKey.Pause;
                case VirtualKeyCode.CAPITAL:
                    return KeyboardKey.CapsLock;
                case VirtualKeyCode.ESCAPE:
                    return KeyboardKey.Escape;
                case VirtualKeyCode.SPACE:
                    return KeyboardKey.Spacebar;
                case VirtualKeyCode.PRIOR:
                    return KeyboardKey.PageUp;
                case VirtualKeyCode.NEXT:
                    return KeyboardKey.PageDown;
                case VirtualKeyCode.END:
                    return KeyboardKey.End;
                case VirtualKeyCode.HOME:
                    return KeyboardKey.Home;
                case VirtualKeyCode.LEFT:
                    return KeyboardKey.LeftArrow;
                case VirtualKeyCode.UP:
                    return KeyboardKey.UpArrow;
                case VirtualKeyCode.RIGHT:
                    return KeyboardKey.RightArrow;
                case VirtualKeyCode.DOWN:
                    return KeyboardKey.DownArrow;
                case VirtualKeyCode.SNAPSHOT:
                    return KeyboardKey.PrintScreen;
                case VirtualKeyCode.INSERT:
                    return KeyboardKey.Insert;
                case VirtualKeyCode.DELETE:
                    return KeyboardKey.Delete;
                case VirtualKeyCode.VK_0:
                    return KeyboardKey.Number0;
                case VirtualKeyCode.VK_1:
                    return KeyboardKey.Number1;
                case VirtualKeyCode.VK_2:
                    return KeyboardKey.Number2;
                case VirtualKeyCode.VK_3:
                    return KeyboardKey.Number3;
                case VirtualKeyCode.VK_4:
                    return KeyboardKey.Number4;
                case VirtualKeyCode.VK_5:
                    return KeyboardKey.Number5;
                case VirtualKeyCode.VK_6:
                    return KeyboardKey.Number6;
                case VirtualKeyCode.VK_7:
                    return KeyboardKey.Number7;
                case VirtualKeyCode.VK_8:
                    return KeyboardKey.Number8;
                case VirtualKeyCode.VK_9:
                    return KeyboardKey.Number9;
                case VirtualKeyCode.VK_A:
                    return KeyboardKey.A;
                case VirtualKeyCode.VK_B:
                    return KeyboardKey.B;
                case VirtualKeyCode.VK_C:
                    return KeyboardKey.C;
                case VirtualKeyCode.VK_D:
                    return KeyboardKey.D;
                case VirtualKeyCode.VK_E:
                    return KeyboardKey.E;
                case VirtualKeyCode.VK_F:
                    return KeyboardKey.F;
                case VirtualKeyCode.VK_G:
                    return KeyboardKey.G;
                case VirtualKeyCode.VK_H:
                    return KeyboardKey.H;
                case VirtualKeyCode.VK_I:
                    return KeyboardKey.I;
                case VirtualKeyCode.VK_J:
                    return KeyboardKey.J;
                case VirtualKeyCode.VK_K:
                    return KeyboardKey.K;
                case VirtualKeyCode.VK_L:
                    return KeyboardKey.L;
                case VirtualKeyCode.VK_M:
                    return KeyboardKey.M;
                case VirtualKeyCode.VK_N:
                    return KeyboardKey.N;
                case VirtualKeyCode.VK_O:
                    return KeyboardKey.O;
                case VirtualKeyCode.VK_P:
                    return KeyboardKey.P;
                case VirtualKeyCode.VK_Q:
                    return KeyboardKey.Q;
                case VirtualKeyCode.VK_R:
                    return KeyboardKey.R;
                case VirtualKeyCode.VK_S:
                    return KeyboardKey.S;
                case VirtualKeyCode.VK_T:
                    return KeyboardKey.T;
                case VirtualKeyCode.VK_U:
                    return KeyboardKey.U;
                case VirtualKeyCode.VK_V:
                    return KeyboardKey.V;
                case VirtualKeyCode.VK_W:
                    return KeyboardKey.W;
                case VirtualKeyCode.VK_X:
                    return KeyboardKey.X;
                case VirtualKeyCode.VK_Y:
                    return KeyboardKey.Y;
                case VirtualKeyCode.VK_Z:
                    return KeyboardKey.Z;
                case VirtualKeyCode.LWIN:
                    return KeyboardModifier.LWin;
                case VirtualKeyCode.RWIN:
                    return KeyboardModifier.RWin;
                case VirtualKeyCode.APPS:
                    return KeyboardKey.KeypadApplication;
                case VirtualKeyCode.NUMPAD0:
                    return KeyboardKey.Keypad0;
                case VirtualKeyCode.NUMPAD1:
                    return KeyboardKey.Keypad1;
                case VirtualKeyCode.NUMPAD2:
                    return KeyboardKey.Keypad2;
                case VirtualKeyCode.NUMPAD3:
                    return KeyboardKey.Keypad3;
                case VirtualKeyCode.NUMPAD4:
                    return KeyboardKey.Keypad4;
                case VirtualKeyCode.NUMPAD5:
                    return KeyboardKey.Keypad5;
                case VirtualKeyCode.NUMPAD6:
                    return KeyboardKey.Keypad6;
                case VirtualKeyCode.NUMPAD7:
                    return KeyboardKey.Keypad7;
                case VirtualKeyCode.NUMPAD8:
                    return KeyboardKey.Keypad8;
                case VirtualKeyCode.NUMPAD9:
                    return KeyboardKey.Keypad9;
                case VirtualKeyCode.MULTIPLY:
                    return KeyboardKey.KeypadMultiply;
                case VirtualKeyCode.ADD:
                    return KeyboardKey.KeypadAdd;
                case VirtualKeyCode.SUBTRACT:
                    return KeyboardKey.KeypadSubtract;
                case VirtualKeyCode.DECIMAL:
                    return KeyboardKey.KeypadDecimal;
                case VirtualKeyCode.DIVIDE:
                    return KeyboardKey.KeypadDivide;
                case VirtualKeyCode.F1:
                    return KeyboardKey.F1;
                case VirtualKeyCode.F2:
                    return KeyboardKey.F2;
                case VirtualKeyCode.F3:
                    return KeyboardKey.F3;
                case VirtualKeyCode.F4:
                    return KeyboardKey.F4;
                case VirtualKeyCode.F5:
                    return KeyboardKey.F5;
                case VirtualKeyCode.F6:
                    return KeyboardKey.F6;
                case VirtualKeyCode.F7:
                    return KeyboardKey.F7;
                case VirtualKeyCode.F8:
                    return KeyboardKey.F8;
                case VirtualKeyCode.F9:
                    return KeyboardKey.F9;
                case VirtualKeyCode.F10:
                    return KeyboardKey.F10;
                case VirtualKeyCode.F11:
                    return KeyboardKey.F11;
                case VirtualKeyCode.F12:
                    return KeyboardKey.F12;
                case VirtualKeyCode.NUMLOCK:
                    return KeyboardKey.NumLock;
                case VirtualKeyCode.LSHIFT:
                    return KeyboardModifier.LShift;
                case VirtualKeyCode.RSHIFT:
                    return KeyboardModifier.RShift;
                case VirtualKeyCode.LCONTROL:
                    return KeyboardModifier.LControl;
                case VirtualKeyCode.RCONTROL:
                    return KeyboardModifier.RControl;
                case VirtualKeyCode.LMENU:
                    return KeyboardModifier.LAlt;
                case VirtualKeyCode.RMENU:
                    return KeyboardModifier.RAlt;
                default:
                    return null;
            }
        }

    }
}
