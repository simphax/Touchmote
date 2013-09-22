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
    public partial class KeymapOutputRow : UserControl
    {
        public Action<Adorner> OnDragStart;
        public Action<Adorner> OnDragStop;

        public KeymapOutputRow(KeymapOutput output)
        {
            InitializeComponent();

            KeymapOutputItem item = new KeymapOutputItem(output);
            item.OnDragStart += item_OnDragStart;
            item.OnDragStop += item_OnDragStop;

            this.mainGrid.Children.Add(item);

        }


        private void item_OnDragStop(Adorner adorner)
        {
            if (OnDragStop != null)
            {
                OnDragStop(adorner);
            }
        }

        private void item_OnDragStart(Adorner adorner)
        {
            if (OnDragStart != null)
            {
                OnDragStart(adorner);
            }
        }
    }
}
