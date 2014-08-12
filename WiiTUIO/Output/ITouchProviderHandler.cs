using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WiiTUIO.Provider;

namespace WiiTUIO.Output
{
    interface ITouchProviderHandler
    {

        event Action OnConnect;
        event Action OnDisconnect;

        void connect();

        void processEventFrame();

        void disconnect();

        void queueContact(WiiContact contact);
    }
}
