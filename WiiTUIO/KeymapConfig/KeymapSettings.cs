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
    public class KeymapSettings
    {
        public string Filename;

        private JObject jsonObj;

        public KeymapSettings(string filename)
        {
            this.Filename = filename;
            if (File.Exists(Settings.Default.keymaps_path + filename))
            {
                StreamReader reader = File.OpenText(Settings.Default.keymaps_path + filename);
                this.jsonObj = (JObject)JToken.ReadFrom(new JsonTextReader(reader));
                reader.Close();
            }
        }

        public string getDefaultKeymap()
        {
            return this.jsonObj.GetValue("Default").ToString();
        }

        public void addToLayoutChooser(Keymap keymap)
        {
            if (this.isInLayoutChooser(keymap))
            {
                return;
            }

            JToken level1 = this.jsonObj.GetValue("LayoutChooser");
            if (level1 == null)
            {
                this.jsonObj.Add("LayoutChooser",new JArray());
            }
            level1 = this.jsonObj.GetValue("LayoutChooser");
            JArray array = (JArray)level1;

            JObject newObj = new JObject();
            newObj.Add("Title", keymap.getName());
            newObj.Add("Keymap", keymap.Filename);
            array.Add(newObj);

            this.jsonObj.Remove("LayoutChooser");
            this.jsonObj.Add("LayoutChooser",array);

            save();
        }

        public void removeFromLayoutChooser(Keymap keymap)
        {
            if (!this.isInLayoutChooser(keymap))
            {
                return;
            }

            JToken level1 = this.jsonObj.GetValue("LayoutChooser");
            if (level1 != null && level1.Type == JTokenType.Array)
            {
                JArray array = (JArray)level1;

                foreach (JToken token in array)
                {
                    if (token.Type == JTokenType.Object)
                    {
                        string filename = ((JObject)token).GetValue("Keymap").ToString();

                        if (filename == keymap.Filename)
                        {
                            token.Remove();

                            this.jsonObj.Remove("LayoutChooser");
                            this.jsonObj.Add("LayoutChooser",array);

                            save();
                            return;
                        }
                    }
                }
            }
        }

        public void addToApplicationSearch(Keymap keymap, string search)
        {
            if (this.isInApplicationSearch(keymap))
            {
                return;
            }

            JToken level1 = this.jsonObj.GetValue("Applications");
            if (level1 == null)
            {
                this.jsonObj.Add("Applications", new JArray());
            }
            level1 = this.jsonObj.GetValue("Applications");
            JArray array = (JArray)level1;

            JObject newObj = new JObject();
            newObj.Add("Search", search);
            newObj.Add("Keymap", keymap.Filename);
            array.Add(newObj);

            this.jsonObj.Remove("Applications");
            this.jsonObj.Add("Applications", array);

            save();
        }

        public void removeFromApplicationSearch(Keymap keymap)
        {
            if (!this.isInApplicationSearch(keymap))
            {
                return;
            }

            JToken level1 = this.jsonObj.GetValue("Applications");
            if (level1 != null && level1.Type == JTokenType.Array)
            {
                JArray array = (JArray)level1;

                foreach (JToken token in array)
                {
                    if (token.Type == JTokenType.Object)
                    {
                        string filename = ((JObject)token).GetValue("Keymap").ToString();

                        if (filename == keymap.Filename)
                        {
                            token.Remove();

                            this.jsonObj.Remove("Applications");
                            this.jsonObj.Add("Applications", array);

                            save();
                            return;
                        }
                    }
                }
            }
        }

        private void save()
        {
            File.WriteAllText(Settings.Default.keymaps_path + this.Filename, this.jsonObj.ToString());
        }

        public bool isInLayoutChooser(Keymap keymap)
        {
            List<LayoutChooserSetting> allKeymaps = this.getLayoutChooserSettings();

            foreach (LayoutChooserSetting setting in allKeymaps)
            {
                if (setting.Keymap == keymap.Filename)
                {
                    return true;
                }
            }
            return false;
        }

        public bool isInApplicationSearch(Keymap keymap)
        {
            List<ApplicationSearchSetting> allKeymaps = this.getApplicationSearchSettings();

            foreach (ApplicationSearchSetting setting in allKeymaps)
            {
                if (setting.Keymap == keymap.Filename)
                {
                    return true;
                }
            }
            return false;
        }

        public string getSearchStringFor(Keymap keymap)
        {
            List<ApplicationSearchSetting> allKeymaps = this.getApplicationSearchSettings();

            foreach (ApplicationSearchSetting setting in allKeymaps)
            {
                if (setting.Keymap == keymap.Filename)
                {
                    return setting.Search;
                }
            }
            return null;
        }

        public void setSearchStringFor(Keymap keymap, string search)
        {
            if (!isInApplicationSearch(keymap))
            {
                return;
            }

            JToken level1 = this.jsonObj.GetValue("Applications");
            if (level1 != null && level1.Type == JTokenType.Array)
            {
                JArray array = (JArray)level1;

                foreach (JToken token in array)
                {
                    if (token.Type == JTokenType.Object)
                    {
                        string filename = ((JObject)token).GetValue("Keymap").ToString();
                        if (filename == keymap.Filename)
                        {
                            ((JObject)token).Remove("Search");
                            ((JObject)token).Add("Search",search);

                            this.jsonObj.Remove("Applications");
                            this.jsonObj.Add("Applications", array);

                            save();

                            return;
                        }
                    }
                }

            }
        }

        public List<LayoutChooserSetting> getLayoutChooserSettings()
        {
            List<LayoutChooserSetting> result = new List<LayoutChooserSetting>();

            JToken level1 = this.jsonObj.GetValue("LayoutChooser");
            if (level1 != null && level1.Type == JTokenType.Array)
            {
                JArray array = (JArray)level1;

                foreach (JToken token in array)
                {
                    if (token.Type == JTokenType.Object)
                    {
                        string title = ((JObject)token).GetValue("Title").ToString();
                        string keymap = ((JObject)token).GetValue("Keymap").ToString();
                        result.Add(new LayoutChooserSetting(title, keymap));
                    }
                }
            }

            return result;
        }

        public List<ApplicationSearchSetting> getApplicationSearchSettings()
        {
            List<ApplicationSearchSetting> result = new List<ApplicationSearchSetting>();

            JToken level1 = this.jsonObj.GetValue("Applications");
            if (level1 != null && level1.Type == JTokenType.Array)
            {
                JArray array = (JArray)level1;

                foreach (JToken token in array)
                {
                    if (token.Type == JTokenType.Object)
                    {
                        string search = ((JObject)token).GetValue("Search").ToString();
                        string keymap = ((JObject)token).GetValue("Keymap").ToString();
                        result.Add(new ApplicationSearchSetting(search, keymap));
                    }
                }
            }

            return result;
        }
    }

    public class LayoutChooserSetting
    {
        public string Title;
        public string Keymap;

        public LayoutChooserSetting(string title, string keymap)
        {
            this.Title = title;
            this.Keymap = keymap;
        }
    }

    public class ApplicationSearchSetting
    {
        public string Search;
        public string Keymap;

        public ApplicationSearchSetting(string search, string keymap)
        {
            this.Search = search;
            this.Keymap = keymap;
        }
    }
}
