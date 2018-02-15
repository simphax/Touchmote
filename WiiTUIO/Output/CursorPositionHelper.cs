using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WiiTUIO.Properties;
using WiiTUIO.Provider;
using WiiTUIO.Filters;
using Microsoft.Win32;

namespace WiiTUIO.Output
{
    /// <summary>
    /// This helper class transforms absolute position of Wii pointer to relative position. 
    /// x=0.5, y=0.5 means center of the screen.
    /// </summary>
    class CursorPositionHelper
    {
        private System.Drawing.Rectangle screenBounds;
        
        public CursorPositionHelper()
        {
            screenBounds = DeviceUtils.DeviceUtil.GetScreen(Settings.Default.primaryMonitor).Bounds;

            Settings.Default.PropertyChanged += SettingsChanged;
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;

        }

        private void SettingsChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "primaryMonitor")
            {
                screenBounds = DeviceUtils.DeviceUtil.GetScreen(Settings.Default.primaryMonitor).Bounds;
            }
        }

        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            screenBounds = DeviceUtils.DeviceUtil.GetScreen(Settings.Default.primaryMonitor).Bounds;
        }

        public Point getSmoothedPosition(Point relativePosition)
        {
            Vector vec = new Vector(relativePosition.X, relativePosition.Y);
            return new Point(vec.X, vec.Y);
        }   

        public Point getRelativePosition(Point absPosition)
        {
            Vector vec = new Vector(absPosition.X, absPosition.Y);
            return new Point(vec.X / screenBounds.Width, vec.Y / screenBounds.Height);
        }        
    }
}
