using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WiiTUIO.Provider
{
    public class Cursor2 : Canvas
    {
        private Point position;
        private static SolidColorBrush brush;
        private static EllipseGeometry ellipse;
        private TranslateTransform transform;

        public Cursor2(Color color)
        {
            this.Width = 80;
            this.Height = 80;

            brush = new SolidColorBrush(color);
            brush.Freeze();

            ellipse = new EllipseGeometry();
            ellipse.RadiusX = 40;
            ellipse.RadiusY = 40;
            ellipse.Freeze();

            this.transform = new TranslateTransform() { X = 0, Y = 0 };

        }
        protected override void OnRender(DrawingContext dc)
        {
            dc.DrawGeometry(brush, null, ellipse);
        }

        public void Render(DrawingContext dc)
        {
            //dc.PushTransform(this.transform);
            dc.DrawGeometry(brush, null, ellipse);
            //dc.Pop();
            //dc.DrawEllipse(brush, null, position, 40, 40);
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
            //this.position = point;
            //this.ellipse.Center = point;
            //this.ellipse.Transform = new TranslateTransform() { X = point.X, Y = point.Y };
            //this.transform.X = point.X;
            //this.transform.Y = point.Y;
            Canvas.SetLeft(this,point.X-40);
            Canvas.SetTop(this,point.Y-40);
        }

        public void Hide()
        {
            
        }

        public void Show()
        {
            
        }

        public void SetPressed()
        {
            
        }
        public void SetReleased()
        {
            
        }
    }
}
