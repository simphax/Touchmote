using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WiiTUIO.Provider
{
    public interface IProvider 
    {

        event Action<int> OnBatteryUpdate;
        event EventHandler<FrameEventArgs> OnNewFrame;

        void start();
        void stop();

    }
}
