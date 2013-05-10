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

        event Action<int, int> OnConnect;//Wiimote ID, Total Wiimotes
        event Action<int, int> OnDisconnect;//Wiimote ID, Total Wiimotes
        event Action<WiimoteStatus> OnStatusUpdate;
        event EventHandler<FrameEventArgs> OnNewFrame;

        void start();
        void stop();

        //static UserControl getSettingsControl();

    }
}
