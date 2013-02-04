using System.Windows.Forms;
using System;

namespace WiiTUIO.Provider
{
    public static class Util
    {

        private static int savedScreenWidth = 0;

        private static int savedScreenHeight = 0;

        public static int ScreenWidth
        {
            get {
                if (savedScreenWidth > 0)
                {
                    return savedScreenWidth;
                }
                else
                {
                    savedScreenWidth = Screen.PrimaryScreen.Bounds.Width;
                    return savedScreenWidth;
                }
            }
        }

        public static int ScreenHeight
        {
            get
            {
                if (savedScreenHeight > 0)
                {
                    return savedScreenHeight;
                }
                else
                {
                    savedScreenHeight = Screen.PrimaryScreen.Bounds.Height;
                    return savedScreenHeight;
                }
            }
        }

    }
}
