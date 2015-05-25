using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using WiiTUIO.Properties;
using WiiTUIO.Provider;
using WindowsInput;
using WindowsInput.Native;

namespace WiiTUIO.Output.Handlers
{
    public class MouseHandler : IButtonHandler, IStickHandler, ICursorHandler
    {
        private InputSimulator inputSimulator;

        private bool mouseLeftDown = false;
        private bool mouseRightDown = false;

        private CursorPositionHelper cursorPositionHelper;

        public MouseHandler()
        {
            this.inputSimulator = new InputSimulator();
            cursorPositionHelper = new CursorPositionHelper();
        }
        
        public bool reset()
        {
            if (mouseLeftDown)
            {
                setButtonUp("mouseleft");
            }
            if (mouseRightDown)
            {
                setButtonUp("mouseright");
            }
            return true;
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
                        mouseLeftDown = true;
                        break;
                    case MouseCode.MOUSERIGHT:
                        this.inputSimulator.Mouse.RightButtonDown();
                        mouseRightDown = true;
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
                        mouseLeftDown = false;
                        break;
                    case MouseCode.MOUSERIGHT:
                        this.inputSimulator.Mouse.RightButtonUp();
                        mouseLeftDown = false;
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
            key = key.ToLower();
            if (key.Equals("mouse"))
            {
                if (!cursorPos.OutOfReach)
                {
                    Point smoothedPos = cursorPositionHelper.getRelativePosition(new Point(cursorPos.X, cursorPos.Y));
                    this.inputSimulator.Mouse.MoveMouseToPositionOnVirtualDesktop((65535 * smoothedPos.X), (65535 * smoothedPos.Y));
                    return true;
                }
            }

            if (key.Equals("fpsmouse"))
            {
                Point smoothedPos = cursorPositionHelper.getSmoothedPosition(new Point(cursorPos.RelativeX, cursorPos.RelativeY));

                /*
                    * TODO: Consider sensor bar position?
                if (Settings.Default.pointer_sensorBarPos == "top")
                {
                    smoothedPos.Y = smoothedPos.Y - Settings.Default.pointer_sensorBarPosCompensation;
                }
                else if (Settings.Default.pointer_sensorBarPos == "bottom")
                {
                    smoothedPos.Y = smoothedPos.Y + Settings.Default.pointer_sensorBarPosCompensation;
                }
                */
                double deadzone = Settings.Default.fpsmouse_deadzone; // TODO: Move to settings
                double shiftX = Math.Abs(smoothedPos.X - 0.5) > deadzone ? smoothedPos.X - 0.5 : 0;
                double shiftY = Math.Abs(smoothedPos.Y - 0.5) > deadzone ? smoothedPos.Y - 0.5 : 0;

                this.inputSimulator.Mouse.MoveMouseBy((int)(Settings.Default.fpsmouse_speed * shiftX), (int)(Settings.Default.fpsmouse_speed * shiftY));

                return true;
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
