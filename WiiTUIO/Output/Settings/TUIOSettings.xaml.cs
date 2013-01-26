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

namespace WiiTUIO.Output
{
    /// <summary>
    /// Interaction logic for TUIOSettings.xaml
    /// </summary>
    public partial class TUIOSettings : Window
    {

        private TUIOProviderHandler parent = null;

        public TUIOSettings(TUIOProviderHandler handler)
        {

            InitializeComponent();

            this.parent = handler;
            tbIP.Text = WiiTUIO.Properties.Settings.Default.tuio_IP;
            tbPort.Text = WiiTUIO.Properties.Settings.Default.tuio_port.ToString();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            WiiTUIO.Properties.Settings.Default.tuio_IP = tbIP.Text;
            WiiTUIO.Properties.Settings.Default.tuio_port = Int32.Parse(tbPort.Text);

            parent.disconnect();
            parent.connect();
            this.Hide();

        }

        private void Window_Closing_1(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Hide();
            e.Cancel = true;
        }

        private void Window_GotFocus_1(object sender, RoutedEventArgs e)
        {
            //tbIP.Text = WiiTUIO.Properties.Settings.Default.tuio_IP;
            //tbPort.Text = WiiTUIO.Properties.Settings.Default.tuio_port.ToString();
        }


    }
}
