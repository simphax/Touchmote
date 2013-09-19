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
