using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using WiiTUIO.Output.Handlers.Touch;
using WiiTUIO.Properties;
using WiiTUIO.Provider;
using WindowsInput;
using WindowsInput.Native;

namespace WiiTUIO.Output.Handlers
{
    public class MouseHandler : IButtonHandler, IStickHandler, ICursorHandler
    {
        private InputSimulator inputSimulator;

        //TODO factor out whats relevant for mouse... this is kinda hacky
        private DuoTouch duoTouch;

        public MouseHandler()
        {
            this.inputSimulator = new InputSimulator();
            this.duoTouch = new DuoTouch(Settings.Default.pointer_positionSmoothing, 1);
        }

        public bool setButtonDown(string key)
        {
            if (Enum.IsDefined(typeof(MouseCode), key.ToUpper()))
            {
                MouseCode mouseCode = (MouseCode)Enum.Parse(typeof(MouseCode), key, true);
                switch (mouseCode)
                {
                    case MouseCode.MOUSELEFT:
                        this.inputSimulator.Mouse.LeftButtonDown();
                        duoTouch.setContactMaster(); //To get touch tap threshold...
                        break;
                    case MouseCode.MOUSERIGHT:
                        this.inputSimulator.Mouse.RightButtonDown();
                        duoTouch.setContactMaster();
                        break;
                    default:
                        return false;
                }
                return true;
            }
            return false;
        }

        public bool setButtonUp(string key)
        {
            if (Enum.IsDefined(typeof(MouseCode), key.ToUpper()))
            {
                MouseCode mouseCode = (MouseCode)Enum.Parse(typeof(MouseCode), key, true);
                switch (mouseCode)
                {
                    case MouseCode.MOUSELEFT:
                        this.inputSimulator.Mouse.LeftButtonUp();
                        duoTouch.releaseContactMaster();
                        break;
                    case MouseCode.MOUSERIGHT:
                        this.inputSimulator.Mouse.RightButtonUp();
                        duoTouch.releaseContactMaster();
                        break;
                    default:
                        return false;
                }
                return true;
            }
            return false;
        }

        public bool setPosition(string key, CursorPos cursorPos)
        {
            if (key.ToLower().Equals("mouse"))
            {
                if (!cursorPos.OutOfReach)
                {
                    duoTouch.setMasterPosition(new Point(cursorPos.X, cursorPos.Y));
                    Queue<WiiContact> contacts = duoTouch.getFrame();

                    if (contacts.Count > 0)
                    {
                        WiiContact first = contacts.First();
                        Point smoothedPos = first.NormalPosition;

                        this.inputSimulator.Mouse.MoveMouseToPositionOnVirtualDesktop((65535 * smoothedPos.X), (65535 * smoothedPos.Y));
                        return true;
                    }
                }
            }
            return false;
        }

        public bool setValue(string key, double value)
        {
            key = key.ToLower();
            switch (key)
            {
                case "mousey+":
                    this.inputSimulator.Mouse.MoveMouseBy(0, (int)(-30 * value + 0.5));
                    break;
                case "mousey-":
                    this.inputSimulator.Mouse.MoveMouseBy(0, (int)(30 * value + 0.5));
                    break;
                case "mousex+":
                    this.inputSimulator.Mouse.MoveMouseBy((int)(30 * value + 0.5), 0);
                    break;
                case "mousex-":
                    this.inputSimulator.Mouse.MoveMouseBy((int)(-30 * value + 0.5), 0);
                    break;
                default:
                    return false;
            }
            return true;
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

    public enum MouseCode
    {
        MOUSELEFT,
        MOUSERIGHT
    }
}
