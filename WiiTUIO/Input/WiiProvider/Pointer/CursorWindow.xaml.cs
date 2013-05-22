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

   [DllImport( "user32.dll" )]
   public static extern IntPtr GetWindow( IntPtr hWnd, GetWindowCmd uCmd );


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

   [DllImport( "user32.dll" )]
   public static extern int SetWindowPos( IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, SetWindowPosFlags uFlags );

   public const int WS_EX_TRANSPARENT = 0x00000020;
   public const int GWL_EXSTYLE = (-20);

   [DllImport("user32.dll")]
   public static extern int GetWindowLong(IntPtr hwnd, int index);

   [DllImport("user32.dll")]
   public static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

   public static void makeNormal(IntPtr hwnd)
   {
       int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
       SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle & ~WS_EX_TRANSPARENT);
   } 

   protected override void OnActivated(EventArgs e)
   {
      IntPtr hWnd = new WindowInteropHelper( this ).Handle;
 
      IntPtr hWndHiddenOwner = GetWindow( hWnd, GetWindowCmd.GW_OWNER );

      if ( hWndHiddenOwner != IntPtr.Zero )
      {
         IntPtr HWND_TOPMOST = new IntPtr(-1);
         SetWindowPos( hWndHiddenOwner, HWND_TOPMOST, 0, 0, 0, 0,
            SetWindowPosFlags.SWP_NOMOVE |
            SetWindowPosFlags.SWP_NOSIZE |
            SetWindowPosFlags.SWP_NOACTIVATE );
      }

      int extendedStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
      SetWindowLong(hWnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);

      int extendedStyle2 = GetWindowLong(hWndHiddenOwner, GWL_EXSTYLE);
      SetWindowLong(hWndHiddenOwner, GWL_EXSTYLE, extendedStyle2 | WS_EX_TRANSPARENT);

      this.Owner = OverlayWindow.Current;
   }



    }
}
