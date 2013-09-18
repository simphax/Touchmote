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

        public Action<bool> OnDrop;

        public KeymapConnectionRow(KeymapInput input, KeymapOutConfig config)
        {
            InitializeComponent();
            this.input = input;
            this.config = config;
            this.connection_input_name.Text = input.Name;
            this.connection_output_name.Text = config.Output.Name;
            this.connection_output_border.BorderBrush = new SolidColorBrush(KeymapColors.GetColor(config.Output.Type));

            if (config.Inherited)
            {
                this.connection_output_border.BorderBrush = new SolidColorBrush(Colors.LightGray);
            }
        }

        private void setOutput(KeymapOutput output)
        {
            this.config.Output = output;
            this.connection_output_name.Text = output.Name;
        }

        private void connection_output_border_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("KeymapOutput"))
            {
                KeymapOutput newOutput = (KeymapOutput)e.Data.GetData("KeymapOutput");
                if (this.input.canHandle(newOutput))
                {
                    this.connection_output_border.BorderBrush = new SolidColorBrush(KeymapColors.GetColor(newOutput.Type));
                    this.setOutput(newOutput);
                }
                if (e.Data.GetDataPresent("KeymapOutputRow"))
                {
                    ((KeymapOutputRow)e.Data.GetData("KeymapOutputRow")).DropDone();
                }
                if (OnDrop != null)
                {
                    OnDrop(this.input.canHandle(newOutput));
                }
            }
            else
            {
                if (OnDrop != null)
                {
                    OnDrop(false);
                }
                if (e.Data.GetDataPresent("KeymapOutputRow"))
                {
                    ((KeymapOutputRow)e.Data.GetData("KeymapOutputRow")).DropDone();
                }
            }
        }

        private void connection_output_border_DragLeave(object sender, DragEventArgs e)
        {
            //this.connection_output_border.BorderBrush = new SolidColorBrush(Colors.Orange);
            //this.connection_output_border.Background = new SolidColorBrush(Colors.White);
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
                    //this.connection_output_border.BorderBrush = new SolidColorBrush(Colors.Orange);
                    //this.connection_output_border.Background = new SolidColorBrush(Colors.Orange);
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

    }
}
