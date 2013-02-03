using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

        public MainWindow()
        {
            InitializeComponent();

        }

        private void consoleLine(string text) {
            this.console.Text += "\n";
            this.console.Text += text;
        }

        private void installAll()
        {

            this.installDriver();
            this.install_service(etd_ServiceName, etd_ServiceFilename, "3333");
            this.give_service_permissions(etd_ServiceName);
        }

        private void uninstallAll()
        {

            this.uninstallDriver();
            this.uninstall_service(etd_ServiceName,etd_ServiceFilename);
        }

        private void installDriver()
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

        private void uninstallDriver()
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
                consoleLine(proc.StandardOutput.ReadToEnd());
            }
            catch (Exception objException)
            {
                consoleLine(objException.Message);
            }
        }

        private void install_service(string service_name, string file_name, string port)
        {
            consoleLine("Installing service : " + service_name + " : " + file_name + " , port : " + port);

            this.uninstall_service(service_name,file_name);

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
            this.installAll();
        }

        private void btnUninstall_Click(object sender, RoutedEventArgs e)
        {
            this.uninstallAll();
        }

    }
}
