using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WiiTUIO.Output.Handlers.Touch;
using WiiTUIO.Properties;
using WiiTUIO.Provider;

namespace WiiTUIO
{
    /// <summary>
    /// Interaction logic for OverlayWindow.xaml
    /// </summary>
    public partial class OverlayWindow : Window
    {

        private WiiKeyMapper keyMapper;
        private static OverlayWindow defaultInstance;

        private bool hidden = true;
        private bool activatedOnce = false;

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

            this.Width = Util.ScreenBounds.Width;
            this.Height = Util.ScreenBounds.Height;
            this.baseGrid.Width = Util.ScreenBounds.Width;
            this.baseGrid.Height = Util.ScreenBounds.Height;

            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;

            this.baseGrid.Visibility = Visibility.Hidden;
            this.layoutChooserOverlay.Visibility = Visibility.Hidden;

            this.scrollViewer.MaxHeight = this.Height / 2-200;
            
            //Compensate for DPI settings
            Loaded += (o, e) =>
            {
                PresentationSource source = PresentationSource.FromVisual(this);
                CompositionTarget ct = source.CompositionTarget;
                Matrix transformMatrix = ct.TransformFromDevice;
                this.baseCanvas.RenderTransform = new MatrixTransform(transformMatrix);
            };
        }

        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(delegate()
            {
                this.Width = Util.ScreenBounds.Width;
                this.Height = Util.ScreenBounds.Height;
                this.baseGrid.Width = Util.ScreenBounds.Width;
                this.baseGrid.Height = Util.ScreenBounds.Height;
                UIHelpers.TopmostFix(this);
            }));
        }

        public void ShowNotice(string message, int wiimoteID)
        {
            Dispatcher.BeginInvoke(new Action(delegate()
            {
                OverlayNotice notice = new OverlayNotice(message, wiimoteID, 3000);
                this.noticeStack.Children.Add(notice);
            }), null);
        }

        public void ShowLayoutOverlay(WiiKeyMapper keyMapper)
        {
            if (this.hidden)
            {
                this.keyMapper = keyMapper;
                this.keyMapper.SwitchToDefault();
                Dispatcher.BeginInvoke(new Action(delegate()
                {

                    this.baseGrid.Opacity = 0.0;
                    this.baseGrid.Visibility = Visibility.Visible;
                    this.layoutChooserOverlay.Visibility = Visibility.Visible;
                    this.Activate();

                    Color bordercolor = CursorColor.getColor(keyMapper.WiimoteID);
                    bordercolor.ScA = 0.5f;
                    this.titleBorder.BorderBrush = new SolidColorBrush(bordercolor);

                    this.title.Text = "Choose a layout for Wiimote " + keyMapper.WiimoteID;

                    this.layoutList.Children.Clear();
                    foreach (LayoutChooserSetting config in this.keyMapper.GetLayoutList())
                    {
                        string name = config.Title;
                        string filename = config.Keymap;
                        LayoutSelectionRow row = new LayoutSelectionRow(name, filename, bordercolor);
                        row.OnClick += Select_Layout;
                        this.layoutList.Children.Add(row);
                    }

                    DoubleAnimation animation = UIHelpers.createDoubleAnimation(1.0, 200, false);
                    animation.FillBehavior = FillBehavior.HoldEnd;
                    animation.Completed += delegate(object sender, EventArgs pEvent)
                    {

                    };
                    this.baseGrid.BeginAnimation(FrameworkElement.OpacityProperty, animation, HandoffBehavior.SnapshotAndReplace);

                    this.hidden = false;
                }), null);
            }
        }

        private void Select_Layout(string filename)
        {
            this.keyMapper.SetFallbackKeymap(filename);
            this.HideOverlay();
        }

        public bool OverlayIsOn()
        {
            return !this.hidden;
        }

        public void HideOverlay()
        {
            if (!this.hidden)
            {
                this.keyMapper.SwitchToFallback();
                Dispatcher.BeginInvoke(new Action(delegate()
                {
                    DoubleAnimation animation = UIHelpers.createDoubleAnimation(0.0, 200, false);
                    animation.FillBehavior = FillBehavior.HoldEnd;
                    animation.Completed += delegate(object sender, EventArgs pEvent)
                    {
                        this.baseGrid.Visibility = Visibility.Hidden;
                    };
                    this.baseGrid.BeginAnimation(FrameworkElement.OpacityProperty, animation, HandoffBehavior.SnapshotAndReplace);
                    this.hidden = true;
                }), null);
            }
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
            this.HideOverlay();
        }

        protected override void OnActivated(EventArgs e)
        {
            if (!activatedOnce)
            {
                activatedOnce = true;
                UIHelpers.TopmostFix(this);
            }
        }

        private void layoutChooserOverlay_MouseUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }
    }
}
