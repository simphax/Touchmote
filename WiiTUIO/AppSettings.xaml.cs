using System;
using System.Collections.Generic;
using System.Linq;
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
using TCD.System.ApplicationExtensions;
using WiiTUIO.Input;
using WiiTUIO.Output;
using WiiTUIO.Properties;
using WiiTUIO.Provider;

namespace WiiTUIO
{
    /// <summary>
    /// Interaction logic for AppSettings.xaml
    /// </summary>
    public partial class AppSettingsUC : UserControl, SubPanel
    {

        public event Action OnClose;

        public AppSettingsUC()
        {
            InitializeComponent();
            this.Initialize();
        }

        public async void Initialize()
        {
            Settings.Default.PropertyChanged += Settings_PropertyChanged;

            this.reloadState();
        }

        private async void reloadState()
        {
            this.cbMinimizeOnStart.IsChecked = Settings.Default.minimizeOnStart;
            this.cbMinimizeToTray.IsChecked = Settings.Default.minimizeToTray;
            this.cbPairOnStart.IsChecked = Settings.Default.pairOnStart;

            InputFactory.InputType inputType = InputFactory.getType(Settings.Default.input);

            switch (inputType)
            {
                case InputFactory.InputType.POINTER:
                    this.cbiPointer.IsSelected = true;
                    break;
                case InputFactory.InputType.PEN:
                    this.cbiPen.IsSelected = true;
                    break;
            }
            this.providerSettingsContent.Children.Clear();
            this.providerSettingsContent.Children.Add(MultiWiiPointerProvider.getSettingsControl());

            this.cbWindowsStart.IsChecked = await ApplicationAutostart.IsAutostartAsync("Touchmote");
        }

        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            this.reloadState();
        }

        private async void cbWindowsStart_Checked(object sender, RoutedEventArgs e)
        {
            this.cbWindowsStart.IsChecked = await ApplicationAutostart.SetAutostartAsync(true, "Touchmote", "", "", true);
        }

        private async void cbWindowsStart_Unchecked(object sender, RoutedEventArgs e)
        {
            this.cbWindowsStart.IsChecked = !(await ApplicationAutostart.SetAutostartAsync(false, "Touchmote", "", "", true));
        }

        private void btnAppSettingsBack_Click(object sender, RoutedEventArgs e)
        {
            if (this.OnClose != null)
            {
                this.OnClose();
            }
        }

        private void ComboBox_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if (ModeComboBox.SelectedItem != null && ((ComboBoxItem)ModeComboBox.SelectedItem).Content != null)
            {
                ComboBoxItem cbItem = (ComboBoxItem)ModeComboBox.SelectedItem;
                if (cbItem == cbiPointer)
                {
                    Settings.Default.input = InputFactory.getType(InputFactory.InputType.POINTER);
                }
                else if (cbItem == cbiPen)
                {
                    Settings.Default.input = InputFactory.getType(InputFactory.InputType.PEN);
                }
            }
        }

        private void cbPairOnStart_Checked(object sender, RoutedEventArgs e)
        {
            Settings.Default.pairOnStart = true;
        }

        private void cbPairOnStart_Unchecked(object sender, RoutedEventArgs e)
        {
            Settings.Default.pairOnStart = false;
        }

        private void cbMinimizeToTray_Checked(object sender, RoutedEventArgs e)
        {
            Settings.Default.minimizeToTray = true;
        }

        private void cbMinimizeToTray_Unchecked(object sender, RoutedEventArgs e)
        {
            Settings.Default.minimizeToTray = false;
        }

        private void cbMinimizeOnStart_Checked(object sender, RoutedEventArgs e)
        {
            Settings.Default.minimizeOnStart = true;
        }

        private void cbMinimizeOnStart_Unchecked(object sender, RoutedEventArgs e)
        {
            Settings.Default.minimizeOnStart = false;
        }
    }
}
