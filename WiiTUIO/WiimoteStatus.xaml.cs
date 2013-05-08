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
using WiiTUIO.Provider;

namespace WiiTUIO
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class WiimoteStatusUC : UserControl
    {
        public int ID;

        public WiimoteStatusUC(int id)
        {
            InitializeComponent();
            this.ID = id;
            this.lbId.Content = ""+id;
            this.setBattery(0);
        }

        public void updateStatus(WiimoteStatus status)
        {
            this.lbId.Content = "" + status.ID;
            this.setBattery(status.Battery);
            if (status.InPowerSave)
            {
                this.lbStatus.Content = "power save";
            }
            else
            {
                this.lbStatus.Content = "connected";
            }
        }

        public void setBattery(int percentage) {
            Brush light = Brushes.White;
            Brush dark = Brushes.Gray;
            this.battery1.Fill = dark;
            this.battery2.Fill = dark;
            this.battery3.Fill = dark;
            this.battery4.Fill = dark;
            this.battery5.Fill = dark;
            this.battery6.Fill = dark;
        }
    }
}
