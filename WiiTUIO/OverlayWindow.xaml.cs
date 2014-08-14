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
using WiiTUIO.DeviceUtils;
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

        private System.Windows.Forms.Screen primaryScreen;

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

            primaryScreen = DeviceUtil.GetScreen(Settings.Default.primaryMonitor);
            
            Settings.Default.PropertyChanged += SettingsChanged;
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;

            this.baseGrid.Visibility = Visibility.Hidden;
            this.layoutChooserOverlay.Visibility = Visibility.Hidden;

            //Compensate for DPI settings
            
            Loaded += (o, e) =>
            {
                //PresentationSource source = PresentationSource.FromVisual(this);
                //CompositionTarget ct = source.CompositionTarget;
                //Matrix transformMatrix = ct.TransformFromDevice;
                //this.baseCanvas.RenderTransform = new MatrixTransform(transformMatrix);

                this.updateWindowToScreen(primaryScreen);
            };
            
        }

        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            this.primaryScreen = DeviceUtils.DeviceUtil.GetScreen(Settings.Default.primaryMonitor);
            Dispatcher.BeginInvoke(new Action(delegate()
            {
                this.updateWindowToScreen(primaryScreen);
            }));
        }

        private void updateWindowToScreen(System.Windows.Forms.Screen screen)
        {
            Console.WriteLine("Setting overlay window position to " + screen.Bounds);
            //this.Left = screen.Bounds.X;
            //this.Top = screen.Bounds.Y;
            PresentationSource source = PresentationSource.FromVisual(this);
            Matrix transformMatrix = source.CompositionTarget.TransformToDevice;

            this.Width = screen.Bounds.Width * transformMatrix.M22;
            this.Height = screen.Bounds.Height * transformMatrix.M11;
            this.scrollViewer.MaxHeight = this.Height - 200;
            UIHelpers.SetWindowPos((new WindowInteropHelper(this)).Handle, IntPtr.Zero, screen.Bounds.X, screen.Bounds.Y, screen.Bounds.Width, screen.Bounds.Height, UIHelpers.SetWindowPosFlags.SWP_NOACTIVATE | UIHelpers.SetWindowPosFlags.SWP_NOZORDER);
            this.baseGrid.Width = this.Width;
            this.baseGrid.Height = this.Height;
            this.baseCanvas.Width = this.Width;
            this.baseCanvas.Height = this.Height;
            UIHelpers.TopmostFix(this);
        }

        private void SettingsChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "primaryMonitor")
            {
                primaryScreen = DeviceUtil.GetScreen(Settings.Default.primaryMonitor);
                Dispatcher.BeginInvoke(new Action(delegate()
                {
                    this.updateWindowToScreen(primaryScreen);
                }));
            }
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
                this.keyMapper.OnButtonDown += keyMapper_OnButtonDown;
                this.keyMapper.OnButtonUp += keyMapper_OnButtonUp;
                Dispatcher.BeginInvoke(new Action(delegate()
                {

                    this.baseGrid.Opacity = 0.0;
                    this.baseGrid.Visibility = Visibility.Visible;
                    this.layoutChooserOverlay.Visibility = Visibility.Visible;
                    this.Activate();

                    Color bordercolor = CursorColor.getColor(keyMapper.WiimoteID);
                    //bordercolor.ScA = 0.5f;
                    bordercolor.R = (byte)(bordercolor.R * 0.8);
                    bordercolor.G = (byte)(bordercolor.G * 0.8);
                    bordercolor.B = (byte)(bordercolor.B * 0.8);
                    this.titleBorder.BorderBrush = new SolidColorBrush(bordercolor);

                    this.title.Text = "Choose a layout for Wiimote " + keyMapper.WiimoteID;
                    //this.title.Foreground = new SolidColorBrush(bordercolor);

                    this.layoutList.Children.Clear();
                    foreach (LayoutChooserSetting config in this.keyMapper.GetLayoutList())
                    {
                        string name = config.Title;
                        string filename = config.Keymap;
                        LayoutSelectionRow row = new LayoutSelectionRow(name, filename, bordercolor);
                        //row.OnClick += row_OnClick;

                        if(this.keyMapper.GetFallbackKeymap().Equals(filename))
                        {
                            row.setSelected(true);
                        }

                        row.MouseDown += row_MouseDown;
                        row.TouchDown += row_TouchDown;
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

        private void row_TouchDown(object sender, TouchEventArgs e)
        {
            for (int i = this.layoutList.Children.Count - 1; i >= 0; i--)
            {
                LayoutSelectionRow row = this.layoutList.Children[i] as LayoutSelectionRow;
                row.setSelected(false);
            }
            LayoutSelectionRow senderRow = sender as LayoutSelectionRow;
            senderRow.setSelected(true);
        }

        void row_MouseDown(object sender, MouseButtonEventArgs e)
        {
            for (int i = this.layoutList.Children.Count - 1; i >= 0; i--)
            {
                LayoutSelectionRow row = this.layoutList.Children[i] as LayoutSelectionRow;
                row.setSelected(false);
            }
            LayoutSelectionRow senderRow = sender as LayoutSelectionRow;
            senderRow.setSelected(true);
        }
        /*
        private void row_OnClick(string keymap)
        {
            if (!this.hidden)
            {
                Select_Layout(keymap);
            }
        }
        */
        private void keyMapper_OnButtonUp(WiiButtonEvent e)
        {
            if(!hidden)
            {
                if (e.Button.ToLower().Equals("down"))
                {
                    highlightNext();
                }
                else if (e.Button.ToLower().Equals("up"))
                {
                    highlightPrev();
                }
                else if (e.Button.ToLower().Equals("right") || e.Button.ToLower().Equals("a"))
                {
                    selectHighlighted();
                }
                
            }
        }

        private void keyMapper_OnButtonDown(WiiButtonEvent e)
        {
            if(!hidden)
            {

            }
        }

        private void highlightNext()
        {
            Dispatcher.BeginInvoke(new Action(delegate()
            {
                int selectedIndex = -2;
                for (int i = 0; i < this.layoutList.Children.Count; i++)
                {
                    LayoutSelectionRow row = this.layoutList.Children[i] as LayoutSelectionRow;
                    if (row.isSelected())
                    {
                        selectedIndex = i;
                        row.setSelected(false);
                    }
                    if (i == selectedIndex + 1)
                    {
                        row.setSelected(true);
                    }
                }
                if (selectedIndex == this.layoutList.Children.Count-1)
                {
                    LayoutSelectionRow row = this.layoutList.Children[0] as LayoutSelectionRow;
                    row.setSelected(true);
                }
                else if (selectedIndex == -2)//If no row was found selected
                {
                    LayoutSelectionRow row = this.layoutList.Children[0] as LayoutSelectionRow;
                    row.setSelected(true);
                }
            }));
        }

        private void highlightPrev()
        {
            Dispatcher.BeginInvoke(new Action(delegate()
            {
                int selectedIndex = -2;
                for (int i = this.layoutList.Children.Count - 1; i >= 0; i--)
                {
                    LayoutSelectionRow row = this.layoutList.Children[i] as LayoutSelectionRow;
                    if (row.isSelected())
                    {
                        selectedIndex = i;
                        row.setSelected(false);
                    }
                    if (i == selectedIndex - 1)
                    {
                        row.setSelected(true);
                    }
                }
                if (selectedIndex == 0)
                {
                    LayoutSelectionRow row = this.layoutList.Children[this.layoutList.Children.Count - 1] as LayoutSelectionRow;
                    row.setSelected(true);
                }
                else if (selectedIndex == -2)//If no row was found selected
                {
                    LayoutSelectionRow row = this.layoutList.Children[this.layoutList.Children.Count - 1] as LayoutSelectionRow;
                    row.setSelected(true);
                }
            }));
        }

        private void selectHighlighted()
        {
            Dispatcher.BeginInvoke(new Action(delegate()
            {
                for (int i = this.layoutList.Children.Count - 1; i >= 0; i--)
                {
                    LayoutSelectionRow row = this.layoutList.Children[i] as LayoutSelectionRow;
                    if (row.isSelected())
                    {
                        this.Select_Layout(row.getFilename());
                    }
                }
            }));
        }

        private void Select_Layout(string filename)
        {
            this.keyMapper.SetFallbackKeymap(filename);
            this.HideOverlay();
        }

        void OverlayWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if(!hidden)
            {
                /*if(e.Key == Key.Down)
                {
                    highlightNext();
                }
                else if(e.Key == Key.Up)
                {
                    highlightPrev();
                }
                else if(e.Key == Key.Right || e.Key == Key.Enter)
                {
                    selectHighlighted();
                }
                else */if (e.Key == Key.Escape)
                {
                    HideOverlay();
                }
            }
        }

        public bool OverlayIsOn()
        {
            return !this.hidden;
        }

        public void HideOverlay()
        {
            if (!this.hidden)
            {
                this.hidden = true;
                this.keyMapper.OnButtonDown -= keyMapper_OnButtonDown;
                this.keyMapper.OnButtonUp -= keyMapper_OnButtonUp;
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

                this.KeyUp += OverlayWindow_KeyUp;
            }
        }

        private void layoutChooserOverlay_MouseUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }
    }
}
