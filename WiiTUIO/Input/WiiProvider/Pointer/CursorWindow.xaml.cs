using Microsoft.Win32;
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

namespace WiiTUIO.Provider
{
    /// <summary>
    /// Interaction logic for Cursor.xaml
    /// </summary>
    public partial class CursorWindow : Window
    {

        private static CursorWindow defaultInstance;

        public static CursorWindow Current
        {
            get
            {
                if (defaultInstance == null)
                {
                    defaultInstance = new CursorWindow();
                }
                return defaultInstance;
            }
        }

        private CursorWindow()
        {
            InitializeComponent();
            this.Width = Util.ScreenBounds.Width;
            this.Height = Util.ScreenBounds.Height;

            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;

            //Compensate for DPI settings
            Loaded += (o, e) =>
            {
                PresentationSource source = PresentationSource.FromVisual(this);
                CompositionTarget ct = source.CompositionTarget;
                Matrix transformMatrix = ct.TransformFromDevice;
                this.cursorCanvas.RenderTransform = new MatrixTransform(transformMatrix);
            };
            
        }

        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            this.Width = Util.ScreenBounds.Width;
            this.Height = Util.ScreenBounds.Height;
        }

        public void addCursor(Cursor cursor)
        {
            this.cursorCanvas.Children.Add(cursor);
        }

        public void removeCursor(Cursor cursor)
        {
            this.cursorCanvas.Children.Remove(cursor);
        }

        protected override void OnActivated(EventArgs e)
        {
            UIHelpers.TopmostFix(this);
            UIHelpers.MakeWindowUnclickable(this);
            this.Owner = OverlayWindow.Current;
        }

    }
}
