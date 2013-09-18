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
        private KeymapOutput output;

        public Action<bool> OnDrop;

        public KeymapConnectionRow(KeymapInput input, KeymapOutput output)
        {
            InitializeComponent();
            this.input = input;
            this.output = output;
            this.connection_input_name.Text = input.Name;
            this.connection_output_name.Text = output.Name;
        }

        private void setOutput(KeymapOutput output)
        {
            this.output = output;
            this.connection_output_name.Text = output.Name;
        }

        private void connection_output_border_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("KeymapOutput"))
            {
                KeymapOutput newOutput = (KeymapOutput)e.Data.GetData("KeymapOutput");
                if (this.input.canHandle(newOutput))
                {
                    this.connection_output_border.BorderBrush = new SolidColorBrush(Colors.Black);
                    this.setOutput(newOutput);
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
            }
        }

        private void connection_output_border_DragLeave(object sender, DragEventArgs e)
        {
            this.connection_output_border.BorderBrush = new SolidColorBrush(Colors.Black);
        }

        private void connection_output_border_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("KeymapOutput"))
            {
                KeymapOutput newOutput = (KeymapOutput)e.Data.GetData("KeymapOutput");
                if (this.input.canHandle(newOutput))
                {
                    this.connection_output_border.BorderBrush = new SolidColorBrush(Colors.Green);
                }
                else
                {
                    this.connection_output_border.BorderBrush = new SolidColorBrush(Colors.Red);
                }
            }
        }

    }
}
