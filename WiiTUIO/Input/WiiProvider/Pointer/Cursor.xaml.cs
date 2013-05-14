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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WiiTUIO.Provider
{
    /// <summary>
    /// Interaction logic for Cursor.xaml
    /// </summary>
    public partial class Cursor : Grid
    {
        public static double CANVAS_WIDTH = 80;

        public Cursor(Color color)
        {
            InitializeComponent();
            this.stroke.Stroke = new SolidColorBrush(color);
        }

        public void SetRotation(double rotation)
        {
            Dispatcher.BeginInvoke(new Action(delegate()
            {
                this.rotationIndicator.RenderTransform = new RotateTransform(this.radianToDegree(rotation));
            }), null);
        }

        public void SetPosition(Point point)
        {
            Dispatcher.BeginInvoke(new Action(delegate()
            {
                this.cursor.SetValue(Canvas.LeftProperty, point.X - (CANVAS_WIDTH/2));
                this.cursor.SetValue(Canvas.TopProperty, point.Y - (CANVAS_WIDTH/2));
            }), null);
        }

        public void Hide()
        {
            this.cursor.Visibility = Visibility.Hidden;
        }

        public void Show()
        {
            this.cursor.Visibility = Visibility.Visible;
        }

        public void TouchDown()
        {
            Dispatcher.BeginInvoke(new Action(delegate()
            {
                DoubleAnimation animation = createDoubleAnimation(20, 200, false);
                animation.FillBehavior = FillBehavior.HoldEnd;
                animation.Completed += delegate(object sender, EventArgs pEvent)
                {

                };
                this.innerEllipse.BeginAnimation(FrameworkElement.WidthProperty, animation, HandoffBehavior.SnapshotAndReplace);
                this.innerEllipse.BeginAnimation(FrameworkElement.HeightProperty, animation, HandoffBehavior.SnapshotAndReplace);

                DoubleAnimation animation2 = createDoubleAnimation(40, 200, false);
                animation2.FillBehavior = FillBehavior.HoldEnd;
                animation2.Completed += delegate(object sender, EventArgs pEvent)
                {

                };
                this.outerEllipse.BeginAnimation(FrameworkElement.WidthProperty, animation2, HandoffBehavior.SnapshotAndReplace);
                this.outerEllipse.BeginAnimation(FrameworkElement.HeightProperty, animation2, HandoffBehavior.SnapshotAndReplace);

                DoubleAnimation animation3 = createDoubleAnimation(46, 200, false);
                animation3.FillBehavior = FillBehavior.HoldEnd;
                animation3.Completed += delegate(object sender, EventArgs pEvent)
                {

                };
                this.stroke.BeginAnimation(FrameworkElement.WidthProperty, animation3, HandoffBehavior.SnapshotAndReplace);
                this.stroke.BeginAnimation(FrameworkElement.HeightProperty, animation3, HandoffBehavior.SnapshotAndReplace);
            }), null);
        }

        public void TouchUp()
        {
            Dispatcher.BeginInvoke(new Action(delegate()
            {
                DoubleAnimation animation = createDoubleAnimation(40, 200, false);
                animation.FillBehavior = FillBehavior.HoldEnd;
                animation.Completed += delegate(object sender, EventArgs pEvent)
                {

                };
                this.innerEllipse.BeginAnimation(FrameworkElement.WidthProperty, animation, HandoffBehavior.SnapshotAndReplace);
                this.innerEllipse.BeginAnimation(FrameworkElement.HeightProperty, animation, HandoffBehavior.SnapshotAndReplace);

                DoubleAnimation animation2 = createDoubleAnimation(50, 200, false);
                animation2.FillBehavior = FillBehavior.HoldEnd;
                animation2.Completed += delegate(object sender, EventArgs pEvent)
                {

                };
                this.outerEllipse.BeginAnimation(FrameworkElement.WidthProperty, animation2, HandoffBehavior.SnapshotAndReplace);
                this.outerEllipse.BeginAnimation(FrameworkElement.HeightProperty, animation2, HandoffBehavior.SnapshotAndReplace);
                
                DoubleAnimation animation3 = createDoubleAnimation(56, 200, false);
                animation3.FillBehavior = FillBehavior.HoldEnd;
                animation3.Completed += delegate(object sender, EventArgs pEvent)
                {

                };
                this.stroke.BeginAnimation(FrameworkElement.WidthProperty, animation3, HandoffBehavior.SnapshotAndReplace);
                this.stroke.BeginAnimation(FrameworkElement.HeightProperty, animation3, HandoffBehavior.SnapshotAndReplace);
            }), null);
        }

        private static DoubleAnimation createDoubleAnimation(double fNew, double fTime, bool bFreeze)
        {
            // Create the animation.
            DoubleAnimation pAction = new DoubleAnimation(fNew, new Duration(TimeSpan.FromMilliseconds(fTime)))
            {
                // Specify settings.
                AccelerationRatio = 0.1,
                DecelerationRatio = 0.9,
                FillBehavior = FillBehavior.HoldEnd
            };

            // Pause the action before starting it and then return it.
            if (bFreeze)
                pAction.Freeze();
            return pAction;
        }

        private double radianToDegree(double angle)
        {
            return angle * (180.0 / Math.PI);
        }
    }
}
