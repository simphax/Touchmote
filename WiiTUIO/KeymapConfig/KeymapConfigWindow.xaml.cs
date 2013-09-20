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

        private SolidColorBrush defaultBrush = new SolidColorBrush(Color.FromRgb(46,46,46));
        private SolidColorBrush highlightBrush = new SolidColorBrush(Color.FromRgb(65, 177, 225));

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
            this.tbApplicationSearch.Text = this.tbApplicationSearch.Tag.ToString();
            this.tbApplicationSearch.Foreground = new SolidColorBrush(Colors.Gray);
            this.tbOutputFilter.Text = this.tbOutputFilter.Tag.ToString();
            this.tbOutputFilter.Foreground = new SolidColorBrush(Colors.Gray);


            this.fillOutputList(selectedOutput, null);
            this.selectKeymap(KeymapDatabase.Current.getKeymap(KeymapDatabase.Current.getKeymapSettings().getDefaultKeymap()));
            this.fillKeymapList();
            
            this.btnAll.IsEnabled = false;
            btnAllBorder.Background = highlightBrush;
            
            this.tbKeymapTitle.LostFocus += tbKeymapTitle_LostFocus;
            this.tbKeymapTitle.KeyUp += tbKeymapTitle_KeyUp;
            this.tbKeymapTitle.Foreground = new SolidColorBrush(Colors.Black);

            this.tbApplicationSearch.LostFocus += tbApplicationSearch_LostFocus;
            this.tbApplicationSearch.KeyUp += tbApplicationSearch_KeyUp;

            this.cbLayoutChooser.Checked += cbLayoutChooser_Checked;
            this.cbLayoutChooser.Unchecked += cbLayoutChooser_Unchecked;

            this.cbApplicationSearch.Checked += cbApplicationSearch_Checked;
            this.cbApplicationSearch.Unchecked += cbApplicationSearch_Unchecked;

        }

        private void fillKeymapList()
        {
            List<Keymap> allKeymaps = KeymapDatabase.Current.getAllKeymaps();
            this.spLayoutList.Children.Clear();
            foreach (Keymap keymap in allKeymaps)
            {
                bool active = this.currentKeymap.Filename == keymap.Filename;
                bool defaultk = keymap.Filename == KeymapDatabase.Current.getKeymapSettings().getDefaultKeymap();
                KeymapRow row = new KeymapRow(keymap,active,defaultk);
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
            string searchstring = KeymapDatabase.Current.getKeymapSettings().getSearchStringFor(this.currentKeymap);
            if (searchstring != null && searchstring != "")
            {
                this.tbApplicationSearch.Text = searchstring;
                this.tbApplicationSearch.Foreground = new SolidColorBrush(Colors.Black);
            }
            else
            {
                this.tbApplicationSearch.Text = this.tbApplicationSearch.Tag.ToString();
                this.tbApplicationSearch.Foreground = new SolidColorBrush(Colors.Gray);
            }
            this.cbApplicationSearch.IsChecked = KeymapDatabase.Current.getKeymapSettings().isInApplicationSearch(this.currentKeymap);
            this.cbLayoutChooser.IsChecked = KeymapDatabase.Current.getKeymapSettings().isInLayoutChooser(this.currentKeymap);

            this.fillConnectionLists(keymap, 0);

            this.fillKeymapList();
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
            btnAllBorder.Background = highlightBrush;
            btn1.IsEnabled = true;
            btn1Border.Background = defaultBrush;
            btn2.IsEnabled = true;
            btn2Border.Background = defaultBrush;
            btn3.IsEnabled = true;
            btn3Border.Background = defaultBrush;
            btn4.IsEnabled = true;
            btn4Border.Background = defaultBrush;
            this.selectWiimoteNumber(0);
        }

        private void btn1_Click(object sender, RoutedEventArgs e)
        {
            btnAll.IsEnabled = true;
            btnAllBorder.Background = defaultBrush;
            btn1.IsEnabled = false;
            btn1Border.Background = highlightBrush;
            btn2.IsEnabled = true;
            btn2Border.Background = defaultBrush;
            btn3.IsEnabled = true;
            btn3Border.Background = defaultBrush;
            btn4.IsEnabled = true;
            btn4Border.Background = defaultBrush;
            this.selectWiimoteNumber(1);
        }

        private void btn2_Click(object sender, RoutedEventArgs e)
        {
            btnAll.IsEnabled = true;
            btnAllBorder.Background = defaultBrush;
            btn1.IsEnabled = true;
            btn1Border.Background = defaultBrush;
            btn2.IsEnabled = false;
            btn2Border.Background = highlightBrush;
            btn3.IsEnabled = true;
            btn3Border.Background = defaultBrush;
            btn4.IsEnabled = true;
            btn4Border.Background = defaultBrush;
            this.selectWiimoteNumber(2);
        }

        private void btn3_Click(object sender, RoutedEventArgs e)
        {
            btnAll.IsEnabled = true;
            btnAllBorder.Background = defaultBrush;
            btn1.IsEnabled = true;
            btn1Border.Background = defaultBrush;
            btn2.IsEnabled = true;
            btn2Border.Background = defaultBrush;
            btn3.IsEnabled = false;
            btn3Border.Background = highlightBrush;
            btn4.IsEnabled = true;
            btn4Border.Background = defaultBrush;
            this.selectWiimoteNumber(3);
        }

        private void btn4_Click(object sender, RoutedEventArgs e)
        {
            btnAll.IsEnabled = true;
            btnAllBorder.Background = defaultBrush;
            btn1.IsEnabled = true;
            btn1Border.Background = defaultBrush;
            btn2.IsEnabled = true;
            btn2Border.Background = defaultBrush;
            btn3.IsEnabled = true;
            btn3Border.Background = defaultBrush;
            btn4.IsEnabled = false;
            btn4Border.Background = highlightBrush;
            this.selectWiimoteNumber(4);
        }

        private void cbLayoutChooser_Checked(object sender, RoutedEventArgs e)
        {
            KeymapDatabase.Current.getKeymapSettings().addToLayoutChooser(this.currentKeymap);
        }

        private void cbLayoutChooser_Unchecked(object sender, RoutedEventArgs e)
        {
            KeymapDatabase.Current.getKeymapSettings().removeFromLayoutChooser(this.currentKeymap);
        }


        private void cbApplicationSearch_Checked(object sender, RoutedEventArgs e)
        {
            KeymapDatabase.Current.getKeymapSettings().addToApplicationSearch(this.currentKeymap,this.tbApplicationSearch.Text);
        }

        private void cbApplicationSearch_Unchecked(object sender, RoutedEventArgs e)
        {
            KeymapDatabase.Current.getKeymapSettings().removeFromApplicationSearch(this.currentKeymap);
        }

        private void tbApplicationSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            if (tbApplicationSearch.Text != "" && tbApplicationSearch.Text != tbApplicationSearch.Tag.ToString())
            {
                KeymapDatabase.Current.getKeymapSettings().setSearchStringFor(this.currentKeymap, tbApplicationSearch.Text);
            }
        }

        void tbApplicationSearch_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                if (tbApplicationSearch.Text != "" && tbApplicationSearch.Text != tbApplicationSearch.Tag.ToString())
                {
                    KeymapDatabase.Current.getKeymapSettings().setSearchStringFor(this.currentKeymap, tbApplicationSearch.Text);
                }
            }
        }

        private void tbDelete_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (MessageBoxResult.Yes == MessageBox.Show("This will pernamently delete the file " + this.currentKeymap.Filename + ", are you sure?", "Delete keymap confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning))
            {
                if (KeymapDatabase.Current.deleteKeymap(this.currentKeymap))
                {
                    this.selectKeymap(KeymapDatabase.Current.getDefaultKeymap());
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
