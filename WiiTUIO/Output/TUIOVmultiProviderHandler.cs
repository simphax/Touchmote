using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.ServiceProcess;
using System.Threading;
using WiiTUIO.Properties;
using System.Configuration;
using System.IO;
using Microsoft.Win32;
using System.Security.Principal;
using HidLibrary;

namespace WiiTUIO.Output
{
    class TUIOVmultiProviderHandler : IProviderHandler
    {
        public event Action OnConnect;

        public event Action OnDisconnect;

        private TUIOProviderHandler TUIOHandler;

        private static string etd_ServiceName = "Tuio-To-vmulti-Device1";
        private string edt_dataFolder;


        public static bool HasDriver()
        {
            IEnumerable<HidDevice> devices = HidDevices.Enumerate();
            bool hasDriver = false;
            foreach (HidDevice device in devices)
            {
                if (device.DevicePath.Substring(0,15) == "\\\\?\\hid#vmultia")
                {
                    hasDriver = true;
                }
            }
            if (hasDriver)
            {
                ServiceController[] services = ServiceController.GetServices();
                foreach (ServiceController service in services)
                {
                    if (service.ServiceName == etd_ServiceName)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public TUIOVmultiProviderHandler()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
            this.edt_dataFolder = (new FileInfo(config.FilePath)).DirectoryName + "\\";
            this.TUIOHandler = new TUIOProviderHandler();
        }

        public void connect()
        {
            //start tuio-to-vmulti service
            start_service(etd_ServiceName);
            this.TUIOHandler.connect();
            OnConnect();
        }

        public void processEventFrame(Provider.FrameEventArgs e)
        {
            this.TUIOHandler.processEventFrame(e);
        }

        public void disconnect()
        {
            //stop tuio-to-vmulti-service
            stop_service(etd_ServiceName);
            this.TUIOHandler.disconnect();
            OnDisconnect();
        }

        public void showSettingsWindow()
        {
            this.TUIOHandler.showSettingsWindow();
        }

        #region Code from EcoTUIOdriver
        public string start_service(string service_name)
        {

            ServiceController sc = new ServiceController();
            sc.ServiceName = service_name;
            if (sc.Status == ServiceControllerStatus.Stopped)
            {
                // Start the service if the current status is stopped.
                try
                {
                    Console.WriteLine("Starting the " + service_name + " service...");
                }
                catch { }
                try
                {
                    // Start the service, and wait until its status is "Running".
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running);
                    Thread.Sleep(500);
                    // Display the current service status.
                    try
                    {
                        Console.WriteLine("The " + service_name + " service status is now set to {0}.",
                                           sc.Status.ToString());
                    }
                    catch { }
                    return "The " + service_name + " service status is now set to " + sc.Status.ToString();
                }
                catch (InvalidOperationException)
                {
                    try
                    {
                        Console.WriteLine("Could not start the " + service_name + " service.");
                    }
                    catch { }
                    return "Could not start the " + service_name + " service.";
                }
                Thread.Sleep(500);
            }
            return "Service is already running";

        }

        public string stop_service(string service_name)
        {

            ServiceController sc = new ServiceController();
            sc.ServiceName = service_name;
            if (sc.Status == ServiceControllerStatus.Running)
            {
                // Start the service if the current status is stopped.
                try
                {
                    Console.WriteLine("Stopping the " + service_name + " service...");
                }
                catch { }
                try
                {
                    // Start the service, and wait until its status is "Running".
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped);
                    Thread.Sleep(500);
                    // Display the current service status.
                    try
                    {
                        Console.WriteLine("The " + service_name + " service status is now set to {0}.",
                                           sc.Status.ToString());
                    }
                    catch { }
                    return "The " + service_name + " service status is now set to " + sc.Status.ToString();
                }
                catch (InvalidOperationException)
                {
                    try
                    {
                        Console.WriteLine("Could not Stop the " + service_name + " service.");
                    }
                    catch { }
                    return "Could not Stop the " + service_name + " service.";
                }
            }
            return "Service is already Stopped";
            Thread.Sleep(500);
        }

        public string get_service_status(string service_name)
        {
            ServiceController sc = new ServiceController();
            sc.ServiceName = service_name;
            try
            {
                return "The Service is " + sc.Status.ToString();
            }
            catch
            { return service_name + "Does not exist"; }
        }

        #endregion

    }
}
