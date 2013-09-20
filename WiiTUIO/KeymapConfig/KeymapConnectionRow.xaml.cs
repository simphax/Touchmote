using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using WiiTUIO.Provider;

namespace WiiTUIO
{
    /// <summary>
    /// Interaction logic for LayoutSelectionRow.xaml
    /// </summary>
    public partial class KeymapConnectionRow : UserControl
    {
        private KeymapInput input;
        private KeymapOutConfig config;

        public Action<KeymapInput, KeymapOutConfig> OnConfigChanged;

        private bool fromDefault;

        public KeymapConnectionRow(KeymapInput input, KeymapOutConfig config, bool fromDefault)
        {
            InitializeComponent();
            this.input = input;
            this.config = config;
            this.fromDefault = fromDefault;

            this.connection_input_name.Text = input.Name;

            this.SetConfig(config);
        }

        public void SetConfig(KeymapOutConfig config)
        {
            this.config = config;
            this.connection_output_name.Text = config.Output.Name;

            if (config.Inherited)
            {
                this.connection_output_border.BorderBrush = new SolidColorBrush(Colors.LightGray);
                this.rClear.Visibility = Visibility.Hidden;
            }
            else
            {
                this.connection_output_border.BorderBrush = new SolidColorBrush(KeymapColors.GetColor(config.Output.Type));
                this.rClear.Visibility = Visibility.Visible;
            }

            if (fromDefault)
            {
                this.rClear.Visibility = Visibility.Hidden;
            }

            if (OnConfigChanged != null)
            {
                OnConfigChanged(this.input,this.config);
            }
        }

        private void connection_output_border_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("KeymapOutput"))
            {
                KeymapOutput newOutput = (KeymapOutput)e.Data.GetData("KeymapOutput");
                if (this.input.canHandle(newOutput))
                {
                    this.SetConfig(new KeymapOutConfig(newOutput,false));
                }
                if (e.Data.GetDataPresent("KeymapOutputRow"))
                {
                    ((KeymapOutputRow)e.Data.GetData("KeymapOutputRow")).DropDone();
                }
            }
            else
            {
                if (e.Data.GetDataPresent("KeymapOutputRow"))
                {
                    ((KeymapOutputRow)e.Data.GetData("KeymapOutputRow")).DropDone();
                }
            }
        }

        private void connection_output_border_DragLeave(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("KeymapOutputRow"))
            {
                ((KeymapOutputRow)e.Data.GetData("KeymapOutputRow")).DropLost();
            }
        }

        private void connection_output_border_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("KeymapOutput"))
            {
                KeymapOutput newOutput = (KeymapOutput)e.Data.GetData("KeymapOutput");
                if (this.input.canHandle(newOutput))
                {
                    if (e.Data.GetDataPresent("KeymapOutputRow"))
                    {
                        ((KeymapOutputRow)e.Data.GetData("KeymapOutputRow")).DropAccepted(this.connection_output_border);
                    }
                }
                else
                {
                    if (e.Data.GetDataPresent("KeymapOutputRow"))
                    {
                        ((KeymapOutputRow)e.Data.GetData("KeymapOutputRow")).DropRejected();
                    }
                }
            }
        }

        private void rClear_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Keymap defaultKeymap = KeymapDatabase.Current.getDefaultKeymap();
            this.config.Output = defaultKeymap.getConfigFor(0,this.input.Key).Output;
            this.config.Inherited = true;
            this.SetConfig(this.config);

            this.connection_output_name.Text = config.Output.Name;
            this.connection_output_border.BorderBrush = new SolidColorBrush(Colors.LightGray);
        }

    }
}
