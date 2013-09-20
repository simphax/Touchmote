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
    public partial class KeymapRow : UserControl
    {
        private Keymap keymap;

        private SolidColorBrush defaultBrush = new SolidColorBrush(Color.FromRgb(46, 46, 46));
        private SolidColorBrush highlightBrush = new SolidColorBrush(Color.FromRgb(65, 177, 225));

        public Action<Keymap> OnClick; //filename

        public KeymapRow(Keymap keymap, bool active, bool defaultk)
        {
            InitializeComponent();
            this.keymap = keymap;
            this.tbName.Text = keymap.getName();

            if (active)
            {
                this.border.Background = highlightBrush;
            }
            else
            {
                this.border.MouseUp += border_MouseUp;
                this.border.Cursor = Cursors.Hand;
            }
            if (defaultk)
            {
                this.tbDefault.Visibility = Visibility.Visible;
            }

        }

        private void border_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (OnClick != null)
            {
                OnClick(this.keymap);
            }
        }



    }
}
