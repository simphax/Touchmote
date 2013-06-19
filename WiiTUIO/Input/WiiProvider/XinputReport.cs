using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiiTUIO.Provider
{
    public class XinputReport
    {
        public double StickLX = 0.5;
        public double StickLY = 0.5;
        public double StickRX = 0.5;
        public double StickRY = 0.5;

        public double TriggerL;
        public double TriggerR;

        public bool A;
        public bool B;
        public bool X;
        public bool Y;

        public bool Back;
        public bool StickPressL;
        public bool StickPressR;
        public bool Start;

        public bool Up;
        public bool Down;
        public bool Right;
        public bool Left;

        public bool BumperL;
        public bool BumperR;

        public bool Guide;

        private int ID;

        public XinputReport(int ID)
        {
            this.ID = ID;
        }

        public Byte[] ToBytes() {

            Byte[] input = new Byte[28];

            input[0] = (Byte)this.ID;
            input[1] = 0x02;

            input[10] = 0;
            input[11] = 0;
            input[12] = 0;
            input[13] = 0;

            input[10] |= (Byte)(this.Back ? 1 << 0 : 0);
            input[10] |= (Byte)(this.StickPressL ? 1 << 1 : 0);
            input[10] |= (Byte)(this.StickPressR ? 1 << 2 : 0);
            input[10] |= (Byte)(this.Start ? 1 << 3 : 0);

            input[10] |= (Byte)(this.Up ? 1 << 4 : 0);
            input[10] |= (Byte)(this.Down ? 1 << 5 : 0);
            input[10] |= (Byte)(this.Right ? 1 << 6 : 0);
            input[10] |= (Byte)(this.Left ? 1 << 7 : 0);

            input[11] |= (Byte)(this.BumperL ? 1 << 2 : 0);
            input[11] |= (Byte)(this.BumperR ? 1 << 3 : 0);

            input[11] |= (Byte)(this.Y ? 1 << 4 : 0);
            input[11] |= (Byte)(this.B ? 1 << 5 : 0);

            input[11] |= (Byte)(this.A ? 1 << 6 : 0);
            input[11] |= (Byte)(this.X ? 1 << 7 : 0);

            input[12] |= (Byte)(this.Guide ? 1 << 0 : 0);

            input[26] = getTriggerLRaw();
            input[27] = getTriggerRRaw();

            Int32 ThumbLX = getStickLXRaw();
            Int32 ThumbLY = getStickLYRaw();
            Int32 ThumbRX = getStickRXRaw();
            Int32 ThumbRY = getStickRYRaw();

            input[14] = (Byte)((ThumbLX >> 0) & 0xFF); // LX
            input[15] = (Byte)((ThumbLX >> 8) & 0xFF);

            input[16] = (Byte)((ThumbLY >> 0) & 0xFF); // LY
            input[17] = (Byte)((ThumbLY >> 8) & 0xFF);

            input[18] = (Byte)((ThumbRX >> 0) & 0xFF); // RX
            input[19] = (Byte)((ThumbRX >> 8) & 0xFF);

            input[20] = (Byte)((ThumbRY >> 0) & 0xFF); // RY
            input[21] = (Byte)((ThumbRY >> 8) & 0xFF);

            return input;
        }


        public Int32 getStickLXRaw()
        {
            if (StickLX > 1.0)
            {
                return 32767;
            }
            if (StickLX < 0.0)
            {
                return -32767;
            }
            return (Int32)((StickLX-0.5) * 2 * 32767);
        }

        public Int32 getStickLYRaw()
        {
            if (StickLY > 1.0)
            {
                return 32767;
            }
            if (StickLY < 0.0)
            {
                return -32767;
            }
            return (Int32)((StickLY - 0.5) * 2 * 32767);
        }

        public Int32 getStickRXRaw()
        {
            if (StickRX > 1.0)
            {
                return 32767;
            }
            if (StickRX < 0.0)
            {
                return -32767;
            }
            return (Int32)((StickRX - 0.5) * 2 * 32767);
        }

        public Int32 getStickRYRaw()
        {
            if (StickRY > 1.0)
            {
                return 32767;
            }
            if (StickRY < 0.0)
            {
                return -32767;
            }
            return (Int32)((StickRY - 0.5) * 2 * 32767);
        }

        public Byte getTriggerLRaw()
        {
            if (TriggerL > 1.0)
            {
                return 255;
            }
            if (TriggerR < 0.0)
            {
                return 0;
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
                return 0;
            }
            return (Byte)(TriggerR * 255);
        }
    }
}
