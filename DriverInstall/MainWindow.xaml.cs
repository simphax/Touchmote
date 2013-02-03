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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string etd_SetviceName = "Tuio-To-vmulti-Device1";
        private string etd_SetviceFilename = "Driver\\tuio_vmulti_service.exe";

        public MainWindow()
        {
            InitializeComponent();


            this.install_service(etd_SetviceName, etd_SetviceFilename,"3333");
            this.give_service_permissions(etd_SetviceName);
        }

        private void consoleLine(string text) {
            this.console.Text += "\n";
            this.console.Text += text;
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

            //sc sdshow Tuio-To-vmulti-Device1
            string service_permissions = sdshow_service(service_name);
            string permission_string = "(A;;RPWPCR;;;" + sid.Value + ")";
            consoleLine(permission_string);
            //Get rid of the newlines
            service_permissions = service_permissions.Replace(Environment.NewLine, "");
            //In case we have already set permissions
            service_permissions = service_permissions.Replace(permission_string,"");
            //Add in the new permissions before the elevated permissions
            service_permissions = service_permissions.Replace("S:", permission_string + "S:");

            consoleLine(service_permissions);
            consoleLine(sdset_service(service_name, service_permissions));
        }

        public string sdshow_service(string service_name)
        {
            try
            {
                System.Diagnostics.ProcessStartInfo procStartInfo =
                    new System.Diagnostics.ProcessStartInfo();

                procStartInfo.FileName = "sc";
                procStartInfo.Arguments = "sdshow " + service_name;
                consoleLine("running " + procStartInfo.FileName + " " + procStartInfo.Arguments);
                // The following commands are needed to redirect the standard output.
                // This means that it will be redirected to the Process.StandardOutput StreamReader.
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
                // Log the exception
            }
        }

        public string sdset_service(string service_name, string permission_string)
        {
            try
            {
                System.Diagnostics.ProcessStartInfo procStartInfo =
                    new System.Diagnostics.ProcessStartInfo();

                procStartInfo.FileName = "sc";
                procStartInfo.Arguments = "sdset " + service_name + " " + permission_string;
                consoleLine("running " + procStartInfo.FileName + " " + procStartInfo.Arguments);
                // The following commands are needed to redirect the standard output.
                // This means that it will be redirected to the Process.StandardOutput StreamReader.
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
                // Log the exception
            }
        }

        private void install_service(string service_name, string file_name, string port)
        {
            consoleLine("Installing service : " + service_name + " : " + file_name + " , port : " + port);
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


            //status = sc.Status.ToString();
            ProcessStartInfo startInfo2 = new ProcessStartInfo();
            startInfo2.FileName = file_name;
            startInfo2.Arguments = "install " + port;
            startInfo2.UseShellExecute = true;
            startInfo2.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo2.CreateNoWindow = true;
            using (Process exeProcess2 = Process.Start(startInfo2))
            {
                exeProcess2.WaitForExit();
            }

        }



    }
}
