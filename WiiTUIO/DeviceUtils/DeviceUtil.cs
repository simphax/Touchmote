using HidLibrary;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WiiCPP;

namespace WiiTUIO.DeviceUtils
{
    class DeviceUtil
    {
        public static IEnumerable<HidDevice> GetHidList()
        {
            return HidLibrary.HidDevices.Enumerate();
        }

        public static IEnumerable<MonitorInfo> GetMonitorList()
        {
            // From WiiCPP...
            return new List<MonitorInfo>(Monitors.enumerateMonitors());
        }

        public static Screen GetScreen(string devicePath)
        {
            MonitorInfo primaryMonitorInfo = null;
            foreach (MonitorInfo info in GetMonitorList())
            {
                if (info.DevicePath == devicePath)
                {
                    primaryMonitorInfo = info;
                }
            }

            if (primaryMonitorInfo != null)
            {
                foreach (Screen screen in Screen.AllScreens)
                {
                    if (screen.DeviceName == primaryMonitorInfo.DeviceName)
                    {
                        return screen;
                    }
                }
            }

            return Screen.PrimaryScreen;
        }
    }
}
