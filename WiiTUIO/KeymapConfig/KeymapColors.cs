using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace WiiTUIO
{
    public class KeymapColors
    {

        public static Color GetColor(KeymapOutputType type)
        {
            switch (type)
            {
                case KeymapOutputType.KEYBOARD:
                    return Colors.Orange;
                case KeymapOutputType.TOUCH:
                    return Colors.Red;
                case KeymapOutputType.MOUSE:
                    return Colors.Orange;
                case KeymapOutputType.XINPUT:
                    return Colors.Green;
                default:
                    return Colors.Black;
            }
        }

    }
}
