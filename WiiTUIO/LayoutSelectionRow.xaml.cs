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

namespace WiiTUIO
{
    /// <summary>
    /// Interaction logic for LayoutSelectionRow.xaml
    /// </summary>
    public partial class LayoutSelectionRow : UserControl
    {
        private string name;
        private string file;

        public Action<string> OnClick;

        public LayoutSelectionRow(string name, string file)
        {
            InitializeComponent();
            this.name = name;
            this.file = file;
            this.tbName.Text = name;
        }

        private void tbName_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (OnClick != null)
            {
                OnClick(this.file);
            }
            this.border.BorderBrush = new SolidColorBrush(Color.FromScRgb(0, 238, 238, 238));
        }

        private void tbName_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.border.BorderBrush = new SolidColorBrush(Color.FromScRgb(20,238,238,238));
        }

        private void tbName_TouchDown_1(object sender, TouchEventArgs e)
        {
            this.border.BorderBrush = new SolidColorBrush(Color.FromScRgb(20, 238, 238, 238));
        }

        private void tbName_TouchUp_1(object sender, TouchEventArgs e)
        {

        }


    }
}
