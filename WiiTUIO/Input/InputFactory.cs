using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WiiTUIO.Provider;

namespace WiiTUIO.Input
{
    class InputFactory
    {

        public enum InputType
        {
            MULTIPOINTER,
            POINTER,
            PEN
        }

        public static string getType(InputType type)
        {
            switch (type)
            {
                case InputType.MULTIPOINTER:
                    return "multipointer";
                case InputType.POINTER:
                    return "pointer";
                case InputType.PEN:
                    return "pen";
                default:
                    return "pointer";
            }
        }

        public static InputType getType(string name)
        {
            if (name == "pointer")
            {
                return InputType.POINTER;
            } 
            else if (name == "multipointer")
            {
                return InputType.MULTIPOINTER;
            }
            else if (name == "pen")
            {
                return InputType.PEN;
            }
            return InputType.POINTER; //Default to pointer
        }

        public static IProvider createInputProvider(string name)
        {
            return createInputProvider(getType(name));
        }

        public static IProvider createInputProvider(InputType type)
        {
            switch (type)
            {
                case InputType.MULTIPOINTER:
                    return new MultiWiiPointerProvider();
                case InputType.POINTER:
                    return new WiiPointerProvider();
                case InputType.PEN:
                    return new WiiProvider();
                default:
                    return null;
            }
        }
    }
}
