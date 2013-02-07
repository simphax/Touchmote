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

        private bool running;

        private uint lastProcessId = 0;

        public SystemProcessMonitor()
        {
            
            this.running = true;

            Thread thread = new Thread(ThreadWorker);
            thread.Start();
        }

        private void ThreadWorker()
        {
            while(running)
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

                System.Threading.Thread.Sleep(500);
            }
        }

        public void stop()
        {
            this.running = false;
        }

        public void Dispose()
        {
            this.running = false;
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
