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

        public Keymap Parent;

        private JObject jsonObj;

        public Keymap(Keymap parent, string filename)
        {
            this.Parent = parent;
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
        public KeymapOutConfig getConfigFor(int controllerId, string input)
        {
            JToken level1 = this.jsonObj.GetValue("All");
            if (level1 != null && level1.Type == JTokenType.Object)
            {
                JToken level2 = ((JObject)level1).GetValue(input);
                if (level2 != null)
                {
                    if (level2.Type == JTokenType.String)
                    {
                        if (KeymapDatabase.Current.getOutput(level2.ToString().ToLower()) != null)
                        {
                            return new KeymapOutConfig(KeymapDatabase.Current.getOutput(level2.ToString().ToLower()), false);
                        }
                    }
                }
            }
            if (this.Parent != null)
            {
                KeymapOutConfig result = this.Parent.getConfigFor(controllerId, input);
                if (result != null)
                {
                    result.Inherited = true;
                }
                return result;
            }
            else
            {
                return null;
            }
        }

    }

    public class KeymapOutConfig
    {
        public bool Inherited;
        public KeymapOutput Output;

        public KeymapOutConfig(KeymapOutput output, bool inherited)
        {
            this.Output = output;
            this.Inherited = inherited;
        }
    }
}
