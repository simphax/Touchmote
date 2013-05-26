using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace WiiTUIO.Provider
{
    public class Cursor2
    {
        private Point position;
        private SolidColorBrush brush;
        private EllipseGeometry ellipse;

        public Cursor2(Color color)
        {
            this.brush = new SolidColorBrush(color);
            this.brush.Freeze();

            this.ellipse = new EllipseGeometry();
            this.ellipse.RadiusX = 40;
            this.ellipse.RadiusY = 40;
        }

        public void Render(DrawingContext dc)
        {
            dc.DrawGeometry(this.brush, null, this.ellipse);
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
            this.ellipse.Center = point;
            //this.ellipse.Transform = new TranslateTransform() { X = point.X, Y = point.Y };
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
