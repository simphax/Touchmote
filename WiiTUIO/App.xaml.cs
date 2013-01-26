using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

using Hardcodet.Wpf.TaskbarNotification;
using System.Windows.Controls;

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
        public static TaskbarIcon TB { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            // Initialise the Tray Icon
            TB = (TaskbarIcon)FindResource("tbNotifyIcon");
            TB.ShowBalloonTip("Touchmote is running", "Click here to set up", BalloonIcon.Info);

            Application.Current.Exit += appWillExit;

            base.OnStartup(e);
        }

        private void appWillExit(object sender, ExitEventArgs e)
        {
            WiiTUIO.Properties.Settings.Default.Save();
            TB.Dispose();
        }


        private void mnuExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown(0);
        }

        private void TaskbarIcon_TrayBalloonTipClicked_1(object sender, RoutedEventArgs e)
        {
            TB.ShowTrayPopup();
        }
    }
}
