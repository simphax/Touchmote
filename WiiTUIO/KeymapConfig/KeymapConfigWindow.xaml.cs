using MahApps.Metro.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WiiTUIO
{
    /// <summary>
    /// Interaction logic for KeymapConfigWindow.xaml
    /// </summary>
    public partial class KeymapConfigWindow : MetroWindow
    {
        private static KeymapConfigWindow defaultInstance;
        public static KeymapConfigWindow Instance
        {
            get
            {
                if (defaultInstance == null)
                {
                    defaultInstance = new KeymapConfigWindow();
                }
                return defaultInstance;
            }
        }

        public KeymapConfigWindow()
        {
            InitializeComponent();

            List<Keymap> allKeymaps = KeymapDatabase.Current.getAllKeymaps();

            foreach (Keymap keymap in allKeymaps)
            {
                KeymapRow row = new KeymapRow(keymap);
                row.OnClick += Select_Keymap;
                this.spLayoutList.Children.Add(row);
            }

            List<KeymapOutput> allKeyboardOutputs = KeymapDatabase.Current.getAvailableOutputs(OutputType.KEYBOARD);

            foreach (KeymapOutput output in allKeyboardOutputs)
            {
                KeymapOutputRow row = new KeymapOutputRow(output);
                this.spOutputList.Children.Add(row);
            }

        }

        private void Select_Keymap(Keymap keymap)
        {
            List<KeymapInput> allInputs = KeymapDatabase.Current.getAvailableInputs(InputSource.WIIMOTE);

            this.spConnections.Children.Clear();

            foreach (KeymapInput input in allInputs)
            {
                string config = keymap.getConfigFor(0, input.Key);
                if (config != null)
                {
                    KeymapOutput output = KeymapDatabase.Current.getOutput(config.ToLower());
                    if(output != null)
                    {
                        this.spConnections.Children.Add(new KeymapConnectionRow(input,output));
                    }
                }
            }
        }

        /*
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Visibility = System.Windows.Visibility.Collapsed;
        }
        */
    }
}
