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

            //Needed to update the name in layout chooser because its names are stored in a different file
            KeymapSettings settings = new KeymapSettings(Settings.Default.keymaps_config);
            if(settings.isInLayoutChooser(this))
            {
                settings.removeFromLayoutChooser(this);
                settings.addToLayoutChooser(this);
            }

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

                JToken outputs = null;

                if (config.Stack.Count > 1)
                {
                    JArray array = new JArray();
                    foreach(KeymapOutput output in config.Stack)
                    {
                        array.Add(output.Key);
                    }
                    outputs = array;
                }
                else if (config.Stack.Count == 1)
                {
                    outputs = config.Stack.First().Key;
                }

                if (config.Scale != Settings.Default.defaultContinousScale || config.Threshold != Settings.Default.defaultContinousPressThreshold || config.Deadzone != Settings.Default.defaultContinousDeadzone)
                {
                    JObject settings = new JObject();
                    if (config.Scale != Settings.Default.defaultContinousScale)
                    {
                        settings.Add("scale", config.Scale);
                    }
                    if (config.Threshold != Settings.Default.defaultContinousPressThreshold)
                    {
                        settings.Add("threshold", config.Threshold);
                    }
                    if (config.Deadzone != Settings.Default.defaultContinousDeadzone)
                    {
                        settings.Add("deadzone", config.Deadzone);
                    }
                    settings.Add("output", outputs);
                    outputs = settings;
                }

                ((JObject)level1).Add(input.Key, outputs);

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
                    else if (level2.Type == JTokenType.Object)
                    {
                        JToken level3 = ((JObject)level2).GetValue("output");
                        if (level3 != null)
                        {
                            KeymapOutConfig outconfig = null;
                            if (level3.Type == JTokenType.String)
                            {
                                if (KeymapDatabase.Current.getOutput(level3.ToString().ToLower()) != null)
                                {
                                    outconfig = new KeymapOutConfig(KeymapDatabase.Current.getOutput(level3.ToString().ToLower()), false);
                                }
                            }
                            else if (level3.Type == JTokenType.Array)
                            {
                                JArray array = (JArray)level3;
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
                                    outconfig = new KeymapOutConfig(result, false);
                                }
                            }

                            if (outconfig != null)
                            {
                                if (((JObject)level2).GetValue("scale") != null && ((JObject)level2).GetValue("scale").Type == JTokenType.Float)
                                {
                                    outconfig.Scale = Double.Parse(((JObject)level2).GetValue("scale").ToString());
                                }

                                if (((JObject)level2).GetValue("threshold") != null && ((JObject)level2).GetValue("threshold").Type == JTokenType.Float)
                                {
                                    outconfig.Threshold = Double.Parse(((JObject)level2).GetValue("threshold").ToString());
                                }

                                if (((JObject)level2).GetValue("deadzone") != null && ((JObject)level2).GetValue("deadzone").Type == JTokenType.Float)
                                {
                                    outconfig.Deadzone = Double.Parse(((JObject)level2).GetValue("deadzone").ToString());
                                }
                                return outconfig;
                            }
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
        public double Scale = Settings.Default.defaultContinousScale;
        public double Threshold = Settings.Default.defaultContinousPressThreshold;
        public double Deadzone = Settings.Default.defaultContinousDeadzone;
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
