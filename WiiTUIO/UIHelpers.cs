using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using WiiTUIO.Properties;

namespace WiiTUIO
{
    public static class UIHelpers
    {
        public static void animateExpand(FrameworkElement elem)
        {
            if (elem.ActualHeight < 20)
            {
                elem.Height = double.NaN; //auto height
                elem.Visibility = Visibility.Visible;
                elem.Measure(new Size(2000,2000));
                double height = (elem.DesiredSize.Height > 0) ? elem.DesiredSize.Height : elem.ActualHeight;
                DoubleAnimation pAnimation = createDoubleAnimation(height, 1000, false);
                elem.Height = 0;
                elem.Visibility = Visibility.Visible;
                pAnimation.FillBehavior = FillBehavior.Stop;
                pAnimation.Completed += delegate(object sender, EventArgs pEvent)
                {
                    elem.Height = Double.NaN;
                    //elem.BeginAnimation(FrameworkElement., null);
                };
                //pAnimation.Freeze();
                elem.BeginAnimation(FrameworkElement.HeightProperty, pAnimation, HandoffBehavior.SnapshotAndReplace);
            }
        }

        public static void animateCollapse(FrameworkElement elem, bool remove)
        {
            if (elem.DesiredSize.Height > 0)
            {
                elem.Height = elem.DesiredSize.Height;
                DoubleAnimation pAnimation = createDoubleAnimation(0, 1000, false);
                pAnimation.FillBehavior = FillBehavior.Stop;
                pAnimation.Completed += delegate(object sender, EventArgs pEvent)
                {
                    //elem.BeginAnimation(FrameworkElement.HeightProperty, null);
                    if (remove && elem.Parent is Panel)
                    {
                        ((Panel)elem.Parent).Children.Remove(elem);
                    }
                    else
                    {
                        elem.Visibility = Visibility.Collapsed;
                        elem.Height = Double.NaN;
                    }
                };
                //pAnimation.Freeze();
                elem.BeginAnimation(FrameworkElement.HeightProperty, pAnimation, HandoffBehavior.SnapshotAndReplace);
            }
        }

        /**
         * @brief Helper method to create a double animation.
         * @param fNew The new value we want to move too.
         * @param fTime The time we want to allow in ms.
         * @param bFreeze Do we want to freeze this animation (so we can't modify it).
         */
        public static DoubleAnimation createDoubleAnimation(double fNew, double fTime, bool bFreeze)
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

        public static void TopmostFix(Window window)
        {
            IntPtr HWND_TOPMOST = new IntPtr(-1);
            IntPtr HWND_NOTOPMOST = new IntPtr(-2);

            IntPtr zorder = Settings.Default.noTopmost ? HWND_NOTOPMOST : HWND_TOPMOST;

            IntPtr hWnd = new WindowInteropHelper(window).Handle;

            if (hWnd != IntPtr.Zero)
            {
                SetWindowPos(hWnd, zorder, 0, 0, 0, 0,
                       SetWindowPosFlags.SWP_NOMOVE |
                       SetWindowPosFlags.SWP_NOSIZE |
                       SetWindowPosFlags.SWP_NOACTIVATE);

                IntPtr hWndHiddenOwner = GetWindow(hWnd, GetWindowCmd.GW_OWNER);

                if (hWndHiddenOwner != IntPtr.Zero)
                {
                    SetWindowPos(hWndHiddenOwner, zorder, 0, 0, 0, 0,
                       SetWindowPosFlags.SWP_NOMOVE |
                       SetWindowPosFlags.SWP_NOSIZE |
                       SetWindowPosFlags.SWP_NOACTIVATE);
                }
            }
        }

        private static int WS_EX_TRANSPARENT = 0x00000020;
        private static int GWL_EXSTYLE = (-20);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        private static void makeNormal(IntPtr hwnd)
        {
            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle & ~WS_EX_TRANSPARENT);
        }

        private static void makeExTransparent(IntPtr hwnd)
        {
            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle| WS_EX_TRANSPARENT);
        }

        public static void MakeWindowUnclickable(Window window)
        {
            IntPtr hWnd = new WindowInteropHelper(window).Handle;

            IntPtr hWndHiddenOwner = GetWindow(hWnd, GetWindowCmd.GW_OWNER);

            makeExTransparent(hWnd);
            makeExTransparent(hWndHiddenOwner);
        }
    }
}
