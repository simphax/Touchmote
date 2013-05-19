using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiiTUIO.Provider
{
    public class XinputDevice
    {

        private int ID;
        private XinputBus bus;

        public XinputDevice(XinputBus bus, int ID)
        {
            this.bus = bus;
            this.ID = ID;
            bus.Plugin(ID);
        }

        public bool Update(XinputReport reportobj)
        {
            Byte[] input = reportobj.ToBytes();
            Byte[] rumble = new Byte[8];
            Byte[] report = new Byte[28];

            bus.Parse(input, report);

            return bus.Report(report, rumble);
        }
    }
}
