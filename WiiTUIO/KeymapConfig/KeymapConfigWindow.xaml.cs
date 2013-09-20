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
        private int selectedWiimote = 0;
        private Keymap currentKeymap;

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
            this.btnAll.IsEnabled = false;
            
            this.tbKeymapTitle.LostFocus += tbKeymapTitle_LostFocus;
            this.tbKeymapTitle.KeyUp += tbKeymapTitle_KeyUp;
            this.tbKeymapTitle.Foreground = new SolidColorBrush(Colors.Black);
        }



        private void fillKeymapList()
        {
            List<Keymap> allKeymaps = KeymapDatabase.Current.getAllKeymaps();
            this.spLayoutList.Children.Clear();
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

        private void selectWiimoteNumber(int number)
        {
            this.selectedWiimote = number;
            this.fillConnectionLists(currentKeymap, number);
        }


        private void selectKeymap(Keymap keymap)
        {
            this.currentKeymap = keymap;

            this.tbKeymapTitle.Text = keymap.getName();

            this.fillConnectionLists(keymap, 0);
        }

        private void fillConnectionLists(Keymap keymap, int wiimote)
        {
            this.spWiimoteConnections.Children.Clear();

            bool defaultKeymap = keymap.Filename == KeymapDatabase.Current.getDefaultKeymap().Filename;

            List<KeymapInput> allIrInputs = KeymapDatabase.Current.getAvailableInputs(KeymapInputSource.IR);
            foreach (KeymapInput input in allIrInputs)
            {
                KeymapOutConfig config = keymap.getConfigFor(wiimote, input.Key);
                if (config != null)
                {
                    KeymapConnectionRow row = new KeymapConnectionRow(input, config, defaultKeymap);
                    row.OnConfigChanged += connectionRow_OnConfigChanged;
                    this.spWiimoteConnections.Children.Add(row);
                }
            }

            List<KeymapInput> allWiimoteInputs = KeymapDatabase.Current.getAvailableInputs(KeymapInputSource.WIIMOTE);

            foreach (KeymapInput input in allWiimoteInputs)
            {
                KeymapOutConfig config = keymap.getConfigFor(wiimote, input.Key);
                if (config != null)
                {
                    KeymapConnectionRow row = new KeymapConnectionRow(input, config, defaultKeymap);
                    row.OnConfigChanged += connectionRow_OnConfigChanged;
                    this.spWiimoteConnections.Children.Add(row);
                }
            }

            List<KeymapInput> allNunchukInputs = KeymapDatabase.Current.getAvailableInputs(KeymapInputSource.NUNCHUK);

            this.spNunchukConnections.Children.Clear();

            foreach (KeymapInput input in allNunchukInputs)
            {
                KeymapOutConfig config = keymap.getConfigFor(wiimote, input.Key);
                if (config != null)
                {
                    KeymapConnectionRow row = new KeymapConnectionRow(input, config, defaultKeymap);
                    row.OnConfigChanged += connectionRow_OnConfigChanged;
                    this.spWiimoteConnections.Children.Add(row);
                }
            }

            List<KeymapInput> allClassicInputs = KeymapDatabase.Current.getAvailableInputs(KeymapInputSource.CLASSIC);

            this.spClassicConnections.Children.Clear();

            foreach (KeymapInput input in allNunchukInputs)
            {
                KeymapOutConfig config = keymap.getConfigFor(wiimote, input.Key);
                if (config != null)
                {
                    KeymapConnectionRow row = new KeymapConnectionRow(input, config, defaultKeymap);
                    row.OnConfigChanged += connectionRow_OnConfigChanged;
                    this.spWiimoteConnections.Children.Add(row);
                }
            }
        }

        private void connectionRow_OnConfigChanged(KeymapInput input, KeymapOutConfig config)
        {
            this.currentKeymap.setConfigFor(this.selectedWiimote, input, config);
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

        private void tbKeymapTitle_LostFocus(object sender, RoutedEventArgs e)
        {
            if (tbKeymapTitle.Text != "" && tbKeymapTitle.Text != tbKeymapTitle.Tag.ToString())
            {
                this.currentKeymap.setName(this.tbKeymapTitle.Text);
                this.fillKeymapList();
            }
        }

        void tbKeymapTitle_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                if (tbKeymapTitle.Text != "" && tbKeymapTitle.Text != tbKeymapTitle.Tag.ToString())
                {
                    this.currentKeymap.setName(this.tbKeymapTitle.Text);
                    this.fillKeymapList();
                }
            }
        }

        private void btnAll_Click(object sender, RoutedEventArgs e)
        {
            btnAll.IsEnabled = false;
            btn1.IsEnabled = true;
            btn2.IsEnabled = true;
            btn3.IsEnabled = true;
            btn4.IsEnabled = true;
            this.selectWiimoteNumber(0);
        }

        private void btn1_Click(object sender, RoutedEventArgs e)
        {
            btnAll.IsEnabled = true;
            btn1.IsEnabled = false;
            btn2.IsEnabled = true;
            btn3.IsEnabled = true;
            btn4.IsEnabled = true;
            this.selectWiimoteNumber(1);
        }

        private void btn2_Click(object sender, RoutedEventArgs e)
        {
            btnAll.IsEnabled = true;
            btn1.IsEnabled = true;
            btn2.IsEnabled = false;
            btn3.IsEnabled = true;
            btn4.IsEnabled = true;
            this.selectWiimoteNumber(2);
        }

        private void btn3_Click(object sender, RoutedEventArgs e)
        {
            btnAll.IsEnabled = true;
            btn1.IsEnabled = true;
            btn2.IsEnabled = true;
            btn3.IsEnabled = false;
            btn4.IsEnabled = true;
            this.selectWiimoteNumber(3);
        }

        private void btn4_Click(object sender, RoutedEventArgs e)
        {
            btnAll.IsEnabled = true;
            btn1.IsEnabled = true;
            btn2.IsEnabled = true;
            btn3.IsEnabled = true;
            btn4.IsEnabled = false;
            this.selectWiimoteNumber(4);
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
