using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiiTUIO.Properties;

namespace WiiTUIO
{
    public class Keymap
    {
        public string Filename;
        public string Name;
        public bool DefaultKeymap;
        public bool InLayoutChooser;

        private JObject jsonObj;

        public Keymap(string filename)
        {
            this.Filename = filename;
            if (File.Exists(Settings.Default.keymaps_path + filename))
            {
                StreamReader reader = File.OpenText(Settings.Default.keymaps_path + filename);
                this.jsonObj = (JObject)JToken.ReadFrom(new JsonTextReader(reader));
                reader.Close();

                this.Name = this.jsonObj.GetValue("Title").ToString();
            }
        }

        public string getFilename()
        {
            return this.Filename;
        }

        public string getName()
        {
            return this.Name;
        }

        //0 = all
        public string getConfigFor(int controllerId, string input)
        {
            JToken level1 = this.jsonObj.GetValue("All");
            if (level1 != null && level1.Type == JTokenType.Object)
            {
                JToken level2 = ((JObject)level1).GetValue(input);
                if (level2 != null)
                {
                    if (level2.Type == JTokenType.String)
                    {
                        return level2.ToString();
                    }
                }
            }
            return null;
        }

    }

    public class KeymapOutConfig
    {
        public bool Inherited;
        public KeymapOutput Output;
    }
}
