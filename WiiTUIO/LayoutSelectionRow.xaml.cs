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
    public partial class LayoutSelectionRow : UserControl
    {
        private string name;
        private string file;
        private bool selected = false;

        public Action<string> OnClick; //filename

        private Color borderColor;

        public LayoutSelectionRow(string name, string file, Color borderColor)
        {
            InitializeComponent();
            this.name = name;
            this.file = file;
            this.tbName.Text = name;
            this.borderColor = borderColor;

            setSelected(false);
        }

        public string getFilename()
        {
            return file;
        }

        public bool isSelected()
        {
            return this.selected;
        }

        public void setSelected(bool selected)
        {
            this.selected = selected;
            if(selected)
            {
                this.border.Background = new SolidColorBrush(this.borderColor);//Color.FromArgb(0xEE, 0x33, 0x33, 0x33));
                this.border.BorderBrush = new SolidColorBrush(Color.FromArgb(0xEE, 0x33, 0x33, 0x33));
            }
            else
            {
                this.border.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x33, 0x33, 0x33));
                this.border.BorderBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x66, 0x66, 0x66));
            }
        }

        private void tbName_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (OnClick != null)
            {
                OnClick(this.file);
            }
        }
        /*
        private void tbName_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.setSelected(true);
        }
        */
    }
}
