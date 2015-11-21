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
        
        OneEuroFilter xFilter;
        OneEuroFilter yFilter;

        public CoordFilter() : this(90.0, 0.003, 0.001)
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
