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
        public int battery;
        public bool powersave;
        

        public WiimoteStatusUC(int id)
        {
            InitializeComponent();
            this.ID = id;
            this.lbId.Content = ""+id;
            this.setBattery(0);
        }

        public void updateStatus(WiimoteStatus status)
        {
            //this.lbId.Content = "" + status.ID;
            if (this.battery != status.Battery)
            {
                this.battery = status.Battery;
                this.setBattery(status.Battery);
            }
            if (this.powersave != status.InPowerSave)
            {
                this.powersave = status.InPowerSave;
                if (status.InPowerSave)
                {
                    this.lbStatus.Foreground = Brushes.Aquamarine;
                    this.lbStatus.Content = "power save";
                }
                else
                {
                    this.lbStatus.Foreground = Brushes.Green;
                    this.lbStatus.Content = "connected";
                }
            }
        }

        public void setBattery(int percentage) {
            Brush light = Brushes.White;
            Brush dark = Brushes.Gray;
            this.battery1.Fill = percentage > 10 ? light : dark;
            this.battery2.Fill = percentage > 20 ? light : dark;
            this.battery3.Fill = percentage > 30 ? light : dark;
            this.battery4.Fill = percentage > 40 ? light : dark;
            this.battery5.Fill = percentage > 50 ? light : dark;
            this.battery6.Fill = percentage > 70 ? light : dark;
        }
    }
}
