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
        private KeymapOutputType selectedOutput = KeymapOutputType.KEYBOARD;

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

            this.tbKeymapTitle.Text = this.tbKeymapTitle.Tag.ToString();
            this.tbKeymapTitle.Foreground = new SolidColorBrush(Colors.Gray);
            this.tbKeymapAppSearch.Text = this.tbKeymapAppSearch.Tag.ToString();
            this.tbKeymapAppSearch.Foreground = new SolidColorBrush(Colors.Gray);
            this.tbOutputFilter.Text = this.tbOutputFilter.Tag.ToString();
            this.tbOutputFilter.Foreground = new SolidColorBrush(Colors.Gray);


            this.fillKeymapList();
            this.fillOutputList(selectedOutput, null);
            this.selectKeymap(KeymapDatabase.Current.getKeymap(KeymapDatabase.Current.getKeymapSettings().getDefaultKeymap()));
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
            List<KeymapInput> allIrInputs = KeymapDatabase.Current.getAvailableInputs(KeymapInputSource.IR);

            this.spWiimoteConnections.Children.Clear();

            foreach (KeymapInput input in allIrInputs)
            {
                KeymapOutConfig config = keymap.getConfigFor(0, input.Key);
                if (config != null)
                {
                    this.spWiimoteConnections.Children.Add(new KeymapConnectionRow(input, config));
                }
            }

            List<KeymapInput> allWiimoteInputs = KeymapDatabase.Current.getAvailableInputs(KeymapInputSource.WIIMOTE);

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
                    this.selectedOutput = KeymapOutputType.KEYBOARD;
                }
                else if (cbItem == cbiTouch)
                {
                    this.selectedOutput = KeymapOutputType.TOUCH;
                }
                else if (cbItem == cbiMouse)
                {
                    this.selectedOutput = KeymapOutputType.MOUSE;
                }
                else if (cbItem == cbi360)
                {
                    this.selectedOutput = KeymapOutputType.XINPUT;
                }
                else if (cbItem == cbiOther)
                {
                    this.selectedOutput = KeymapOutputType.DISABLE;
                }
                this.fillOutputList(this.selectedOutput,"");
            }
        }


        private void fillOutputList(KeymapOutputType type, string filter)
        {
            this.spOutputList.Children.Clear();
            List<KeymapOutput> allOutputs = KeymapDatabase.Current.getAvailableOutputs(type);
            allOutputs.Sort(new KeymapOutputComparer());

            foreach (KeymapOutput output in allOutputs)
            {
                if (filter == null || filter == "" || output.Name.ToLower().Contains(filter.ToLower()))
                {
                    KeymapOutputRow row = new KeymapOutputRow(output);
                    row.OnDragStart += output_OnDragStart;
                    row.OnDragStop += output_OnDragStop;
                    this.spOutputList.Children.Add(row);
                }
            }
        }

        private void tbOutputFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.tbOutputFilter.Text == this.tbOutputFilter.Tag.ToString())
            {
                this.fillOutputList(selectedOutput, null);
            }
            else
            {
                this.fillOutputList(selectedOutput, this.tbOutputFilter.Text);
            }
        }

        private void tb_placeholder_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox)
            {
                TextBox tb = (TextBox)sender;
                if (tb.Text == tb.Tag.ToString())
                {
                    tb.Text = "";
                    tb.Foreground = new SolidColorBrush(Colors.Black);
                }
            }
        }

        private void tb_placeholder_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox)
            {
                TextBox tb = (TextBox)sender;
                if (tb.Text == "")
                {
                    tb.Text = tb.Tag.ToString();
                    tb.Foreground = new SolidColorBrush(Colors.Gray);
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
