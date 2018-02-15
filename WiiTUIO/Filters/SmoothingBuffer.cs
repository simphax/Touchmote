using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WiiTUIO.Properties;

namespace WiiTUIO.Filters
{
    /// <summary>
    /// A SmoothingBuffer class is a helper around a smoothing array.  It implements a linear smoothing by taking the average over the
    /// number of items pushed into the buffer.  The oldest items are replaced by the newest ones.
    /// </summary>
    public class SmoothingBuffer {
        /// <summary>
        /// A reference to an array which contains our smoothing buffer data.
        /// </summary>
        public Vector[] tSmoothBuffer;

        /// <summary>
        /// A reference to the front of the buffer.
        /// </summary>
        public int iSmoothIndex = 0;

        /// <summary>
        /// Create a new smoothing buffer with a default size.
        /// </summary>
        /// <param name="iSmoothSize">The number of elements of smoothing to use.</param>
        public SmoothingBuffer(int iSmoothSize) {
            // Check the smooth size is valid.
            if (iSmoothSize <= 0)
                throw new ArgumentOutOfRangeException("Cannot have a smooth size that is <= 0.");

            // Create a new smoothing buffer.
            this.tSmoothBuffer = new Vector[iSmoothSize];
            this.iSmoothIndex = 0;
        }

        /// <summary>
        /// Return the length of the smoothing buffer.
        /// </summary>
        /// <returns>An integer which describes the length of the smoothing buffer.</returns>
        public int getSmoothSize() {
            return this.tSmoothBuffer.Length;
        }

        /// <summary>
        /// This method will expand or contract the smoothing array with an option to preserve smoothing data
        /// as best as it can (naturally if we are contracting then we don't take the time to compute a similar distribution)
        /// we just truncate the data.  Sorry for being lazy! lol
        /// </summary>
        /// <param name="iSmoothSize">The new size of the smoothing buffer.</param>
        /// <param name="bPreserve">True to preseve data, False for not.</param>
        public void resize(int iSmoothSize, bool bPreserve) {
            // Argument checking.
            if (iSmoothSize <= 0)
                throw new ArgumentOutOfRangeException("The smooth size cannot be <= 0.");

            // Create a new array.
            Vector[] tNewBuffer = new Vector[iSmoothIndex];

            // If we want to preserve the data we already have.. (or as much as we can).
            int iMax = Math.Min(iSmoothIndex, tSmoothBuffer.Length);
            if (bPreserve && tSmoothBuffer != null) {
                iMax = Math.Min(iSmoothSize, iMax);
                if (iMax != 0) {
                    for (int i = 0; i < iMax; ++i)
                        tNewBuffer[i] = tSmoothBuffer[i];
                }
            }

            // Swap the pointers.
            this.tSmoothBuffer = tNewBuffer;
            this.iSmoothIndex = iMax;
        }

        /// <summary>
        /// Clear the information in this smoothing buffer.
        /// </summary>
        public void clear() {
            this.iSmoothIndex = 0;
        }

        private Vector current_pos = new Vector();
        private double Radius = Settings.Default.pointer_positionRadius;
        /// <summary>
        /// Insert a value into this buffer for smoothing.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        public void addValue(double x, double y) {
            // Insert the value then update the counter.
            int iIndex = iSmoothIndex % tSmoothBuffer.Length;
            tSmoothBuffer[iIndex].X = x;
            tSmoothBuffer[iIndex].Y = y;
            ++iSmoothIndex;
        }

        /// <summary>
        /// Insert a value into this buffer for smoothing.
        /// </summary>
        /// <param name="tPoint">A pointer to the point structure.</param>
        public void addValue(Vector vPoint) {
            addValue(vPoint.X, vPoint.Y);
        }

        public void replaceLast(Vector vPoint) {
            // Insert the value then update the counter.
            int lastIndex = (iSmoothIndex - 1) % tSmoothBuffer.Length;
            if (lastIndex < 0) {
                lastIndex = tSmoothBuffer.Length - 1;
            }

            tSmoothBuffer[lastIndex] = vPoint;
        }

        /// <summary>
        /// Calculate and return the smoothed value.
        /// Note that this function is worst case O(n) where n is the size of the smoothing buffer.
        /// </summary>
        /// <returns></returns>
        public Vector getSmoothedValue() {

            // Get the number of values to iterate too.
            Vector tSmooth = new Vector(0, 0);
            int iMax = Math.Min(iSmoothIndex, tSmoothBuffer.Length);
            if (iMax == 0)
                throw new Exception("No values in the smoothing buffer!");
            /*
            Vector curVector = tSmoothBuffer[iMax-1];

            if(iMax >= 5) 
            {

                double deltaX = Math.Abs(curVector.X*4 - tSmoothBuffer[iMax-2].X - tSmoothBuffer[iMax-3].X - tSmoothBuffer[iMax-4].X - tSmoothBuffer[iMax-5].X);
                Console.WriteLine("DeltaX: "+deltaX);
                if(deltaX < 200)
                {
                    tSmoothBuffer[iMax - 1] = tSmoothBuffer[iMax - 2];
                    return tSmoothBuffer[iMax-2];
                }
            }
            */
            // Sum up the values in the array.
            for (int i = 0; i < iMax; ++i) {
                tSmooth += tSmoothBuffer[i];
            }

            // Divide to average.
            tSmooth /= iMax;

            // Return the value.
            return tSmooth;
        }
    }
}
