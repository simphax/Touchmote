using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WiiTUIO.Provider
{
    public class BitmapCursor : Image
    {
        // the pixel format for the image.  This one is blue-green-red-alpha 32bit format
	    private static PixelFormat PIXEL_FORMAT = PixelFormats.Bgra32;
	    // the bitmap used as a pixel source for the image
	    WriteableBitmap bitmap;
	    // the clipping bounds of the bitmap
	    Int32Rect bitmapRect;
	    // the pixel array.  unsigned ints are 32 bits
	    uint[] pixels;
        uint[] normal;
        BitmapImage cursor;
	    // the width of the bitmap.  sort of.
	    int stride;

        Point lastPosition;

        public BitmapCursor(Color color)
        {
            int width = Util.ScreenWidth;
            int height = Util.ScreenHeight;
            // set the image width
            this.Width = width;
            // set the image height
            this.Height = height;
            // define the clipping bounds
            bitmapRect = new Int32Rect(0, 0, width, height);
            // define the WriteableBitmap
            bitmap = new WriteableBitmap(width, height, 96, 96, PIXEL_FORMAT, null);
            // define the stride
            stride = (width * PIXEL_FORMAT.BitsPerPixel + 7) / 8;
            // allocate our pixel array
            pixels = new uint[width * height];
            normal = new uint[width * height];
            // set the image source to be the bitmap
            cursor = new BitmapImage(new Uri("cursor.png", UriKind.Relative));
            //img.CopyPixels(new Int32Rect(0, 0, 80, 80), pixels, stride, 0);
            //img.CopyPixels(new Int32Rect(0, 0, 80, 80), normal, stride, 0);
            //normal = new uint[80*80];
            //img.CopyPixels(new Int32Rect(0, 0, 80, 80), normal, (80 * PIXEL_FORMAT.BitsPerPixel + 7) / 8, 0);
            

            bitmap.WritePixels(bitmapRect, pixels, stride, 0);
            
            this.Source = bitmap;
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
            //Canvas.SetLeft(this, point.X - 40);
            //Canvas.SetTop(this, point.Y - 40);
            //this.RenderTransform = new TranslateTransform() { X = point.X, Y = point.Y };
            //bitmap.Lock();
            int position;
            int offset = 0;
            
            if (lastPosition != null)
            {
                
                position = (int)lastPosition.Y * 2560 + (int)lastPosition.X;
                /*for (int x = 0; x < 80; x++)
                {
                    for (int y = 0; y < 80; y++)
                    {
                        offset = y * 2560 + x;
                        if (position + offset < pixels.Count()-1)
                        {
                        pixels[position + offset] = 0;
                        }
                    }
                }*/
                if ((int)lastPosition.X + 80 < 2560 && (int)lastPosition.Y + 80 < 1440)
                {
                    bitmap.WritePixels(new Int32Rect((int)lastPosition.X, (int)lastPosition.Y, 80, 80), new uint[2560*1440], stride, 0);
                }
            }
            
            //Array.Clear(pixels,0,pixels.Length);

            //pixels = new uint[2560 * 1440];
            /*
            if (lastPosition != null)
            {
                bitmap.WritePixels(new Int32Rect((int)lastPosition.X, (int)lastPosition.Y, 80, 80), pixels, stride, 0);
            }
            */
            lastPosition = point;

            position = (int)point.Y * 2560 + (int)point.X;
            /*
            for (int x = 0; x < 80; x++)
            {
                for(int y = 0; y < 80; y++)
                {
                    offset = y * 2560 + x;
                    if (position + offset < pixels.Length - 1)
                    {
                        pixels[position + offset] = normal[offset];
                    }
                }
            }
            */
            
            if ((int)point.X + 80 < 2560 && (int)point.Y + 80 < 1440)
            {
                cursor.CopyPixels(pixels, stride, position);
                bitmap.WritePixels(new Int32Rect((int)point.X, (int)point.Y, 80, 80), pixels, stride, position);
            }
            //bitmap.AddDirtyRect(bitmapRect);

            //bitmap.Unlock();
            //this.InvalidateVisual();
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
