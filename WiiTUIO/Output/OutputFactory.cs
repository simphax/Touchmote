using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WiiTUIO.Properties;
using WiiTUIO.WinTouch;

namespace WiiTUIO.Output
{
    class OutputFactory
    {

        private static IProviderHandler current;

        public static IProviderHandler getCurrentProviderHandler()
        {
            if (current == null)
            {
                current = createProviderHandler(Settings.Default.output);
            }
            return current;
        }

        public enum OutputType
        {
            TOUCH,
            TOUCHMTV,
            TOUCHVMULTI,
            TUIO,
            TUIOTOUCH,
            DRAW
        }

        private static string getType(OutputType type)
        {
            switch (type)
            {
                case OutputType.TOUCH:
                    return "touch";
                case OutputType.TOUCHMTV:
                    return "touch-mtv";
                case OutputType.TOUCHVMULTI:
                    return "touch-vmulti";
                case OutputType.TUIO:
                    return "tuio";
                case OutputType.TUIOTOUCH:
                    return "tuio-touch";
                case OutputType.DRAW:
                    return "draw";
                default:
                    return "touch";
            }
        }

        private static OutputType getType(string name)
        {
            if (name == "touch")
            {
                return OutputType.TOUCH;
            }
            if (name == "touch-mtv")
            {
                return OutputType.TOUCHMTV;
            }
            if (name == "touch-vmulti")
            {
                return OutputType.TOUCHVMULTI;
            }
            else if (name == "tuio")
            {
                return OutputType.TUIO;
            }
            else if (name == "tuio-touch")
            {
                return OutputType.TUIOTOUCH;
            }
            else if (name == "draw")
            {
                return OutputType.DRAW;
            }
            return OutputType.TOUCH; //Default to touch
        }

        private static IProviderHandler createProviderHandler(string name)
        {
            return createProviderHandler(getType(name));
        }

        private static IProviderHandler createProviderHandler(OutputType type)
        {
            switch (type)
            {
                case OutputType.TOUCH:
                    return new TouchInjectProviderHandler();
                case OutputType.TOUCHMTV:
                    return new MTVProviderHandler();
                case OutputType.TOUCHVMULTI:
                    return new VmultiProviderHandler();
                case OutputType.TUIO:
                    return new TUIOProviderHandler();
                case OutputType.TUIOTOUCH:
                    return new TUIOVmultiProviderHandler();
                case OutputType.DRAW:
                    return new DrawingProviderHandler();
                default:
                    return null;
            }
        }
    }
}
