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

        public Cursor2(Color color)
        {
            this.brush = new SolidColorBrush(color);
        }

        public void Render(DrawingContext dc)
        {
            dc.DrawEllipse(brush, new Pen(), position, 80, 80);
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
            this.position = point;
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
