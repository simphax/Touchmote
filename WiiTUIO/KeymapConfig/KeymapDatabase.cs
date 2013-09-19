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
            allInputs.Add(new KeymapInput(KeymapInputSource.IR, "Pointer", "Pointer", true, true));
            allInputs.Add(new KeymapInput(KeymapInputSource.WIIMOTE, "A", "A"));
            allInputs.Add(new KeymapInput(KeymapInputSource.WIIMOTE, "B", "B"));
            allInputs.Add(new KeymapInput(KeymapInputSource.WIIMOTE, "Home", "Home"));
            allInputs.Add(new KeymapInput(KeymapInputSource.WIIMOTE, "Left", "Left"));

            allOutputs = new List<KeymapOutput>();
            allOutputs.Add(new KeymapOutput(KeymapOutputType.TOUCH, "Touch Cursor", "touch", false, true));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.TOUCH, "Touch Main", "touchmaster"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.TOUCH, "Touch Slave", "touchslave"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "Left", "left"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "Right", "right"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "Up", "up"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "Down", "down"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "Volume Up", "volume_up"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "Volume Down", "volume_down"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "C", "vk_c"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "Tab", "tab"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "Left Win", "lwin"));
        }

        public KeymapSettings getKeymapSettings()
        {
            return new KeymapSettings(Settings.Default.keymaps_config);
        }

        public List<Keymap> getAllKeymaps()
        {
            List<Keymap> list = new List<Keymap>();
            string[] files = Directory.GetFiles(Settings.Default.keymaps_path, "*.json");
            string defaultKeymapFilename = this.getKeymapSettings().getDefaultKeymap();

            Keymap defaultKeymap = new Keymap(null, defaultKeymapFilename);
            list.Add(defaultKeymap);

            foreach (string filepath in files)
            {
                string filename = Path.GetFileName(filepath);
                if (filename != Settings.Default.keymaps_config && filename != defaultKeymapFilename)
                {
                    list.Add(new Keymap(defaultKeymap, filename));
                }
            }
            return list;
        }

        public Keymap getKeymap(string filename)
        {
            List<Keymap> list = this.getAllKeymaps();

            foreach (Keymap keymap in list)
            {
                if (keymap.Filename == filename)
                {
                    return keymap;
                }
            }
            return null;
        }

        public List<KeymapInput> getAvailableInputs(KeymapInputSource source)
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

        public List<KeymapOutput> getAvailableOutputs(KeymapOutputType type)
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
                if (input.Key == key.ToLower())
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
                if (output.Key == key.ToLower())
                {
                    return output;
                }
            }
            return null;
        }
    }

    public enum KeymapInputSource
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
        public KeymapInputSource Source { get; private set; }
        public bool Continous { get; private set; }
        public bool Cursor { get; private set; }

        public KeymapInput(KeymapInputSource source, string name, string key)
            : this(source, name, key, false, false)
        {

        }

        public KeymapInput(KeymapInputSource source, string name, string key, bool continous, bool cursor)
        {
            this.Source = source;
            this.Name = name;
            this.Key = key;
            this.Continous = continous;
            this.Cursor = cursor;
        }

        public bool canHandle(KeymapOutput output)
        {
            return this.Continous == output.Continous || this.Cursor == output.Cursor;
        }
    }


    public enum KeymapOutputType
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
        public KeymapOutputType Type { get; private set; }
        public bool Continous { get; private set; }
        public bool Cursor { get; private set; }

        public KeymapOutput(KeymapOutputType type, string name, string key)
            : this(type, name, key, false, false)
        {

        }

        public KeymapOutput(KeymapOutputType type, string name, string key, bool continous, bool cursor)
        {
            this.Type = type;
            this.Name = name;
            this.Key = key;
            this.Continous = continous;
            this.Cursor = cursor;
        }
    }
}
