using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Interop;

namespace WiiTUIO.Provider
{
    public class D3DCursorWindow
    {
        private static D3DCursorWindow defaultInstance;

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
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
            cursors = new List<D3DCursor>(2);
            mutex = new Mutex();

            Thread cursorRenderThread = new Thread(CursorRenderWorker);
            cursorRenderThread.Priority = ThreadPriority.BelowNormal;
            cursorRenderThread.IsBackground = true;
            cursorRenderThread.Start();

            Thread cursorMoveThread = new Thread(CursorMoveWorker);
            cursorMoveThread.Priority = ThreadPriority.Normal;
            cursorMoveThread.IsBackground = true;
            cursorMoveThread.Start();

        }

        private Mutex mutex;
        private List<D3DCursor> cursors;

        [DllImport("D3DCursor.dll")]
        private static extern void SetD3DCursorPosition(int id, int x, int y);

        [DllImport("D3DCursor.dll")]
        private static extern void SetD3DCursorPressed(int id, bool pressed);

        [DllImport("D3DCursor.dll")]
        private static extern void SetD3DCursorHidden(int id, bool hidden);

        [DllImport("D3DCursor.dll")]
        private static extern void AddD3DCursor(int id, uint color, IntPtr hInstance, IntPtr parent);

        [DllImport("D3DCursor.dll")]
        private static extern void RemoveD3DCursor(int id);
        
        [DllImport("D3DCursor.dll")]
        private static extern void RenderAllD3DCursors();

        [DllImport("D3DCursor.dll")]
        private static extern void MoveAllD3DCursors();


        //Should be run with a dispatcher
        public void Start(IntPtr parent, int width, int height)
        {
            //StartD3DCursorWindow(Process.GetCurrentProcess().Handle, parent, width, height);
        }

        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
 	        
        }

        public void AddCursor(D3DCursor cursor)
        {
            mutex.WaitOne();
            cursors.Add(cursor);

            OverlayWindow.Current.Dispatcher.BeginInvoke(new Action(delegate()
            {
                AddD3DCursor(cursor.ID, (uint)((((uint)cursor.Color.R) << 16) | (((uint)cursor.Color.G) << 8) | (uint)cursor.Color.B), Process.GetCurrentProcess().Handle, (new WindowInteropHelper(OverlayWindow.Current)).Handle);
            }));
           
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
            OverlayWindow.Current.Dispatcher.BeginInvoke(new Action(delegate()
            {
                foreach(D3DCursor cursor in cursors)
                {
                    SetD3DCursorPosition(cursor.ID, cursor.X, cursor.Y);
                    SetD3DCursorPressed(cursor.ID, cursor.Pressed);
                    SetD3DCursorHidden(cursor.ID, cursor.Hidden);
                }
            }));
            //renderNow = true;
        }

        public void CursorMoveWorker()
        {
            while (true)
            {
                //while (!renderNow)
                //{
                //    Thread.Sleep(0);
                //}
                //renderNow = false;
                MoveAllD3DCursors();
                Thread.Sleep(10);
            }
        }

        public void CursorRenderWorker()
        {
            while (true)
            {
                //while (!renderNow)
                //{
                //    Thread.Sleep(0);
                //}
                //renderNow = false;
                RenderAllD3DCursors();
                Thread.Sleep(15);
            }
        }

    }
}
