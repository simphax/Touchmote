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
        private AdornerLayer adornerLayer;

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

            this.fillKeymapList();
            this.fillOutputList(KeymapOutputType.KEYBOARD);
            this.selectKeymap(KeymapDatabase.Current.getKeymap("default.json"));
        }

        private void fillKeymapList()
        {
            List<Keymap> allKeymaps = KeymapDatabase.Current.getAllKeymaps();

            foreach (Keymap keymap in allKeymaps)
            {
                KeymapRow row = new KeymapRow(keymap);
                row.OnClick += selectKeymap;
                this.spLayoutList.Children.Add(row);
            }
        }

        private void output_OnDragStop(Adorner obj)
        {
            this.adornerLayer.Remove(obj);
        }

        private void output_OnDragStart(Adorner obj)
        {
            if (this.adornerLayer == null)
            {
                this.adornerLayer = AdornerLayer.GetAdornerLayer(this.mainPanel);
            }
            if (!this.adornerLayer.GetChildObjects().Contains(obj))
            {
                this.adornerLayer.Add(obj);
            }
        }


        private void selectKeymap(Keymap keymap)
        {
            List<KeymapInput> allWiimoteInputs = KeymapDatabase.Current.getAvailableInputs(KeymapInputSource.WIIMOTE);

            this.spWiimoteConnections.Children.Clear();

            foreach (KeymapInput input in allWiimoteInputs)
            {
                KeymapOutConfig config = keymap.getConfigFor(0, input.Key);
                if (config != null)
                {
                    this.spWiimoteConnections.Children.Add(new KeymapConnectionRow(input, config));
                }
            }
            
            List<KeymapInput> allNunchukInputs = KeymapDatabase.Current.getAvailableInputs(KeymapInputSource.NUNCHUK);

            this.spNunchukConnections.Children.Clear();

            foreach (KeymapInput input in allNunchukInputs)
            {
                KeymapOutConfig config = keymap.getConfigFor(0, input.Key);
                if (config != null)
                {
                    this.spNunchukConnections.Children.Add(new KeymapConnectionRow(input, config));
                }
            }

            List<KeymapInput> allClassicInputs = KeymapDatabase.Current.getAvailableInputs(KeymapInputSource.CLASSIC);

            this.spClassicConnections.Children.Clear();

            foreach (KeymapInput input in allNunchukInputs)
            {
                KeymapOutConfig config = keymap.getConfigFor(0, input.Key);
                if (config != null)
                {
                    this.spClassicConnections.Children.Add(new KeymapConnectionRow(input, config));
                }
            }
        }

        private void cbOutput_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbOutput.SelectedItem != null && ((ComboBoxItem)cbOutput.SelectedItem).Content != null)
            {
                ComboBoxItem cbItem = (ComboBoxItem)cbOutput.SelectedItem;
                if (cbItem == cbiKeyboard)
                {
                    this.fillOutputList(KeymapOutputType.KEYBOARD);
                }
                else if (cbItem == cbiTouch)
                {
                    this.fillOutputList(KeymapOutputType.TOUCH);
                }
                else if (cbItem == cbiMouse)
                {
                    this.fillOutputList(KeymapOutputType.MOUSE);
                }
                else if (cbItem == cbi360)
                {
                    this.fillOutputList(KeymapOutputType.XINPUT);
                }
            }
        }


        private void fillOutputList(KeymapOutputType type)
        {
            this.spOutputList.Children.Clear();
            List<KeymapOutput> allKeyboardOutputs = KeymapDatabase.Current.getAvailableOutputs(type);

            foreach (KeymapOutput output in allKeyboardOutputs)
            {
                KeymapOutputRow row = new KeymapOutputRow(output);
                row.OnDragStart += output_OnDragStart;
                row.OnDragStop += output_OnDragStop;
                this.spOutputList.Children.Add(row);
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
