using HidLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WiiCPP;
using WiiTUIO.DeviceUtils;
using WiiTUIO.Output;
using WiiTUIO.Properties;

namespace WiiTUIO.Provider
{
    /// <summary>
    /// Interaction logic for WiiPointerProviderSettings.xaml
    /// </summary>
    public partial class WiiPointerProviderSettings : UserControl
    {
        bool initializing = true;

        public WiiPointerProviderSettings()
        {
            InitializeComponent();

            //this.cbSystemCursor.IsChecked = Settings.Default.pointer_changeSystemCursor;
            //this.cbMoveCursor.IsChecked = Settings.Default.pointer_moveCursor;
            if (Settings.Default.pointer_sensorBarPos == "top")
            {
                this.cbiTop.IsSelected = true;
            }
            else if (Settings.Default.pointer_sensorBarPos == "bottom")
            {
                this.cbiBottom.IsSelected = true;
            }
            else
            {
                this.cbiCenter.IsSelected = true;
            }

            if(VmultiDevice.Current.isAvailable())
            {
                string currentMonitor = VmultiUtil.getCurrentMonitorDevicePath();

                IEnumerable<MonitorInfo> monInfos = DeviceUtil.GetMonitorList();

                foreach (MonitorInfo monInfo in monInfos)
                {
                    ComboBoxItem cbItem = new ComboBoxItem();
                    cbItem.Content = monInfo.FriendlyName;
                    cbItem.DataContext = monInfo;
                    this.MonitorComboBox.Items.Add(cbItem);

                    if (monInfo.DevicePath == currentMonitor)
                    {
                        this.MonitorComboBox.SelectedItem = cbItem;
                    }
                }
                this.initializing = false;
            }
            else
            {
                ComboBoxItem cbItem = new ComboBoxItem();
                cbItem.Content = "Requires driver";
                this.MonitorComboBox.Items.Add(cbItem);
                this.MonitorComboBox.IsEnabled = false;
                this.MonitorComboBox.SelectedIndex = 0;
                this.initializing = false;
            }

        }

        private void SBPositionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.initializing)
            {
                if (this.cbiTop.IsSelected)
                {
                    Settings.Default.pointer_sensorBarPos = "top";
                }
                else if (this.cbiBottom.IsSelected)
                {
                    Settings.Default.pointer_sensorBarPos = "bottom";
                }
                else
                {
                    Settings.Default.pointer_sensorBarPos = "center";
                }
            }
        }

        private void MonitorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.initializing)
            {
                MonitorInfo monInfo = ((ComboBoxItem)this.MonitorComboBox.SelectedItem).DataContext as MonitorInfo;
                if(monInfo != null)
                {
                    VmultiUtil.setCurrentMonitor(monInfo);
                }
            }
        }
        /*
        private void systemCursor_Checked(object sender, RoutedEventArgs e)
        {
            Settings.Default.pointer_changeSystemCursor = true;
        }

        private void systemCursor_Unchecked(object sender, RoutedEventArgs e)
        {
            Settings.Default.pointer_changeSystemCursor = false;
        }
        */
        /*
        private void moveCursor_Checked(object sender, RoutedEventArgs e)
        {
            Settings.Default.pointer_moveCursor = true;
        }

        private void moveCursor_Unchecked(object sender, RoutedEventArgs e)
        {
            Settings.Default.pointer_moveCursor = false;
        }
         * */

    }
}
