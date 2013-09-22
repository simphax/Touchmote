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
                if (level2 != null)
                {
                    ((JObject)level1).Remove(input.Key);
                }

                if (config.Stack.Count > 1)
                {
                    JArray array = new JArray();
                    foreach(KeymapOutput output in config.Stack)
                    {
                        array.Add(output.Key);
                    }
                    ((JObject)level1).Add(input.Key, array);
                }
                else if (config.Stack.Count == 1)
                {
                    ((JObject)level1).Add(input.Key, config.Stack.First().Key);
                }

                jsonObj.Remove(key);
                jsonObj.Add(key, level1);
            }
            save();
        }

        //0 = all
        public KeymapOutConfig getConfigFor(int controllerId, string input)
        {
            string key;
            if (controllerId > 0)
            {
                key = "" + controllerId;
            }
            else
            {
                key = "All";
            }

            JToken level1 = this.jsonObj.GetValue(key);
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
                    else if (level2.Type == JTokenType.Array)
                    {
                        JArray array = (JArray)level2;
                        List<KeymapOutput> result = new List<KeymapOutput>();
                        foreach (JValue value in array)
                        {
                            if (KeymapDatabase.Current.getOutput(value.ToString().ToLower()) != null)
                            {
                                result.Add(KeymapDatabase.Current.getOutput(value.ToString().ToLower()));
                            }
                        }
                        if (result.Count == array.Count)
                        {
                            return new KeymapOutConfig(result, false);
                        }
                    }
                }
            }
            if(controllerId > 0)
            {
                //If we are searching for controller-specific keymaps we can inherit from the "All" setting.
                KeymapOutConfig result = this.getConfigFor(0, input);
                if (result != null)
                {
                    result.Inherited = true;
                }
                return result;
            }
            //If we can not find any setting in the All group, search for inherit from the default keymap
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
                //This will happen if we request a input string that is not defined in this nor the default keymap.
                return null;
            }
        }

    }

    public class KeymapOutConfig
    {
        public bool Inherited;
        public List<KeymapOutput> Stack;

        public KeymapOutConfig(KeymapOutput output, bool inherited)
        {
            this.Stack = new List<KeymapOutput>();
            this.Stack.Add(output);
            this.Inherited = inherited;
        }

        public KeymapOutConfig(List<KeymapOutput> output, bool inherited)
        {
            this.Stack = output;
            this.Inherited = inherited;
        }

        public void addOutput(KeymapOutput output)
        {
            this.Stack.Add(output);
        }
    }
}
