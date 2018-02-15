//Copyright David Anson
//http://blogs.msdn.com/b/delay/archive/2009/08/31/get-out-of-the-way-with-the-tray-minimize-to-tray-sample-implementation-for-wpf.aspx

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
/// <summary>
/// Class implementing support for "minimize to tray" functionality.
/// </summary>
using System.Windows;
using System.Windows.Forms;
using WiiTUIO;
public static class MinimizeToTray
{
    /// <summary>
    /// Enables "minimize to tray" behavior for the specified Window.
    /// </summary>
    /// <param name="window">Window to enable the behavior for.</param>
    public static void Enable(Window window, bool minimizeNow)
    {
        UIHelpers.HideFromAltTab(window);
        if (MinimizeInstances.ContainsKey(window))
        {
            Console.WriteLine(string.Format("Minimization already enabled for '{0}'", window.Title));
            if (minimizeNow)
            {
                MinimizeInstances[window].MinimizeNow();
            }
        }
        else
        {
            var instance = new MinimizeToTrayInstance(window);
            instance.Enable();
            MinimizeInstances.Add(window, instance);

            if (minimizeNow)
            {
                instance.MinimizeNow();
            }
        }
    }

    public static void Disable(Window window)
    {
        UIHelpers.RevertHideFromAltTab(window);
        if (!MinimizeInstances.ContainsKey(window))
        {
            Console.WriteLine(string.Format("Minimization not enabled for '{0}'", window.Title));
        }
        else
        {
            var instance = MinimizeInstances[window];
            instance.Disable();
            MinimizeInstances.Remove(window);
        }
    }

    private static Dictionary<Window, MinimizeToTrayInstance> _minimizeInstances;
    /// <summary>
    /// Gets or sets the windows for which tray minimization is currently enabled.
    /// </summary>
    /// <value>The windows for which tray minimization is currently enabled.</value>
    private static Dictionary<Window, MinimizeToTrayInstance> MinimizeInstances
    {
        get
        {
            if (_minimizeInstances == null)
            {
                _minimizeInstances = new Dictionary<Window, MinimizeToTrayInstance>();
            }
            return _minimizeInstances;
        }
        set { _minimizeInstances = value; }
    }

    /// <summary>
    /// Class implementing "minimize to tray" functionality for a Window instance.
    /// </summary>
    private class MinimizeToTrayInstance
    {
        private Window _window;
        private NotifyIcon _notifyIcon;
        private bool _balloonShown;

        /// <summary>
        /// Enables minimization for this Window.
        /// </summary>
        public void Enable()
        {
            _window.StateChanged += new EventHandler(HandleStateChanged);
        }

        /// <summary>
        /// Disables minimization for this Window.
        /// </summary>
        public void Disable()
        {
            _window.StateChanged -= new EventHandler(HandleStateChanged);
        }

        /// <summary>
        /// Initializes a new instance of the MinimizeToTrayInstance class.
        /// </summary>
        /// <param name="window">Window instance to attach to.</param>
        public MinimizeToTrayInstance(Window window)
        {
            Debug.Assert(window != null, "window parameter is null.");
            _window = window;
            //_window.StateChanged += new EventHandler(HandleStateChanged);
        }

        public void MinimizeNow()
        {
            App.Current.Dispatcher.BeginInvoke(new Action(delegate()
            {
                _window.WindowState = WindowState.Minimized;
                this.HandleStateChanged(null, null);

            }),null);
        }

        /// <summary>
        /// Handles the Window's StateChanged event.
        /// </summary>
        /// <param name="sender">Event source.</param>
        /// <param name="e">Event arguments.</param>
        private void HandleStateChanged(object sender, EventArgs e)
        {
            if (_notifyIcon == null)
            {
                // Initialize NotifyIcon instance "on demand"
                _notifyIcon = new NotifyIcon();
                _notifyIcon.Icon = Icon.ExtractAssociatedIcon(Assembly.GetEntryAssembly().Location);
                _notifyIcon.MouseClick += new MouseEventHandler(HandleNotifyIconOrBalloonClicked);
                _notifyIcon.BalloonTipClicked += new EventHandler(HandleNotifyIconOrBalloonClicked);
            }
            // Update copy of Window Title in case it has changed
            _notifyIcon.Text = _window.Title;

            // Show/hide Window and NotifyIcon
            var minimized = (_window.WindowState == WindowState.Minimized);
            _window.ShowInTaskbar = !minimized;
            _notifyIcon.Visible = minimized;
            if (minimized && !_balloonShown)
            {
                // If this is the first time minimizing to the tray, show the user what happened
                _notifyIcon.ShowBalloonTip(1000, null, _window.Title, ToolTipIcon.None);
                _balloonShown = true;
            }
        }

        /// <summary>
        /// Handles a click on the notify icon or its balloon.
        /// </summary>
        /// <param name="sender">Event source.</param>
        /// <param name="e">Event arguments.</param>
        private void HandleNotifyIconOrBalloonClicked(object sender, EventArgs e)
        {
            // Restore the Window
            _window.WindowState = WindowState.Normal;
            _window.Width = 419; //Hack to fix layout problems with minimizing on start.
            _window.Activate();
        }
    }
}

