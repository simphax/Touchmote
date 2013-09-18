using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiiTUIO.Properties;

namespace WiiTUIO
{
    class KeymapDatabase
    {
        private List<KeymapInput> allInputs;
        private List<KeymapOutput> allOutputs;

        private static KeymapDatabase currentInstance;
        public static KeymapDatabase Current
        {
            get
            {
                if (currentInstance == null)
                {
                    currentInstance = new KeymapDatabase();
                }
                return currentInstance;
            }
        }

        private KeymapDatabase()
        {
            allInputs = new List<KeymapInput>();
            allInputs.Add(new KeymapInput(InputSource.IR, "Pointer", "Pointer"));
            allInputs.Add(new KeymapInput(InputSource.WIIMOTE, "A", "A"));
            allInputs.Add(new KeymapInput(InputSource.WIIMOTE, "B", "B"));
            allInputs.Add(new KeymapInput(InputSource.WIIMOTE, "Home", "Home"));
            allInputs.Add(new KeymapInput(InputSource.WIIMOTE, "Left", "Left"));

            allOutputs = new List<KeymapOutput>();
            allOutputs.Add(new KeymapOutput(OutputType.TOUCH, "Touch Cursor", "touch", true));
            allOutputs.Add(new KeymapOutput(OutputType.TOUCH, "Touch Main", "touchmaster"));
            allOutputs.Add(new KeymapOutput(OutputType.TOUCH, "Touch Slave", "touchslave"));
            allOutputs.Add(new KeymapOutput(OutputType.KEYBOARD, "Left", "left"));
            allOutputs.Add(new KeymapOutput(OutputType.KEYBOARD, "Right", "right"));
            allOutputs.Add(new KeymapOutput(OutputType.KEYBOARD, "Up", "up"));
            allOutputs.Add(new KeymapOutput(OutputType.KEYBOARD, "Down", "down"));
            allOutputs.Add(new KeymapOutput(OutputType.KEYBOARD, "Volume Up", "volume_up"));
            allOutputs.Add(new KeymapOutput(OutputType.KEYBOARD, "Volume Down", "volume_down"));
            allOutputs.Add(new KeymapOutput(OutputType.KEYBOARD, "C", "vk_c"));
            allOutputs.Add(new KeymapOutput(OutputType.KEYBOARD, "Tab", "tab"));
            allOutputs.Add(new KeymapOutput(OutputType.KEYBOARD, "Left Win", "lwin"));
        }

        public List<Keymap> getAllKeymaps()
        {
            List<Keymap> list = new List<Keymap>();
            string[] files = Directory.GetFiles(Settings.Default.keymaps_path,"*.json");
            foreach (string filepath in files)
            {
                string filename = Path.GetFileName(filepath);
                if (filename != Settings.Default.keymaps_config)
                {
                    list.Add(new Keymap(filename));
                }
            }
            return list;
        }



        public List<KeymapInput> getAvailableInputs(InputSource source)
        {
            List<KeymapInput> list = new List<KeymapInput>();
            foreach (KeymapInput input in allInputs)
            {
                if (input.Source == source)
                {
                    list.Add(input);
                }
            }
            return list;
        }

        public List<KeymapOutput> getAvailableOutputs(OutputType type)
        {
            List<KeymapOutput> list = new List<KeymapOutput>();
            foreach (KeymapOutput output in allOutputs)
            {
                if (output.Type == type)
                {
                    list.Add(output);
                }
            }
            return list;
        }

        public KeymapInput getInput(string key)
        {
            List<KeymapInput> list = this.allInputs;
            foreach (KeymapInput input in list)
            {
                if (input.Key == key)
                {
                    return input;
                }
            }
            return null;
        }

        public KeymapOutput getOutput(string key)
        {
            List<KeymapOutput> list = this.allOutputs;
            foreach (KeymapOutput output in list)
            {
                if (output.Key == key)
                {
                    return output;
                }
            }
            return null;
        }
    }

    public enum InputSource
    {
        IR,
        WIIMOTE,
        NUNCHUK,
        CLASSIC
    }

    public class KeymapInput
    {
        public string Name { get; private set; }
        public string Key { get; private set; }
        public InputSource Source { get; private set; }
        public bool Continous { get; private set; }

        public KeymapInput(InputSource source, string name, string key) : this(source, name,key,false)
        {

        }

        public KeymapInput(InputSource source, string name, string key, bool continous)
        {
            this.Source = source;
            this.Name = name;
            this.Key = key;
            this.Continous = continous;
        }

        public bool canHandle(KeymapOutput output)
        {
            return this.Continous == output.Continous;
        }
    }


    public enum OutputType
    {
        TOUCH,
        KEYBOARD,
        MOUSE,
        XINPUT
    }
    public class KeymapOutput
    {
        public string Name { get; private set; }
        public string Key { get; private set; }
        public OutputType Type { get; private set; }
        public bool Continous { get; private set; }

        public KeymapOutput(OutputType type, string name, string key)
            : this(type, name, key, false)
        {

        }

        public KeymapOutput(OutputType type, string name, string key, bool continous)
        {
            this.Type = type;
            this.Name = name;
            this.Key = key;
            this.Continous = continous;
        }
    }
}
