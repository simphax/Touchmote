using ScpControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiiTUIO.Provider
{
    public partial class XinputBus : BusDevice
    {

        private static XinputBus defaultInstance;

        public static XinputBus Default
        {
            get
            {
                if (defaultInstance == null)
                {
                    defaultInstance = new XinputBus();
                    defaultInstance.Open();
                    defaultInstance.Start();
                }
                return defaultInstance;
            }
        }

        public XinputBus() 
        {
            App.Current.Dispatcher.BeginInvoke(new Action(delegate()
            {
                App.Current.Exit += OnAppExit;
            }), null);
        }

        private void OnAppExit(object sender, System.Windows.ExitEventArgs e)
        {
            this.Stop();
            this.Close();
        }

        public override Int32 Parse(Byte[] Input, Byte[] Output) 
        {
            for (Int32 Index = 0; Index < 28; Index++) Output[Index] = 0x00;

            Output[0] = 0x1C;
            Output[4] = Input[0];
            Output[9] = 0x14;

            Int32 Buttons = Input[1];

            if ((Buttons & (0x1 << 0)) > 0) Output[11] |= (Byte)(1 << 7); // Y
            if ((Buttons & (0x1 << 1)) > 0) Output[11] |= (Byte)(1 << 5); // B
            if ((Buttons & (0x1 << 2)) > 0) Output[11] |= (Byte)(1 << 4); // A
            if ((Buttons & (0x1 << 3)) > 0) Output[11] |= (Byte)(1 << 6); // X

            Output[12] = Input[2]; // Left Trigger
            Output[13] = Input[3]; // Right Trigger

            Int32 ThumbLX = Scale(Input[4], false);
            Int32 ThumbLY = -Scale(Input[5], false);

            Output[14] = (Byte)((ThumbLX >> 0) & 0xFF); // LX
            Output[15] = (Byte)((ThumbLX >> 8) & 0xFF);

            Output[16] = (Byte)((ThumbLY >> 0) & 0xFF); // LY
            Output[17] = (Byte)((ThumbLY >> 8) & 0xFF);

            return Input[0];
        }
    }
}
