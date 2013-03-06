using System.Windows.Forms;
using System;
using System.Drawing;

namespace WiiTUIO.Provider
{
    public static class Util
    {

        public static Rectangle ScreenBounds
        {
            get
            {
                return Screen.PrimaryScreen.Bounds;
            }
        }

        public static int ScreenWidth
        {
            get {
                return Screen.PrimaryScreen.Bounds.Width;
            }
        }

        public static int ScreenHeight
        {
            get
            {
                return Screen.PrimaryScreen.Bounds.Height;
            }
        }

    }
}
