using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Permissions;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DriverInstall
{
    public partial class MainWindow : Window
    {
        private string etd_ServiceName = "Tuio-To-vmulti-Device1";
        private string etd_ServiceFilename = "Driver\\tuio_vmulti_service.exe";
        private string edt_dataFolder = "C:\\Users\\AppData\\TUIO-To-Vmulti\\Data\\";

        private bool shutdown = false;

        public MainWindow()
        {
            InitializeComponent();

            if (Environment.GetCommandLineArgs().Contains("-silent"))
            {
                this.Visibility = Visibility.Hidden;
                this.shutdown = true;
            }

            if (Environment.GetCommandLineArgs().Contains("-install"))
            {
                if (Environment.GetCommandLineArgs().Contains("-vmulti"))
                {
                    this.installVmultiDriverComplete();
                }

                if (Environment.GetCommandLineArgs().Contains("-service"))
                {
                    this.installTuioServiceComplete();
                }

                if (Environment.GetCommandLineArgs().Contains("-scpbus"))
                {
                    this.installScpBusDriverComplete();
                }

                if (Environment.GetCommandLineArgs().Contains("-certificate"))
                {
                    this.installCert();
                }
            }
            else if (Environment.GetCommandLineArgs().Contains("-uninstall"))
            {

                if (Environment.GetCommandLineArgs().Contains("-service"))
                {
                    this.uninstallTuioServiceComplete();
                }

                if (Environment.GetCommandLineArgs().Contains("-vmulti"))
                {
                    this.uninstallVmultiDriverComplete();
                }

                if (Environment.GetCommandLineArgs().Contains("-scpbus"))
                {
                    this.uninstallScpBusDriverComplete();
                }

                if (Environment.GetCommandLineArgs().Contains("-certificate"))
                {
                    this.uninstallCert();
                }
            }

            if (shutdown)
            {
                Application.Current.Shutdown(1);
            }

            
        }

        private void consoleLine(string text) {
            this.console.Text += "\n";
            this.console.Text += text;
        }

        private void installAll()
        {
            this.installAllDriversComplete();
            this.uninstallCert();
            this.installCert();
        }

        private void uninstallAll()
        {
            this.uninstallAllDriversComplete();
            this.uninstallCert();
        }

        private void installAllDriversComplete()
        {
            this.installVmultiDriverComplete();
            this.installScpBusDriverComplete();
            this.installTuioServiceComplete();
            //this.store_settings();
            //this.uninstall_service(etd_ServiceName, etd_ServiceFilename);
            //this.install_service(etd_ServiceName, etd_ServiceFilename, "3333");
            //this.give_service_permissions(etd_ServiceName);
        }

        private void installVmultiDriverComplete()
        {
            this.uninstallVmultiDriver();
            this.uninstallVmultiDriver();
            this.installVmultiDriver();

            this.removeAllButTouch();
        }

        private void installTuioServiceComplete()
        {
            this.store_settings();
            this.uninstall_service(etd_ServiceName, etd_ServiceFilename);
            this.install_service(etd_ServiceName, etd_ServiceFilename, "3333");
            this.give_service_permissions(etd_ServiceName);
        }

        private void uninstallTuioServiceComplete()
        {
            this.uninstall_service(etd_ServiceName, etd_ServiceFilename);
        }

        private void installScpBusDriverComplete()
        {
            this.uninstallScpBusDriver();
            this.installScpBusDriver();
        }

        private void installCert()
        {
            X509Store store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            StorePermission sp = new StorePermission(PermissionState.Unrestricted);
            sp.Flags = StorePermissionFlags.OpenStore;
            sp.Assert();
            store.Open(OpenFlags.ReadWrite);
            X509Certificate2Collection collection = new X509Certificate2Collection();
            string path = System.AppDomain.CurrentDomain.BaseDirectory + "CodeSign.cer";
            X509Certificate2 cert = new X509Certificate2(path);
            byte[] encodedCert = cert.GetRawCertData();
            consoleLine("Adding Touchmote Test Certificate to trusted root.");
            store.Add(cert);
            store.Close();
        }

        private void uninstallCert()
        {
            X509Store store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            StorePermission sp = new StorePermission(PermissionState.Unrestricted);
            sp.Flags = StorePermissionFlags.OpenStore;
            sp.Assert();
            store.Open(OpenFlags.ReadWrite);
            consoleLine("Removing Touchmote Test Certificate.");
            foreach (X509Certificate2 c in store.Certificates)
            {
                if (c.IssuerName.Name.Contains("Touchmote"))
                {
                    store.Remove(c);
                }
            }
            store.Close();
        }

        private void uninstallAllDriversComplete()
        {
            uninstallVmultiDriverComplete();
            uninstallScpBusDriverComplete();
            uninstallTuioServiceComplete();
        }

        private void uninstallVmultiDriverComplete()
        {
            this.uninstallVmultiDriver();
            this.uninstallVmultiDriver();
        }

        private void uninstallScpBusDriverComplete()
        {
            this.uninstallScpBusDriver();
        }

        private void installVmultiDriver()
        {
            try
            {
                //Devcon install vmultia.inf ecologylab\vmultia
                System.Diagnostics.ProcessStartInfo procStartInfo =
                    new System.Diagnostics.ProcessStartInfo();

                procStartInfo.WorkingDirectory = System.AppDomain.CurrentDomain.BaseDirectory + "Driver\\";

                procStartInfo.FileName = procStartInfo.WorkingDirectory + "devcon";
                procStartInfo.Arguments = "install vmultia.inf ecologylab\\vmultia";
                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.UseShellExecute = false;
                procStartInfo.CreateNoWindow = true;
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo = procStartInfo;
                proc.Start();
                string result = proc.StandardOutput.ReadToEnd();
                consoleLine(result);
                proc.WaitForExit();
            }
            catch (Exception objException)
            {
                consoleLine(objException.Message);
            }
        }

        private void installScpBusDriver()
        {
            try
            {
                //Devcon install vmultia.inf ecologylab\vmultia
                System.Diagnostics.ProcessStartInfo procStartInfo =
                    new System.Diagnostics.ProcessStartInfo();

                procStartInfo.WorkingDirectory = System.AppDomain.CurrentDomain.BaseDirectory + "Driver\\";

                procStartInfo.FileName = procStartInfo.WorkingDirectory + "devcon";
                procStartInfo.Arguments = "install ScpVBus.inf root\\ScpVBus";
                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.UseShellExecute = false;
                procStartInfo.CreateNoWindow = true;
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo = procStartInfo;
                proc.Start();
                string result = proc.StandardOutput.ReadToEnd();
                consoleLine(result);
                proc.WaitForExit();
            }
            catch (Exception objException)
            {
                consoleLine(objException.Message);
            }
        }

        private void uninstallVmultiDriver()
        {
            try
            {
                //Devcon remove *multi*
                System.Diagnostics.ProcessStartInfo procStartInfo =
                    new System.Diagnostics.ProcessStartInfo();

                procStartInfo.WorkingDirectory = System.AppDomain.CurrentDomain.BaseDirectory + "Driver\\";

                procStartInfo.FileName = procStartInfo.WorkingDirectory + "devcon";
                procStartInfo.Arguments = "remove *vmulti*";

                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.UseShellExecute = false;
                procStartInfo.CreateNoWindow = true;
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo = procStartInfo;
                proc.Start();
                string result = proc.StandardOutput.ReadToEnd();
                consoleLine(result);
                proc.WaitForExit();
            }
            catch (Exception objException)
            {
                consoleLine(objException.Message);
            }
        }

        private void uninstallScpBusDriver()
        {
            try
            {
                //Devcon remove *multi*
                System.Diagnostics.ProcessStartInfo procStartInfo =
                    new System.Diagnostics.ProcessStartInfo();

                procStartInfo.WorkingDirectory = System.AppDomain.CurrentDomain.BaseDirectory + "Driver\\";

                procStartInfo.FileName = procStartInfo.WorkingDirectory + "devcon";
                procStartInfo.Arguments = "remove *Scp*";

                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.UseShellExecute = false;
                procStartInfo.CreateNoWindow = true;
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo = procStartInfo;
                proc.Start();
                string result = proc.StandardOutput.ReadToEnd();
                consoleLine(result);
                proc.WaitForExit();
            }
            catch (Exception objException)
            {
                consoleLine(objException.Message);
            }
        }

        private void removeAllButTouch()
        {
            for (int i = 2; i <= 9; i++)
            {
                try
                {
                    //Devcon remove *multi*
                    System.Diagnostics.ProcessStartInfo procStartInfo =
                        new System.Diagnostics.ProcessStartInfo();

                    procStartInfo.WorkingDirectory = System.AppDomain.CurrentDomain.BaseDirectory + "Driver\\";

                    procStartInfo.FileName = procStartInfo.WorkingDirectory + "devcon";
                    procStartInfo.Arguments = "disable *vmulti*COL0"+i+"*";

                    procStartInfo.RedirectStandardOutput = true;
                    procStartInfo.UseShellExecute = false;
                    procStartInfo.CreateNoWindow = true;
                    System.Diagnostics.Process proc = new System.Diagnostics.Process();
                    proc.StartInfo = procStartInfo;
                    proc.Start();
                    string result = proc.StandardOutput.ReadToEnd();
                    consoleLine(result);
                    proc.WaitForExit();
                }
                catch (Exception objException)
                {
                    consoleLine(objException.Message);
                }
            }
        }
        //As described here
        //http://stackoverflow.com/questions/4436558/start-stop-a-windows-service-from-a-non-administrator-user-account
        private void give_service_permissions(string service_name)
        {

            //LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList RegistryKey
            string keyName = "LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\ProfileList";
            RegistryKey profileList = Registry.LocalMachine.OpenSubKey(keyName);
            WindowsIdentity user = WindowsIdentity.GetCurrent();
            SecurityIdentifier sid = user.User;
            consoleLine(sid.Value);

            string service_permissions = sdshow_service(service_name);
            string permission_string = "(A;;RPWPCR;;;" + sid.Value + ")";
            consoleLine(permission_string);
            service_permissions = service_permissions.Replace(Environment.NewLine, "");
            service_permissions = service_permissions.Replace(permission_string,"");
            service_permissions = service_permissions.Replace("S:", permission_string + "S:");

            sdset_service(service_name, service_permissions);
        }

        //sc sdshow Tuio-To-vmulti-Device1
        public string sdshow_service(string service_name)
        {
            try
            {
                System.Diagnostics.ProcessStartInfo procStartInfo =
                    new System.Diagnostics.ProcessStartInfo();

                procStartInfo.FileName = "sc";
                procStartInfo.Arguments = "sdshow " + service_name;
                consoleLine("running " + procStartInfo.FileName + " " + procStartInfo.Arguments);
                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.UseShellExecute = false;
                procStartInfo.CreateNoWindow = true;
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo = procStartInfo;
                proc.Start();
                return proc.StandardOutput.ReadToEnd();
            }
            catch (Exception objException)
            {
                consoleLine(objException.Message);
                return "";
            }
        }

        public void sdset_service(string service_name, string permission_string)
        {
            try
            {
                System.Diagnostics.ProcessStartInfo procStartInfo =
                    new System.Diagnostics.ProcessStartInfo();

                procStartInfo.FileName = "sc";
                procStartInfo.Arguments = "sdset " + service_name + " " + permission_string;
                consoleLine("running " + procStartInfo.FileName + " " + procStartInfo.Arguments);
                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.UseShellExecute = false;
                procStartInfo.CreateNoWindow = true;
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo = procStartInfo;
                proc.Start();
            }
            catch (Exception objException)
            {
                consoleLine(objException.Message);
            }
        }

        private void store_settings()
        {

            System.IO.Directory.CreateDirectory(this.edt_dataFolder);

            System.IO.File.WriteAllText(this.edt_dataFolder + "tuioport1.txt", "3333");
            System.IO.File.WriteAllText(this.edt_dataFolder + "inverthorizontal1.txt", "false");
            System.IO.File.WriteAllText(this.edt_dataFolder + "invertverticle1.txt", "false");
            System.IO.File.WriteAllText(this.edt_dataFolder + "swapxy1.txt", "false");
            System.IO.File.WriteAllText(this.edt_dataFolder + "xrange_min1.txt", "0");
            System.IO.File.WriteAllText(this.edt_dataFolder + "xrange_max1.txt", "1");

            System.IO.File.WriteAllText(this.edt_dataFolder + "yrange_min1.txt", "0");
            System.IO.File.WriteAllText(this.edt_dataFolder + "yrange_max1.txt", "1");
            System.IO.File.WriteAllText(this.edt_dataFolder + "x01.txt", "0");
            System.IO.File.WriteAllText(this.edt_dataFolder + "y01.txt", "0");
            System.IO.File.WriteAllText(this.edt_dataFolder + "service1.txt", "");

        }

        private void install_service(string service_name, string file_name, string port)
        {
            consoleLine("Installing service : " + service_name + " : " + file_name + " , port : " + port);
            //status = sc.Status.ToString();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = file_name;
            startInfo.Arguments = "install " + port;
            startInfo.UseShellExecute = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.CreateNoWindow = true;
            using (Process exeProcess = Process.Start(startInfo))
            {
                exeProcess.WaitForExit();
            }

        }

        private void uninstall_service(string service_name, string file_name)
        {
            consoleLine("Uninstalling service : " + service_name + " : " + file_name);
            //Process Process_Remove = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = file_name;
            startInfo.Arguments = "remove " + "3";
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = true;

            startInfo.WindowStyle = ProcessWindowStyle.Hidden;

            using (Process exeProcess = Process.Start(startInfo))
            {
                exeProcess.WaitForExit();
            }
        }

        private void btnInstall_Click(object sender, RoutedEventArgs e)
        {
            this.installAllDriversComplete();
        }

        private void btnUninstall_Click(object sender, RoutedEventArgs e)
        {
            this.uninstallAllDriversComplete();
        }

    }
}
