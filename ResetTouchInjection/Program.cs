using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResetTouchInjection
{
    class Program
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern Boolean InitializeTouchInjection(UInt32 maxCount, uint dwMode);

        static void Main(string[] args)
        {
            InitializeTouchInjection(256, 1);
        }
    }
}
