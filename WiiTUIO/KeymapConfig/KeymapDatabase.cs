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

        public List<KeymapInput> getAvailableInputs()
        {
            List<KeymapInput> list = new List<KeymapInput>();
            list.Add(new KeymapInput("A","A", false));
            list.Add(new KeymapInput("B", "B", false));
            list.Add(new KeymapInput("Home", "Home", false));
            list.Add(new KeymapInput("Left", "Left", false));

            return list;
        }

        public List<KeymapOutput> getAvailableOutputs()
        {
            List<KeymapOutput> list = new List<KeymapOutput>();
            list.Add(new KeymapOutput("Left Win", "LWin", false));
            return list;
        }
    }

    public class KeymapInput
    {
        public string Name;
        public string Key;
        public bool Continous;

        public KeymapInput(string name, string key, bool continous)
        {
            this.Name = name;
            this.Key = key;
            this.Continous = continous;
        }
    }
    public class KeymapOutput
    {
        public string Name;
        public string Key;
        public bool Continous;

        public KeymapOutput(string name, string key, bool continous)
        {
            this.Name = name;
            this.Key = key;
            this.Continous = continous;
        }
    }
}
