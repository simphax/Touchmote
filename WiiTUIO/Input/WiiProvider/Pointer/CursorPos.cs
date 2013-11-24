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
        public double Rotation;
        public bool OutOfReach;

        public CursorPos(int x, int y, double rotation)
        {
            this.X = x;
            this.Y = y;
            this.Rotation = rotation;
            this.OutOfReach = false;
        }
    }
}
