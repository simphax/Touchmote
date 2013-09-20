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
            }
            else
            {
                this.jsonObj = new JObject();
                this.jsonObj.Add("Title", "New Keymap");
                save();
            }
        }

        public string getFilename()
        {
            return this.Filename;
        }

        public string getName()
        {
            return this.jsonObj.GetValue("Title").ToString();
        }

        public void setName(string name)
        {
            this.jsonObj.Remove("Title");
            this.jsonObj.Add("Title", name);
            save();
        }

        private void save()
        {
            File.WriteAllText(Settings.Default.keymaps_path + this.Filename, this.jsonObj.ToString());
        }

        public void setConfigFor(int controllerId, KeymapInput input, KeymapOutConfig config)
        {
            string key;
            if (controllerId == 0)
            {
                key = "All";
            }
            else
            {
                key = "" + controllerId;
            }

            {
                JToken level1 = this.jsonObj.GetValue(key);
                if (level1 == null || level1.Type != JTokenType.Object)
                {
                    jsonObj.Add(key,new JObject());
                }
                level1 = this.jsonObj.GetValue(key);
                JToken level2 = ((JObject)level1).GetValue(input.Key);
                if (level2 == null)
                {
                    ((JObject)level1).Add(input.Key, config.Output.Key);
                }
                else
                {
                    ((JObject)level1).Remove(input.Key);
                    ((JObject)level1).Add(input.Key, config.Output.Key);
                }
                jsonObj.Remove(key);
                jsonObj.Add(key, level1);
            }
            save();
        }

        //0 = all
        public KeymapOutConfig getConfigFor(int controllerId, string input)
        {
            if (controllerId > 0)
            {
                JToken level1 = this.jsonObj.GetValue("" + controllerId);
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
            }
            {
                //No controller-specific keymapping was found so we search for the "All" settings
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
                                return new KeymapOutConfig(KeymapDatabase.Current.getOutput(level2.ToString().ToLower()), controllerId > 0);
                            }
                        }
                    }
                }
            }
            //If we can not find any setting in the All group, search for it in the default keymap
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
                //This will only happen if we request a input string that is not defined in the default keymap.
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
