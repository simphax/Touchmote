using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WiiTUIO
{
    class WiimotePairing
    {

        public Action WiimotePaired;
        public Action WiimotePairingStart;
        public Action WiimotePairingStop;
        public Action<string> WiimoteFound;
        public Action<string> WiimoteRemoved;

        public List<string> getPairedWiimotes()
        {
            return new List<string>();
        }

        public void start()
        {

            WiimotePairingStart();
        }

        public void stop()
        {

            WiimotePairingStop();
        }
    }
}
