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

        public KeymapConnectionRow(KeymapInput input, KeymapOutput output)
        {
            InitializeComponent();
            this.input = input;
            this.output = output;
            this.connection_input_name.Text = input.Name;
            this.connection_output_name.Text = output.Name;
        }

    }
}
