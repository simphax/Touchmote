using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMultiDllWrapper;

namespace WiiTUIO.Output
{
    class VmultiDevice : VMulti
    {

        private static VmultiDevice currentInstance;

        public static VmultiDevice Current
        {
            get
            {
                if (currentInstance == null)
                {
                    currentInstance = new VmultiDevice();
                    if(!currentInstance.connect())
                    {
                        Console.WriteLine("Could not connect to the Vmulti device");
                    }
                }
                return currentInstance;
            }
        }

        public bool isAvailable()
        {
            return this.isConnected();
        }

    }
}
