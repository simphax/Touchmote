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
        private const double CANVAS_HALF_WIDTH = 40; //80/2
        public bool hidden = false;
        public bool pressed = false;

        private static Brush innerBrush = new SolidColorBrush(Color.FromScRgb(0.5f, 1, 1, 1));
        private static Brush outerBrush = new SolidColorBrush(Color.FromScRgb(0.4f, 0, 0, 0));

        public Cursor(Color color)
        {
            InitializeComponent();

            innerBrush.Freeze();
            this.innerEllipse.Fill = innerBrush;

            outerBrush.Freeze();
            this.outerEllipse.Fill = outerBrush;

            color.ScA = 0.5f;
            Brush strokeBrush = new SolidColorBrush(color);
            strokeBrush.Freeze();
            this.stroke.Stroke = strokeBrush;
            //this.cursor.RenderTransform = new ScaleTransform();
        }

        public void SetRotation(double rotation)
        {
            /*
            Dispatcher.BeginInvoke(new Action(delegate()
            {
                this.rotationIndicator.RenderTransform = new RotateTransform(this.radianToDegree(rotation));
            }), null);
            */
        }

        public void SetPosition(Point point)
        {
            Dispatcher.BeginInvoke(new Action(delegate()
            {
                //this.SetValue(Canvas.LeftProperty, point.X - CANVAS_HALF_WIDTH);
                //this.SetValue(Canvas.TopProperty, point.Y - CANVAS_HALF_WIDTH);
                //this.RenderTransform = new TranslateTransform() { X = point.X - CANVAS_HALF_WIDTH, Y = point.Y - CANVAS_HALF_WIDTH };
                Canvas.SetLeft(this, point.X - CANVAS_HALF_WIDTH);
                Canvas.SetTop(this, point.Y - CANVAS_HALF_WIDTH);
            }),System.Windows.Threading.DispatcherPriority.Send, null);
        }

        public void Hide()
        {
            if (!hidden)
            {
                this.hidden = true;
                Dispatcher.BeginInvoke(new Action(delegate()
                {
                    DoubleAnimation animation = UIHelpers.createDoubleAnimation(0, 200, false);
                    animation.FillBehavior = FillBehavior.HoldEnd;
                    animation.Completed += delegate(object sender, EventArgs pEvent)
                    {
                    };
                    this.innerEllipse.BeginAnimation(FrameworkElement.WidthProperty, animation, HandoffBehavior.SnapshotAndReplace);
                    this.innerEllipse.BeginAnimation(FrameworkElement.HeightProperty, animation, HandoffBehavior.SnapshotAndReplace);
                    this.outerEllipse.BeginAnimation(FrameworkElement.WidthProperty, animation, HandoffBehavior.SnapshotAndReplace);
                    this.outerEllipse.BeginAnimation(FrameworkElement.HeightProperty, animation, HandoffBehavior.SnapshotAndReplace);
                    this.stroke.BeginAnimation(FrameworkElement.WidthProperty, animation, HandoffBehavior.SnapshotAndReplace);
                    this.stroke.BeginAnimation(FrameworkElement.HeightProperty, animation, HandoffBehavior.SnapshotAndReplace);
                }), null);
            }
        }

        public void Show()
        {
            if (hidden)
            {
                this.hidden = false;
                Dispatcher.BeginInvoke(new Action(delegate()
                {
                    if (this.pressed)
                        this.animatePressed();
                    else
                        this.animateReleased();

                }), null);
            }
        }

        public void SetPressed()
        {
            if (!pressed)
            {
                this.pressed = true;
                this.animatePressed();
                
            }
        }

        private void animatePressed()
        {
            if (!hidden)
            {
                Dispatcher.BeginInvoke(new Action(delegate()
                {
                    DoubleAnimation animation = UIHelpers.createDoubleAnimation(20, 200, false);
                    animation.FillBehavior = FillBehavior.HoldEnd;
                    animation.Completed += delegate(object sender, EventArgs pEvent)
                    {

                    };
                    this.innerEllipse.BeginAnimation(FrameworkElement.WidthProperty, animation, HandoffBehavior.SnapshotAndReplace);
                    this.innerEllipse.BeginAnimation(FrameworkElement.HeightProperty, animation, HandoffBehavior.SnapshotAndReplace);

                    DoubleAnimation animation2 = UIHelpers.createDoubleAnimation(40, 200, false);
                    animation2.FillBehavior = FillBehavior.HoldEnd;
                    animation2.Completed += delegate(object sender, EventArgs pEvent)
                    {

                    };
                    this.outerEllipse.BeginAnimation(FrameworkElement.WidthProperty, animation2, HandoffBehavior.SnapshotAndReplace);
                    this.outerEllipse.BeginAnimation(FrameworkElement.HeightProperty, animation2, HandoffBehavior.SnapshotAndReplace);

                    DoubleAnimation animation3 = UIHelpers.createDoubleAnimation(46, 200, false);
                    animation3.FillBehavior = FillBehavior.HoldEnd;
                    animation3.Completed += delegate(object sender, EventArgs pEvent)
                    {

                    };
                    this.stroke.BeginAnimation(FrameworkElement.WidthProperty, animation3, HandoffBehavior.SnapshotAndReplace);
                    this.stroke.BeginAnimation(FrameworkElement.HeightProperty, animation3, HandoffBehavior.SnapshotAndReplace);
                }), null);
            }
        }

        public void SetReleased()
        {
            if (pressed)
            {
                this.pressed = false;
                this.animateReleased();
            }
        }

        private void animateReleased()
        {
            if (!hidden)
            {
                Dispatcher.BeginInvoke(new Action(delegate()
                {
                    DoubleAnimation animation = UIHelpers.createDoubleAnimation(40, 200, false);
                    animation.FillBehavior = FillBehavior.HoldEnd;
                    animation.Completed += delegate(object sender, EventArgs pEvent)
                    {

                    };
                    this.innerEllipse.BeginAnimation(FrameworkElement.WidthProperty, animation, HandoffBehavior.SnapshotAndReplace);
                    this.innerEllipse.BeginAnimation(FrameworkElement.HeightProperty, animation, HandoffBehavior.SnapshotAndReplace);

                    DoubleAnimation animation2 = UIHelpers.createDoubleAnimation(50, 200, false);
                    animation2.FillBehavior = FillBehavior.HoldEnd;
                    animation2.Completed += delegate(object sender, EventArgs pEvent)
                    {

                    };
                    this.outerEllipse.BeginAnimation(FrameworkElement.WidthProperty, animation2, HandoffBehavior.SnapshotAndReplace);
                    this.outerEllipse.BeginAnimation(FrameworkElement.HeightProperty, animation2, HandoffBehavior.SnapshotAndReplace);

                    DoubleAnimation animation3 = UIHelpers.createDoubleAnimation(56, 200, false);
                    animation3.FillBehavior = FillBehavior.HoldEnd;
                    animation3.Completed += delegate(object sender, EventArgs pEvent)
                    {

                    };
                    this.stroke.BeginAnimation(FrameworkElement.WidthProperty, animation3, HandoffBehavior.SnapshotAndReplace);
                    this.stroke.BeginAnimation(FrameworkElement.HeightProperty, animation3, HandoffBehavior.SnapshotAndReplace);
                }), null);
            }
        }

        private double radianToDegree(double angle)
        {
            return angle * (180.0 / Math.PI);
        }
    }
}
