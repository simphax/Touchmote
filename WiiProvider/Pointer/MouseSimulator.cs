using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace WiiTUIO.Provider
{
    public static class MouseSimulator
    {

        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }


        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public int type;
            public MOUSEINPUT mi;
        }

        const uint MOUSEEVENTF_MOVE = 0x0001;
        const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        const uint MOUSEEVENTF_LEFTUP = 0x0004;
        const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        const uint MOUSEEVENTF_XDOWN = 0x0080;
        const uint MOUSEEVENTF_XUP = 0x0100;
        const uint MOUSEEVENTF_WHEEL = 0x0800;
        const uint MOUSEEVENTF_VIRTUALDESK = 0x4000;
        const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

        const int INPUT_MOUSE = 0;
        const int INPUT_KEYBOARD = 1;
        const int INPUT_HARDWARE = 2;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public static implicit operator System.Windows.Point(POINT point)
            {
                return new System.Windows.Point(point.X, point.Y);
            }
        }

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("User32.Dll")]
        public static extern long SetCursorPos(int x, int y);

        [DllImport("User32.Dll")]
        static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);


        /// <summary>
        /// Sets the cursor position
        /// </summary>
        public static void SetCursorPosition(int x, int y)
        {
            SetCursorPos(x,y);
        }

        /// <summary>
        /// Fake a small movement by a mouse, to keep the cursor showing.
        /// </summary>
        public static void WakeCursor()
        {
            INPUT input = new INPUT();
            input.type = INPUT_MOUSE;
            input.mi.mouseData = 0;
            input.mi.time = 0;
            input.mi.dx = 1;
            input.mi.dy = 1;
            input.mi.dwFlags = MOUSEEVENTF_MOVE;

            INPUT input2 = new INPUT();
            input2.type = INPUT_MOUSE;
            input2.mi.mouseData = 0;
            input2.mi.time = 0;
            input2.mi.dx = -1;
            input2.mi.dy = -1;
            input2.mi.dwFlags = MOUSEEVENTF_MOVE;

            INPUT[] inputs = { input,input2 };

            SendInput(2, inputs, Marshal.SizeOf(input));
        }
    }
}
