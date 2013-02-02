using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WiiTUIO.WinTouch;

namespace WiiTUIO.Output
{
    class OutputFactory
    {

        public enum OutputType
        {
            TOUCH,
            TUIO,
            DRAW
        }

        public static string getType(OutputType type)
        {
            switch (type)
            {
                case OutputType.TOUCH:
                    return "touch";
                case OutputType.TUIO:
                    return "tuio";
                case OutputType.DRAW:
                    return "draw";
                default:
                    return "touch";
            }
        }

        public static OutputType getType(string name)
        {
            if (name == "touch")
            {
                return OutputType.TOUCH;
            }
            else if (name == "tuio")
            {
                return OutputType.TUIO;
            }
            else if (name == "draw")
            {
                return OutputType.DRAW;
            }
            return OutputType.TOUCH; //Default to touch
        }

        public static IProviderHandler createProviderHandler(string name)
        {
            return createProviderHandler(getType(name));
        }

        public static IProviderHandler createProviderHandler(OutputType type)
        {
            switch (type)
            {
                case OutputType.TOUCH:
                    return new ProviderHandler();
                case OutputType.TUIO:
                    return new TUIOProviderHandler();
                case OutputType.DRAW:
                    return new DrawingProviderHandler();
                default:
                    return null;
            }
        }
    }
}
