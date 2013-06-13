using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace WiiTUIO.Provider
{
    public class D3DCursor
    {
        public int X, Y, ID;
        public double Rotation;
        public bool Hidden, Pressed;
        public Color Color;

        public D3DCursor(int id, Color color)
        {
            Color = color;
            ID = id;
            X = 0;
            Y = 0;
            Rotation = 0;
            Hidden = false;
            Pressed = false;
        }

        public void Hide()
        {
            this.Hidden = true;
        }

        public void Show()
        {
            this.Hidden = false;
        }

        public void SetPosition(Point point)
        {
            this.X = (int)point.X;
            this.Y = (int)point.Y;
        }

        public void SetReleased()
        {
            this.Pressed = false;
        }

        public void SetPressed()
        {
            this.Pressed = true;
        }

        public void SetRotation(double rotation)
        {
            this.Rotation = rotation;
        }
    }
}
