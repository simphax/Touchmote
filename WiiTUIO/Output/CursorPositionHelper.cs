using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WiiTUIO.Properties;
using WiiTUIO.Provider;
using Microsoft.Win32;

namespace WiiTUIO.Output
{
    /// <summary>
    /// This helper class transforms absolute position of Wii pointer to relative position. 
    /// x=0.5, y=0.5 means center of the screen.
    /// </summary>
    static class CursorPositionHelper
    {
        private static SmoothingBuffer smoothingBuffer;
        private static System.Drawing.Rectangle screenBounds;
        
        static CursorPositionHelper()
        {
            smoothingBuffer = new SmoothingBuffer(Settings.Default.pointer_positionSmoothing);
            screenBounds = DeviceUtils.DeviceUtil.GetScreen(Settings.Default.primaryMonitor).Bounds;

            Settings.Default.PropertyChanged += SettingsChanged;
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;

        }

        private static void SettingsChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "primaryMonitor")
            {
                screenBounds = DeviceUtils.DeviceUtil.GetScreen(Settings.Default.primaryMonitor).Bounds;
            }
        }

        private static void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            screenBounds = DeviceUtils.DeviceUtil.GetScreen(Settings.Default.primaryMonitor).Bounds;
        }

        public static Point getRelativePosition(Point absPosition)
        {
            smoothingBuffer.addValue(new System.Windows.Vector(absPosition.X, absPosition.Y));
            System.Windows.Vector smoothedVec = smoothingBuffer.getSmoothedValue();
            return new Point(smoothedVec.X / screenBounds.Width, smoothedVec.Y / screenBounds.Height);
        }        
    }
}
