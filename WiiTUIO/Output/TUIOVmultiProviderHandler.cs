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

namespace WiiTUIO.Output
{
    class TUIOVmultiProviderHandler : IProviderHandler
    {
        public event Action OnConnect;

        public event Action OnDisconnect;

        private TUIOProviderHandler TUIOHandler;

        private string etd_SetviceName = "Tuio-To-vmulti-Device1";
        private string etd_SetviceFilename = "Driver\\Tuio-to-Vmulti-Service-1.exe";
        private string edt_dataFolder;


        public TUIOVmultiProviderHandler()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
            this.edt_dataFolder = (new FileInfo(config.FilePath)).DirectoryName + "\\";
            this.TUIOHandler = new TUIOProviderHandler();
            do_apply_stuff();
        }

        public void connect()
        {
            //start tuio-to-vmulti service
            start_service(etd_SetviceName);
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
            stop_service(etd_SetviceName);
            this.TUIOHandler.disconnect();
            OnDisconnect();
        }

        public void showSettingsWindow()
        {
            this.TUIOHandler.showSettingsWindow();
        }

        #region Code from EcoTUIOdriver
        private void do_apply_stuff()
        {
            Console.WriteLine("Data folder = " + this.edt_dataFolder);

            System.IO.Directory.CreateDirectory(this.edt_dataFolder);

            System.IO.File.WriteAllText(this.edt_dataFolder + "tuioport1.txt", Settings.Default.tuio_port.ToString());
            System.IO.File.WriteAllText(this.edt_dataFolder + "inverthorizontal1.txt", "false");
            System.IO.File.WriteAllText(this.edt_dataFolder + "invertverticle1.txt", "false");
            System.IO.File.WriteAllText(this.edt_dataFolder + "swapxy1.txt", "false");
            System.IO.File.WriteAllText(this.edt_dataFolder + "xrange_min1.txt", "0");
            System.IO.File.WriteAllText(this.edt_dataFolder + "xrange_max1.txt", "1");

            System.IO.File.WriteAllText(this.edt_dataFolder + "yrange_min1.txt", "0");
            System.IO.File.WriteAllText(this.edt_dataFolder + "yrange_max1.txt", "1");
            System.IO.File.WriteAllText(this.edt_dataFolder + "x01.txt", "0");
            System.IO.File.WriteAllText(this.edt_dataFolder + "y01.txt", "0");
            System.IO.File.WriteAllText(this.edt_dataFolder + "service1.txt", get_service_status(etd_SetviceName));

            //Installs service if it's not already installed . 
            //install_service(etd_SetviceName, etd_SetviceFilename, Settings.Default.tuio_port.ToString());

        }



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
