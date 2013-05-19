using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiiTUIO.Provider
{
    public class XinputReport
    {
        public double LStickX;
        public double LStickY;

        public Byte[] ToBytes() {

            Byte[] result = new Byte[6];

            result[0] = 1;
            result[1] = 0;

            result[4] = getLStickXRaw();
            result[5] = getLStickYRaw();

            return result;
        }


        public Byte getLStickXRaw()
        {
            if (LStickX > 1.0)
            {
                return 255;
            }
            if (LStickX < -1.0)
            {
                return 1;
            }
            return (Byte)((LStickX + 1) * 0.5 * 255);
        }

        public Byte getLStickYRaw()
        {
            if (LStickY > 1.0)
            {
                return 255;
            }
            if (LStickY < -1.0)
            {
                return 1;
            }
            return (Byte)((LStickY + 1) * 0.5 * 255);
        }
    }
}
