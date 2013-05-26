using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

//using Hardcodet.Wpf.TaskbarNotification;
using System.Windows.Controls;
using System.Diagnostics;

namespace WiiTUIO
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// The tray's taskbar icon
        /// </summary>
       // public static TaskbarIcon TB { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            Process thisProc = Process.GetCurrentProcess();
            if (Process.GetProcessesByName(thisProc.ProcessName).Length > 1)
            {
                MessageBox.Show("Touchmote is already running. Look for it in the taskbar.");
                Application.Current.Shutdown(220);
                return;
            }

            // Initialise the Tray Icon
            //TB = (TaskbarIcon)FindResource("tbNotifyIcon");
            //TB.ShowBalloonTip("Touchmote is running", "Click here to set it up", BalloonIcon.Info);

            Application.Current.Exit += appWillExit;

            base.OnStartup(e);
        }

        private void appWillExit(object sender, ExitEventArgs e)
        {
            if (e.ApplicationExitCode != 220)
            {
                WiiTUIO.Properties.Settings.Default.Save();
                //TB.Dispose();
                SystemProcessMonitor.Default.Dispose();
            }
        }


        private void mnuExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown(0);
        }
        /*
        private void TaskbarIcon_TrayBalloonTipClicked_1(object sender, RoutedEventArgs e)
        {
            TB.ShowTrayPopup();
        }
         * */
    }
}

