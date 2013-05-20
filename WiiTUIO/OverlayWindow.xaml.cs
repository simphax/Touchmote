using Microsoft.Win32;
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
using System.Windows.Shapes;
using WiiTUIO.Provider;

namespace WiiTUIO
{
    /// <summary>
    /// Interaction logic for OverlayWindow.xaml
    /// </summary>
    public partial class OverlayWindow : Window
    {

        private static OverlayWindow defaultInstance;

        public static OverlayWindow Current
        {
            get
            {
                if (defaultInstance == null)
                {
                    defaultInstance = new OverlayWindow();
                }
                return defaultInstance;
            }
        }

        private OverlayWindow()
        {
            InitializeComponent();
            this.Width = System.Windows.SystemParameters.PrimaryScreenWidth;
            this.Height = System.Windows.SystemParameters.PrimaryScreenHeight;

            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;

            this.layoutOverlay.Visibility = Visibility.Hidden;
        }

        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            this.Width = Util.ScreenBounds.Width;
            this.Height = Util.ScreenBounds.Height;
        }

        public void ShowLayoutOverlay(int wiimoteId)
        {
            Dispatcher.BeginInvoke(new Action(delegate()
            {
                this.layoutOverlay.Visibility = Visibility.Visible;
            }), null);
        }

        public bool LayoutOverlayIsVisible()
        {
            return this.layoutOverlay.Visibility == Visibility.Visible;
        }

        public void HideLayoutOverlay()
        {
            Dispatcher.BeginInvoke(new Action(delegate()
            {
                this.layoutOverlay.Visibility = Visibility.Hidden;
            }), null);
        }

        private void Border_MouseEnter(object sender, MouseEventArgs e)
        {
            Border border = (Border)sender;
            border.BorderBrush = Brushes.Gray;
        }

        private void Border_MouseLeave(object sender, MouseEventArgs e)
        {
            Border border = (Border)sender;
            border.BorderBrush = new SolidColorBrush(Color.FromRgb(35,35,35));
        }

        private void Border_TouchEnter(object sender, TouchEventArgs e)
        {
            Border border = (Border)sender;
            border.BorderBrush = Brushes.Gray;
        }

        private void Border_TouchLeave(object sender, TouchEventArgs e)
        {
            Border border = (Border)sender;
            border.BorderBrush = new SolidColorBrush(Color.FromRgb(35, 35, 35));
        }

        private void Grid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            //this.layoutOverlay.Visibility = Visibility.Hidden;
        }
    }
}
