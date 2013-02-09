using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace WiiTUIO.Provider
{
    public static class MouseSimulator
    {

        private static IntPtr lastCursor;

        [DllImport("user32.dll")]
        public static extern IntPtr LoadCursorFromFile(string lpFileName);

        [DllImport("user32.dll")]
        static extern bool SetSystemCursor(IntPtr hcur, uint id);

        [DllImport("user32.dll")]
        static extern bool DestroyCursor(IntPtr hcur);

        [DllImport("user32.dll")]
        static extern IntPtr LoadCursor(IntPtr hInstance, uint id);

        [DllImport("user32.dll")]
        static extern IntPtr CopyIcon(IntPtr hcur);

        enum IDC_STANDARD_CURSORS : uint
        {
            IDC_ARROW = 32512,
            IDC_IBEAM = 32513,
            IDC_WAIT = 32514,
            IDC_CROSS = 32515,
            IDC_UPARROW = 32516,
            IDC_SIZE = 32640,
            IDC_ICON = 32641,
            IDC_SIZENWSE = 32642,
            IDC_SIZENESW = 32643,
            IDC_SIZEWE = 32644,
            IDC_SIZENS = 32645,
            IDC_SIZEALL = 32646,
            IDC_NO = 32648,
            IDC_HAND = 32649,
            IDC_APPSTARTING = 32650,
            IDC_HELP = 32651
        }


        private static Dictionary<IDC_STANDARD_CURSORS, IntPtr> cursorCopies = new Dictionary<IDC_STANDARD_CURSORS, IntPtr>()
	{
	    {IDC_STANDARD_CURSORS.IDC_ARROW, IntPtr.Zero},
	    {IDC_STANDARD_CURSORS.IDC_IBEAM, IntPtr.Zero},
	    {IDC_STANDARD_CURSORS.IDC_WAIT, IntPtr.Zero},
	    {IDC_STANDARD_CURSORS.IDC_CROSS, IntPtr.Zero},
	    {IDC_STANDARD_CURSORS.IDC_UPARROW, IntPtr.Zero},
	    {IDC_STANDARD_CURSORS.IDC_SIZE, IntPtr.Zero},
	    {IDC_STANDARD_CURSORS.IDC_ICON, IntPtr.Zero},
	    {IDC_STANDARD_CURSORS.IDC_SIZENWSE, IntPtr.Zero},
	    {IDC_STANDARD_CURSORS.IDC_SIZENESW, IntPtr.Zero},
	    {IDC_STANDARD_CURSORS.IDC_SIZEWE, IntPtr.Zero},
	    {IDC_STANDARD_CURSORS.IDC_SIZENS, IntPtr.Zero},
	    {IDC_STANDARD_CURSORS.IDC_SIZEALL, IntPtr.Zero},
	    {IDC_STANDARD_CURSORS.IDC_NO, IntPtr.Zero},
	    {IDC_STANDARD_CURSORS.IDC_HAND, IntPtr.Zero},
	    {IDC_STANDARD_CURSORS.IDC_APPSTARTING, IntPtr.Zero},
	    {IDC_STANDARD_CURSORS.IDC_HELP, IntPtr.Zero},
	};

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

        public static void FireMouseEvent()
        {

        }

        //Temporary solution to the "diamond cursor" trouble.
        public static void RefreshMainCursor()
        {
            IntPtr cursorCopy = CopyIcon(lastCursor);
            SetSystemCursor(cursorCopy, (uint)IDC_STANDARD_CURSORS.IDC_ARROW);
            DestroyCursor(cursorCopy);
        }

        /// <summary>
        /// Sets the cursor position
        /// </summary>
        public static void SetCursorPosition(int x, int y)
        {
            SetCursorPos(x,y);
        }

        public static Point GetCursorPosition()
        {
            POINT point = new POINT();
            GetCursorPos(out point);
            return new Point(point.X, point.Y);
        }

        public static void ResetSystemCursor() 
        {
            foreach (KeyValuePair<IDC_STANDARD_CURSORS, IntPtr> pair in cursorCopies)
            {
                SetSystemCursor(pair.Value, (uint)pair.Key);
                DestroyCursor(pair.Value);
            }

            DestroyCursor(lastCursor);
        }

        public static void SetSystemCursor(string path)
        {
            lastCursor = LoadCursorFromFile(path);
            //Dictionaries can not be changed while enumerating so we loop a list instead.
            List<IDC_STANDARD_CURSORS> keys = new List<IDC_STANDARD_CURSORS>(cursorCopies.Keys);
            foreach (IDC_STANDARD_CURSORS key in keys)
            {
                IntPtr cursorCopy = CopyIcon(lastCursor);
                cursorCopies[key] = CopyIcon(LoadCursor(IntPtr.Zero, (uint)key));
                SetSystemCursor(cursorCopy, (uint)key);
                DestroyCursor(cursorCopy);
            }
            
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
