using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WiiTUIO.Provider;

namespace WiiTUIO.Output
{
    interface IProviderHandler
    {

        event Action OnConnect;
        event Action OnDisconnect;

        void connect();

        void processEventFrame(FrameEventArgs e);

        void disconnect();

        void showSettingsWindow();
    }
}
