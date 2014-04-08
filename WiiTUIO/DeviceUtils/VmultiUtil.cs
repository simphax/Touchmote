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
using WiiCPP;
using WiiTUIO.Properties;

namespace WiiTUIO.DeviceUtils
{
    class VmultiUtil
    {

        private static string vmultiDevicePathSearch = "hid#vmultia&col01";
        private static string vmultiDevconSearch = "hid*vmultia*col01";
        private static string registryPath = "SOFTWARE\\Microsoft\\Wisp\\Pen\\Digimon";

        //Returns device path to current monitor for the vmulti device
        public static string getCurrentMonitor()
        {
            RegistryKey regKey = Registry.LocalMachine.OpenSubKey(registryPath, false);
            if (regKey != null)
            {
                string[] valueNames = regKey.GetValueNames();

                foreach(string valueName in valueNames)
                {
                    if (valueName.ToLower().Contains(vmultiDevicePathSearch))
                    {
                        string monitorPath = regKey.GetValue(valueName).ToString();
                        regKey.Close();
                        return monitorPath;
                    }
                }

                regKey.Close();
            }

            return null;
        }

        public static bool setCurrentMonitor(MonitorInfo monitor)
        {
            bool success = false;
            RegistryKey regKey = Registry.LocalMachine.OpenSubKey(registryPath, true);
            if (regKey != null)
            {
                string[] valueNames = regKey.GetValueNames();

                foreach (string valueName in valueNames)
                {
                    if (valueName.ToLower().Contains(vmultiDevicePathSearch))
                    {
                        Console.WriteLine("Set vmulti monitor to " + monitor.DevicePath);
                        regKey.SetValue(valueName,monitor.DevicePath);
                        Settings.Default.primaryMonitor = monitor.DevicePath;

                        success = true;
                    }
                }
                if(!success)
                {
                    IEnumerable<HidDevice> hidDevices = HidDevices.Enumerate();

                    string devicePath = null;

                    foreach(HidDevice device in hidDevices)
                    {
                        if(device.DevicePath.ToLower().Contains(vmultiDevicePathSearch))
                        {
                            devicePath = device.DevicePath;
                        }
                    }

                    if(devicePath != null)
                    {
                        Console.WriteLine("Creating new registry row for " + devicePath + ". Setting vmulti monitor to " + monitor.DevicePath);
                        regKey.SetValue("20-" + devicePath, monitor.DevicePath, RegistryValueKind.String);
                        Settings.Default.primaryMonitor = monitor.DevicePath;
                        success = true;
                    }
                }

                if(success)
                {
                    //Disable and enable the vmulti device to force windows to update the touch monitor settings
                    Launcher.Launch("Driver", "devcon", " disable \"" + vmultiDevconSearch + "\"", new Action(delegate()
                    {
                        Launcher.Launch("Driver", "devcon", " enable \"" + vmultiDevconSearch + "\"", null);
                    }));
                }

                regKey.Close();
            }
            return success;
        }
    }
}
