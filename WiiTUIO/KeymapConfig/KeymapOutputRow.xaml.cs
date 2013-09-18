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
        private KeymapOutput output;

        public Action<Adorner> OnDragStart;
        public Action<Adorner> OnDragStop;
        private TestAdorner adorner;

        public KeymapOutputRow(KeymapOutput output)
        {
            InitializeComponent();
            this.output = output;
            this.tbName.Text = output.Name;

            
            //this.adorner.Visibility = Visibility.Hidden;

        }

        private void border_MouseMove(object sender, MouseEventArgs e)
        {
            Border border = sender as Border;
            if (border != null && e.LeftButton == MouseButtonState.Pressed)
            {
                //if (this.adorner == null)
                //{
                    this.adorner = new TestAdorner(this);
                    this.adorner.IsHitTestVisible = false;
                //}
                //this.adorner.Visibility = Visibility.Visible;
                Console.WriteLine("Start");
                if (OnDragStart != null)
                {
                    OnDragStart(this.adorner);
                }
                DataObject data = new DataObject();
                data.SetData("KeymapOutput", this.output);
                DragDrop.DoDragDrop(border,
                                     data,
                                     DragDropEffects.Copy | DragDropEffects.Move);
                if (OnDragStop != null)
                {
                    OnDragStop(this.adorner);
                }
                
                //this.adorner.Visibility = Visibility.Hidden;
            }
        }

        protected override void OnGiveFeedback(GiveFeedbackEventArgs e)
        {
            base.OnGiveFeedback(e);

            if (this.adorner != null)
            {
                Point curpos = MouseSimulator.GetCursorPosition();
                Point point = this.PointFromScreen(curpos);
                this.adorner.SetPosition(point.X, point.Y);
            }

            Cursor cursor = ((TextBlock)this.Resources["CursorClosedHand"]).Cursor;

            Mouse.SetCursor(cursor);
            e.Handled = true;
        }
    }

    public class TestAdorner : Adorner
    {
        private Rect adornedElementRect;
        private double offsetx, offsety;
         // Be sure to call the base class constructor. 
        public TestAdorner(UIElement adornedElement)
        : base(adornedElement) 
      {
          //adornedElementRect = new Rect(adornedElement.DesiredSize);
          adornedElementRect = new Rect(0,0,140,30);
          Point curpos = MouseSimulator.GetCursorPosition();
          Point point = adornedElement.PointFromScreen(curpos);
          offsetx = point.X;
          offsety = point.Y;
      }


        private double x, y;

        public void SetPosition(double x, double y)
        {
            this.x = x;
            this.y = y;
            this.InvalidateVisual();
        }

        // A common way to implement an adorner's rendering behavior is to override the OnRender 
        // method, which is called by the layout system as part of a rendering pass. 
        protected override void OnRender(DrawingContext drawingContext)
        {
            adornedElementRect.X = this.x-offsetx;
            adornedElementRect.Y = this.y-offsety;

            VisualBrush vb = new VisualBrush(this.AdornedElement);
            vb.Opacity = 0.5;
            drawingContext.DrawRectangle(vb, null, adornedElementRect);

        }
    }
}
