using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiiTUIO.Provider
{
    public class XinputReport
    {
        public double StickLX;
        public double StickLY;

        public double TriggerL;
        public double TriggerR;

        public Byte[] ToBytes() {

            Byte[] result = new Byte[6];

            result[0] = 1;
            result[1] = 0;

            result[2] = getTriggerLRaw();
            result[3] = getTriggerRRaw();

            result[4] = getStickLXRaw();
            result[5] = getStickLYRaw();

            return result;
        }


        public Byte getStickLXRaw()
        {
            if (StickLX > 1.0)
            {
                return 255;
            }
            if (StickLX < -1.0)
            {
                return 1;
            }
            return (Byte)((StickLX + 1) * 0.5 * 255);
        }

        public Byte getStickLYRaw()
        {
            if (StickLY > 1.0)
            {
                return 255;
            }
            if (StickLY < -1.0)
            {
                return 1;
            }
            return (Byte)((StickLY + 1) * 0.5 * 255);
        }

        public Byte getTriggerLRaw()
        {
            if (TriggerL > 1.0)
            {
                return 255;
            }
            if (TriggerR < 0.0)
            {
                return 1;
            }
            return (Byte)(TriggerL * 255);
        }

        public Byte getTriggerRRaw()
        {
            if (TriggerR > 1.0)
            {
                return 255;
            }
            if (TriggerR < 0.0)
            {
                return 1;
            }
            return (Byte)(TriggerR * 255);
        }
    }
}
