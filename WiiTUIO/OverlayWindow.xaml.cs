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
            this.layoutOverlay.Visibility = Visibility.Hidden;

            this.layoutOverlay.Height = this.Height / 2;
            
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
            this.Width = Util.ScreenBounds.Width;
            this.Height = Util.ScreenBounds.Height;
            this.baseGrid.Width = Util.ScreenBounds.Width;
            this.baseGrid.Height = Util.ScreenBounds.Height;
        }

        public void ShowLayoutOverlay(WiiKeyMapper keyMapper)
        {
            this.keyMapper = keyMapper;
            Dispatcher.BeginInvoke(new Action(delegate()
            {

                this.baseGrid.Opacity = 0.0;
                this.baseGrid.Visibility = Visibility.Visible;
                this.layoutOverlay.Visibility = Visibility.Visible;
                this.Activate();

                Color bordercolor = CursorColor.getColor(keyMapper.WiimoteID);
                bordercolor.ScA = 0.5f;
                this.layoutOverlay.BorderBrush = new SolidColorBrush(bordercolor);

                this.title.Content = "Choose a layout for Wiimote "+keyMapper.WiimoteID;

                this.layoutList.Children.Clear();
                foreach(JObject config in this.keyMapper.GetLayoutList())
                {
                    string name = config.GetValue("Name").ToString();
                    string filename = config.GetValue("Keymap").ToString();
                    LayoutSelectionRow row = new LayoutSelectionRow(name, filename);
                    row.OnClick += Select_Layout;
                    this.layoutList.Children.Add(row);
                }

                DoubleAnimation animation = createDoubleAnimation(1.0, 200, false);
                animation.FillBehavior = FillBehavior.HoldEnd;
                animation.Completed += delegate(object sender, EventArgs pEvent)
                {

                };
                this.baseGrid.BeginAnimation(FrameworkElement.OpacityProperty, animation, HandoffBehavior.SnapshotAndReplace);
 
                
            }), null);
        }

        private void Select_Layout(string filename)
        {
            this.keyMapper.SetDefaultKeymap(filename);
            this.HideOverlay();
        }

        public bool OverlayIsOn()
        {
            return this.baseGrid.Visibility == Visibility.Visible;
        }

        public void HideOverlay()
        {
            Dispatcher.BeginInvoke(new Action(delegate()
            {
                DoubleAnimation animation = createDoubleAnimation(0.0, 200, false);
                animation.FillBehavior = FillBehavior.HoldEnd;
                animation.Completed += delegate(object sender, EventArgs pEvent)
                {
                    this.baseGrid.Visibility = Visibility.Hidden;
                };
                this.baseGrid.BeginAnimation(FrameworkElement.OpacityProperty, animation, HandoffBehavior.SnapshotAndReplace);

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
            this.HideOverlay();
        }

        private static DoubleAnimation createDoubleAnimation(double fNew, double fTime, bool bFreeze)
        {
            // Create the animation.
            DoubleAnimation pAction = new DoubleAnimation(fNew, new Duration(TimeSpan.FromMilliseconds(fTime)))
            {
                // Specify settings.
                AccelerationRatio = 0.1,
                DecelerationRatio = 0.9,
                FillBehavior = FillBehavior.HoldEnd
            };

            // Pause the action before starting it and then return it.
            if (bFreeze)
                pAction.Freeze();
            return pAction;
        }

        //http://social.msdn.microsoft.com/Forums/en-US/wpf/thread/cdbe457f-d653-4a18-9295-bb9b609bc4e3
        public enum GetWindowCmd : uint
        {
            GW_HWNDFIRST = 0,
            GW_HWNDLAST = 1,
            GW_HWNDNEXT = 2,
            GW_HWNDPREV = 3,
            GW_OWNER = 4,
            GW_CHILD = 5,
            GW_ENABLEDPOPUP = 6
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindow(IntPtr hWnd, GetWindowCmd uCmd);


        [Flags]
        public enum SetWindowPosFlags
        {
            SWP_NOSIZE = 0x0001,
            SWP_NOMOVE = 0x0002,
            SWP_NOZORDER = 0x0004,
            SWP_NOREDRAW = 0x0008,
            SWP_NOACTIVATE = 0x0010,
            SWP_FRAMECHANGED = 0x0020,
            SWP_SHOWWINDOW = 0x0040,
            SWP_HIDEWINDOW = 0x0080,
            SWP_NOCOPYBITS = 0x0100,
            SWP_NOOWNERZORDER = 0x0200,
            SWP_NOSENDCHANGING = 0x0400
        }

        [DllImport("user32.dll")]
        public static extern int SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, SetWindowPosFlags uFlags);

        protected override void OnActivated(EventArgs e)
        {
            IntPtr hWnd = new WindowInteropHelper(this).Handle;

            IntPtr hWndHiddenOwner = GetWindow(hWnd, GetWindowCmd.GW_OWNER);

            if (hWndHiddenOwner != IntPtr.Zero)
            {
                IntPtr HWND_TOPMOST = new IntPtr(-1);
                SetWindowPos(hWndHiddenOwner, HWND_TOPMOST, 0, 0, 0, 0,
                   SetWindowPosFlags.SWP_NOMOVE |
                   SetWindowPosFlags.SWP_NOSIZE |
                   SetWindowPosFlags.SWP_NOACTIVATE);
            }
        }
    }
}
