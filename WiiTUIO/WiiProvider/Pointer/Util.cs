using System.Windows.Forms;
using System;

namespace WiiTUIO.Provider
{
    public static class Util
    {

        public static int ScreenWidth
        {
            get { return Screen.PrimaryScreen.Bounds.Width; }
        }

        public static int ScreenHeight
        {
            get { return Screen.PrimaryScreen.Bounds.Height; }
        }

    }
}
