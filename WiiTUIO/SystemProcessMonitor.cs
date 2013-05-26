using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WiiTUIO
{
    class SystemProcessMonitor : IDisposable
    {
        /// <summary>
        /// The GetForegroundWindow function returns a handle to the foreground window.
        /// </summary>
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        public Action<ProcessChangedEvent> ProcessChanged;

        private uint lastProcessId = 0;

        private System.Timers.Timer pollingTimer;

        private static SystemProcessMonitor defaultInstance;
        public static SystemProcessMonitor Default
        {
            get
            {
                if (defaultInstance == null)
                {
                    defaultInstance = new SystemProcessMonitor();
                }
                return defaultInstance;
            }
        }

        private SystemProcessMonitor()
        {
            pollingTimer = new System.Timers.Timer();
            pollingTimer.Interval = 500;
            pollingTimer.Elapsed += pollingTimer_Elapsed;
            this.Start();
        }

        private void pollingTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            IntPtr foregroundWindow = GetForegroundWindow();
            uint procId = 0;
            GetWindowThreadProcessId(foregroundWindow, out procId);

            if (procId != lastProcessId)
            {
                Process process = Process.GetProcessById((int)procId);
                if (ProcessChanged != null && process != null && process.Id > 0)
                {
                    this.ProcessChanged(new ProcessChangedEvent(process));
                }
                this.lastProcessId = procId;
            }
        }

        public void Start()
        {
            pollingTimer.Start();
        }

        public void Stop()
        {
            pollingTimer.Stop();
        }

        public void Dispose()
        {
            this.Stop();
            pollingTimer.Dispose();
        }
    }

    public class ProcessChangedEvent
    {
        public Process Process;

        public ProcessChangedEvent(Process process)
        {
            this.Process = process;
        }
    }
}
