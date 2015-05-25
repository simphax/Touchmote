using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiiTUIO.Provider
{
    public class CursorPos
    {
        public int X;
        public int Y;
        public double RelativeX;
        public double RelativeY;
        public double Rotation;
        public bool OutOfReach;

        public CursorPos(int x, int y, double relativeX, double relativeY, double rotation)
        {
            this.X = x;
            this.Y = y;
            this.RelativeX = relativeX;
            this.RelativeY = relativeY;
            this.Rotation = rotation;
            this.OutOfReach = false;
        }
    }
}
