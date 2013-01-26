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
using WiiTUIO.Properties;

namespace WiiTUIO.Provider
{
    /// <summary>
    /// Interaction logic for WiiPointerProviderSettings.xaml
    /// </summary>
    public partial class WiiPointerProviderSettings : Window
    {
        public WiiPointerProviderSettings()
        {
            InitializeComponent();

            
        }

        private void systemCursor_Checked(object sender, RoutedEventArgs e)
        {
            Settings.Default.pointer_changeSystemCursor = true;
        }

        private void systemCursor_Unchecked(object sender, RoutedEventArgs e)
        {
            Settings.Default.pointer_changeSystemCursor = false;
        }

        private void moveCursor_Checked(object sender, RoutedEventArgs e)
        {
            Settings.Default.pointer_moveCursor = true;
        }

        private void moveCursor_Unchecked(object sender, RoutedEventArgs e)
        {
            Settings.Default.pointer_moveCursor = false;
        }

        private void Window_Closing_1(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Hide();
            e.Cancel = true;
        }

        private void Window_GotFocus_1(object sender, RoutedEventArgs e)
        {
            cbMoveCursor.IsChecked = Settings.Default.pointer_moveCursor;
            cbSystemCursor.IsChecked = Settings.Default.pointer_changeSystemCursor;
        }
    }
}
