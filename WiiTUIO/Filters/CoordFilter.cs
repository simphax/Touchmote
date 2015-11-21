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
        public double Dcutoff;

        OneEuroFilter xFilter;
        OneEuroFilter yFilter;

        public CoordFilter() : this(120.0, 0.02, 0.007, 2.0)
        {
        }

        public CoordFilter(double freq, double mincutoff, double beta, double dcutoff)
        {
            this.Freq = freq;
            this.Mincutoff = mincutoff;
            this.Beta = beta;
            this.Dcutoff = dcutoff;
            xFilter = new OneEuroFilter(Mincutoff, Beta, Dcutoff);
            yFilter = new OneEuroFilter(Mincutoff, Beta, Dcutoff);
        }

        public Point AddGetFilteredCoord(Point point, double width, double height)
        {
            Point unitVector = new Point();
            unitVector.X = point.X / width;
            unitVector.Y = point.Y / height;

            Point filteredVector = filterUnitVector(unitVector);

            filteredVector.X *= width;
            filteredVector.Y *= height;
            

            return filteredVector;
        }

        private Point filterUnitVector(Point vector)
        {
            Point filteredVector = new Point();
            filteredVector.X = xFilter.Filter(vector.X, 1 / Freq);
            filteredVector.Y = yFilter.Filter(vector.Y, 1 / Freq);
            return filteredVector;
        }
    }
}
