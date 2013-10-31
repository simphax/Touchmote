using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiiTUIO.Output.Handlers.Xinput
{
    public class XinputDevice
    {

        private int ID;
        private XinputBus bus;

        public Action<Byte,Byte> OnRumble;

        public XinputDevice(XinputBus bus, int ID)
        {
            this.bus = bus;
            this.ID = ID;
            bus.Plugin(ID);
        }

        public void Remove()
        {
            bus.Unplug(ID);
        }

        public bool Update(XinputReport reportobj)
        {
            Byte[] input = reportobj.ToBytes();
            Byte[] rumble = new Byte[8];
            Byte[] report = new Byte[28];

            bus.Parse(input, report);

            if (bus.Report(report, rumble))
            {
                if (rumble[1] == 0x08)
                {
                    Byte big = (Byte)(rumble[3]);
                    Byte small = (Byte)(rumble[4]);

                    if (OnRumble != null)
                    {
                        OnRumble(big, small);
                    }
                }
                return true;
            }

            return false;
        }
    }
}
