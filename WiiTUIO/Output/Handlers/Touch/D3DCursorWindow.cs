using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WiiCPP;
using WiiTUIO.DeviceUtils;
using WiiTUIO.Properties;

namespace WiiTUIO.Output.Handlers.Touch
{
    public class D3DCursorWindow
    {
        private static D3DCursorWindow defaultInstance;

        private Screen primaryScreen;

        public static D3DCursorWindow Current
        {
            get
            {
                if (defaultInstance == null)
                {
                    defaultInstance = new D3DCursorWindow();
                }
                return defaultInstance;
            }
        }

        private D3DCursorWindow()
        {
            Settings.Default.PropertyChanged += SettingsChanged;
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
            cursors = new List<D3DCursor>(2);
            mutex = new Mutex();

            primaryScreen = DeviceUtil.GetScreen(Settings.Default.primaryMonitor);
        }

        private void SettingsChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(e.PropertyName == "primaryMonitor")
            {
                primaryScreen = DeviceUtil.GetScreen(Settings.Default.primaryMonitor);
                this.updateWindowToScreen(primaryScreen);
            }
        }

        private void updateWindowToScreen(Screen screen)
        {
            Console.WriteLine("Setting cursor window position to " + screen.Bounds);
            SetD3DCursorWindowPosition(screen.Bounds.X, screen.Bounds.Y, screen.Bounds.Width, screen.Bounds.Height, !Settings.Default.noTopmost);
        }

        private Mutex mutex;
        private List<D3DCursor> cursors;
            
        [DllImport("D3DCursor.dll")]
        private static extern IntPtr StartD3DCursorWindow(IntPtr hInstance, IntPtr parent, int windowX, int windowY, int windowWidth, int windowHeight, bool topmost);

        [DllImport("D3DCursor.dll")]
        private static extern void SetD3DCursorWindowPosition(int x, int y, int width, int height, bool topmost);

        [DllImport("D3DCursor.dll")]
        private static extern void SetD3DCursorPosition(int id, int x, int y);

        [DllImport("D3DCursor.dll")]
        private static extern void SetD3DCursorPressed(int id, bool pressed);

        [DllImport("D3DCursor.dll")]
        private static extern void SetD3DCursorHidden(int id, bool hidden);

        [DllImport("D3DCursor.dll")]
        private static extern void AddD3DCursor(int id, uint color);

        [DllImport("D3DCursor.dll")]
        private static extern void RemoveD3DCursor(int id);

        [DllImport("D3DCursor.dll")]
        private static extern void RenderAllD3DCursors();

        //Should be run with a dispatcher
        public void Start(IntPtr parent)
        {
            StartD3DCursorWindow(Process.GetCurrentProcess().Handle, parent, primaryScreen.Bounds.X, primaryScreen.Bounds.Y, primaryScreen.Bounds.Width, primaryScreen.Bounds.Height, !Settings.Default.noTopmost);
            updateWindowToScreen(primaryScreen);
        }

        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            primaryScreen = DeviceUtil.GetScreen(Settings.Default.primaryMonitor);
            this.updateWindowToScreen(primaryScreen);
        }

        public void AddCursor(D3DCursor cursor)
        {
            mutex.WaitOne();
            cursors.Add(cursor);

            AddD3DCursor(cursor.ID, (uint)((((uint)cursor.Color.R) << 16) | (((uint)cursor.Color.G) << 8) | (uint)cursor.Color.B));

            SetD3DCursorPosition(cursor.ID, cursor.X, cursor.Y);
            SetD3DCursorPressed(cursor.ID, cursor.Pressed);
            SetD3DCursorHidden(cursor.ID, cursor.Hidden);
            mutex.ReleaseMutex();
        }

        public void RemoveCursor(D3DCursor cursor)
        {
            mutex.WaitOne();
            cursors.Remove(cursor);

            RemoveD3DCursor(cursor.ID);
            mutex.ReleaseMutex();
        }

        public void RefreshCursors()
        {
            foreach(D3DCursor cursor in cursors)
            {
                SetD3DCursorPosition(cursor.ID, cursor.X, cursor.Y);
                SetD3DCursorPressed(cursor.ID, cursor.Pressed);
                SetD3DCursorHidden(cursor.ID, cursor.Hidden);
            }
            RenderAllD3DCursors();
        }

    }
}
