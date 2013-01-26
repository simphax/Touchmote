using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace WiiTUIO.Provider
{
    public interface IProvider 
    {

        event Action<int> OnConnect;
        event Action<int> OnDisconnect;
        event Action<int> OnBatteryUpdate;
        event EventHandler<FrameEventArgs> OnNewFrame;

        void start();
        void stop();

        void showSettingsWindow();

    }
}
