using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WiiTUIO.Provider
{
    public partial class CursorCanvas : Canvas
    {
        private List<Cursor2> cursors;

        public CursorCanvas()
        {
            InitializeComponent();

            cursors = new List<Cursor2>();

            Timer frameCount = new Timer();
            frameCount.Interval = 1000;
            frameCount.Elapsed += frameCount_Elapsed;
            frameCount.Start();

            Timer frameRefresh = new Timer();
            frameRefresh.Interval = 10;
            frameRefresh.Elapsed += frameRefresh_Elapsed;
            frameRefresh.Start();

            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            
        }

        void frameRefresh_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(delegate()
            {
                //this.InvalidateVisual();
            }), System.Windows.Threading.DispatcherPriority.Send, null);
            //this.Dispatcher.Invoke(DispatcherPriority.Render, new Action(delegate() { }));
        }

        void frameCount_Elapsed(object sender, ElapsedEventArgs e)
        {
            Console.WriteLine("Cursor canvas FPS : " + frame);
            frame = 0;
        }

        public void AddCursor(BitmapCursor cursor)
        {
            this.Children.Add(cursor);
            //cursors.Add(cursor);
        }

        public void RemoveCursor(BitmapCursor cursor)
        {
            this.Children.Remove(cursor);
            //cursors.Remove(cursor);
        }

        int frame = 0;
        protected override void OnRender(DrawingContext drawingContext)
        {
            /*foreach (Cursor2 cursor in cursors)
            {
                cursor.Render(drawingContext);
            }*/
            base.OnRender(drawingContext);
            frame++;
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            this.InvalidateVisual();
            Console.WriteLine("Size changed");
        }

    }
}
