using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace WiiTUIO.Provider
{
    public interface IProvider 
    {

        event Action<int> OnConnect;
        event Action<int> OnDisconnect;
        event Action<int> OnBatteryUpdate;
        event Action<int> OnButtonDown;
        event Action<int> OnButtonUp;
        event EventHandler<FrameEventArgs> OnNewFrame;

        void start();
        void stop();

        UserControl getSettingsControl();

    }
}
