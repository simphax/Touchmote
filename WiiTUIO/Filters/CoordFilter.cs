using System;
using System.Collections.Generic;

using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WiiTUIO.Filters
{
    public class CoordFilter
    {
        public double Freq;
        public double Mincutoff;
        public double Beta;

        int frameCount = 0;

        OneEuroFilter xFilter;
        OneEuroFilter yFilter;

        public CoordFilter() : this(100.0, 1.0, 0.005)
        {
        }

        public CoordFilter(double freq, double mincutoff, double beta)
        {
            this.Freq = freq;
            this.Mincutoff = mincutoff;
            this.Beta = beta;
            xFilter = new OneEuroFilter(Mincutoff, Beta);
            yFilter = new OneEuroFilter(Mincutoff, Beta);
        }

        public Point AddGetFilteredCoord(Point point, double width, double height)
        {
            Point unitVector = new Point();
            unitVector.X = point.X / width;
            unitVector.Y = point.Y / height;

            Point filteredVector = filterUnitVector(unitVector);

            filteredVector.X *= width;
            filteredVector.Y *= height;

            frameCount++;

            return filteredVector;
        }

        private Point filterUnitVector(Point vector)
        {
            Point filteredVector = new Point();
            filteredVector.X = xFilter.Filter(vector.X, frameCount / Freq);
            filteredVector.Y = yFilter.Filter(vector.Y, frameCount / Freq);
            return filteredVector;
        }
    }
}
