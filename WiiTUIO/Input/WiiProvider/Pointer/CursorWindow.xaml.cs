using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
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
using WiiTUIO.Properties;

namespace WiiTUIO.Provider
{
    /// <summary>
    /// Interaction logic for Cursor.xaml
    /// </summary>
    public partial class CursorWindow : Window
    {
        private CursorCanvas cursorCanvas;
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

            this.cursorCanvas = new CursorCanvas();
            this.baseCanvas.Children.Add(cursorCanvas);

            this.Width = Util.ScreenBounds.Width;
            this.Height = Util.ScreenBounds.Height;
            this.baseCanvas.Width = Util.ScreenBounds.Width;
            this.baseCanvas.Height = Util.ScreenBounds.Height;
            this.cursorCanvas.Width = Util.ScreenBounds.Width;
            this.cursorCanvas.Height = Util.ScreenBounds.Height;

            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;

            /*
            //Compensate for DPI settings
            Loaded += (o, e) =>
            {
                PresentationSource source = PresentationSource.FromVisual(this);
                CompositionTarget ct = source.CompositionTarget;
                Matrix transformMatrix = ct.TransformFromDevice;
                this.cursorCanvas.RenderTransform = new MatrixTransform(transformMatrix);
            };
            */
            Console.WriteLine("Render capability Tier: " + (RenderCapability.Tier >> 16));

        }

        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            this.Width = Util.ScreenBounds.Width;
            this.Height = Util.ScreenBounds.Height;
            this.cursorCanvas.Width = Util.ScreenBounds.Width;
            this.cursorCanvas.Height = Util.ScreenBounds.Height;
        }

        public void addCursor(Cursor2 cursor)
        {
            this.cursorCanvas.AddCursor(cursor);
        }

        public void removeCursor(Cursor2 cursor)
        {
            this.cursorCanvas.RemoveCursor(cursor);
        }

        protected override void OnActivated(EventArgs e)
        {
            UIHelpers.TopmostFix(this);
            UIHelpers.MakeWindowUnclickable(this);
            this.Owner = OverlayWindow.Current;
        }

    }
}
