using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace WiiTUIO.Provider
{
    class CursorColor
    {
        public static Color getColor(int id)
        {
            switch (id)
            {
                case 1:
                    return Color.FromRgb(128,255,0);
                case 2:
                    return Color.FromRgb(197, 0, 255);
                case 3:
                    return Color.FromRgb(0, 220, 255);
                case 4:
                    return Color.FromRgb(255, 255, 0);
                default:
                    return randomColor();
            }

        }

        public static Color randomColor()
        {
            Random rand = new Random();
            byte red = (byte)rand.Next(255);
            byte green = (byte)rand.Next(255);
            byte blue = (byte)rand.Next(255);

            int kill = rand.Next(2);
            switch (kill)
            {
                case 0:
                    red = 0;
                    break;
                case 1:
                    green = 0;
                    break;
                case 2:
                    blue = 0;
                    break;
            }

            return Color.FromRgb(red,green,blue);
        }
    }
}
