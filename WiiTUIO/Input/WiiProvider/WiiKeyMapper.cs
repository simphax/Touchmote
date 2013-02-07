using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiimoteLib;
using WindowsInput;

namespace WiiTUIO.Provider
{
    class WiiKeyMapper
    {

        public Action<WiimoteButton> OnButtonDown;
        public Action<WiimoteButton> OnButtonUp;

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


        ButtonState PressedButtons;

        public WiiKeyMapper()
        {
            PressedButtons = new ButtonState();
        }

        public void processButtonState(ButtonState buttonState)
        {

            if (buttonState.A && !PressedButtons.A)
            {
                OnButtonDown(WiimoteButton.A);
                PressedButtons.A = true;
            }
            else if (!buttonState.B && PressedButtons.B)
            {
                OnButtonUp(WiimoteButton.A);
                PressedButtons.A = false;
            }

            if (buttonState.B && !PressedButtons.B)
            {
                InputSimulator.SimulateKeyDown(VirtualKeyCode.RETURN);
                OnButtonDown(WiimoteButton.B);
                PressedButtons.B = true;
            }
            else if (!buttonState.B && PressedButtons.B)
            {
                InputSimulator.SimulateKeyUp(VirtualKeyCode.RETURN);
                OnButtonUp(WiimoteButton.B);
                PressedButtons.B = false;
            }


            if (buttonState.Up && !PressedButtons.Up)
            {
                InputSimulator.SimulateKeyDown(VirtualKeyCode.UP);
                PressedButtons.Up = true;
            }
            else if (!buttonState.Up && PressedButtons.Up)
            {
                InputSimulator.SimulateKeyUp(VirtualKeyCode.UP);
                PressedButtons.Up = false;
            }

            if (buttonState.Down && !PressedButtons.Down)
            {
                InputSimulator.SimulateKeyDown(VirtualKeyCode.DOWN);
                PressedButtons.Down = true;
            }
            else if (!buttonState.Down && PressedButtons.Down)
            {
                InputSimulator.SimulateKeyUp(VirtualKeyCode.DOWN);
                PressedButtons.Down = false;
            }

            if (buttonState.Left && !PressedButtons.Left)
            {
                InputSimulator.SimulateKeyDown(VirtualKeyCode.LEFT);
                PressedButtons.Left = true;
            }
            else if (!buttonState.Left && PressedButtons.Left)
            {
                InputSimulator.SimulateKeyUp(VirtualKeyCode.LEFT);
                PressedButtons.Left = false;
            }

            if (buttonState.Right && !PressedButtons.Right)
            {
                InputSimulator.SimulateKeyDown(VirtualKeyCode.RIGHT);
                PressedButtons.Right = true;
            }
            else if (!buttonState.Right && PressedButtons.Right)
            {
                InputSimulator.SimulateKeyUp(VirtualKeyCode.RIGHT);
                PressedButtons.Right = false;
            }

            if (buttonState.Home && !PressedButtons.Home)
            {
                InputSimulator.SimulateKeyDown(VirtualKeyCode.LWIN);
                PressedButtons.Home = true;
            }
            else if (!buttonState.Home && PressedButtons.Home)
            {
                InputSimulator.SimulateKeyUp(VirtualKeyCode.LWIN);
                PressedButtons.Home = false;
            }

            if (buttonState.Plus && !PressedButtons.Plus)
            {
                InputSimulator.SimulateModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.OEM_PLUS);
                PressedButtons.Plus = true;
            }
            else if (PressedButtons.Plus && !buttonState.Plus)
            {
                PressedButtons.Plus = false;
            }
            if (buttonState.Minus && !PressedButtons.Minus)
            {
                InputSimulator.SimulateModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.OEM_MINUS);
                PressedButtons.Minus = true;
            }
            else if (PressedButtons.Minus && !buttonState.Minus)
            {
                PressedButtons.Minus = false;
            }


            if (buttonState.One && !PressedButtons.One)
            {
                OnButtonDown(WiimoteButton.One);
                PressedButtons.One = true;
            }
            else if (PressedButtons.One && !buttonState.One)
            {
                OnButtonUp(WiimoteButton.One);
                PressedButtons.One = false;
            }
            if (buttonState.Two && !PressedButtons.Two)
            {
                OnButtonDown(WiimoteButton.Two);
                PressedButtons.Two = true;
            }
            else if (PressedButtons.Two && !buttonState.Two)
            {
                OnButtonUp(WiimoteButton.One);
                PressedButtons.Two = false;
            }
        }
    }
}
