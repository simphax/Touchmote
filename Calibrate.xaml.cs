using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using WiiTUIO.Provider;

namespace WiiTUIO
{
    /// <summary>
    /// A Canvas which we can use to calibrate the Wiimote.
    /// </summary>
    public partial class Calibrate : UserControl
    {
        /// <summary>
        /// A reference to the WiiProvider since we need to enable/disable a few things such as raw data-access for calibration.
        /// </summary>
        private WiiProvider pWiiProvider = null;

        /// <summary>
        /// The calibration margin which defines how far away from the edges the hit spot is placed.
        /// </summary>
        private double fCalibrationMargin = 0.1;

        /// <summary>
        /// The source rectangle to transform from.
        /// </summary>
        private WiiProvider.CalibrationRectangle pSourceRectangle = null;

        /// <summary>
        /// The destination rectangle to transform too.
        /// </summary>
        private WiiProvider.CalibrationRectangle pDestinationRectangle = null;

        /// <summary>
        /// A helper varaible which stores which phase of the calibration we are in,
        /// </summary>
        private int iCalibrationPhase = 0;

        private EventHandler<FrameEventArgs> pEventHandler;

        /// <summary>
        /// An event which is raised once calibration is finished.
        /// </summary>
        public event Action<WiiProvider.CalibrationRectangle, WiiProvider.CalibrationRectangle, Vector> OnCalibrationFinished;

        /// <summary>
        /// A boolean which we can use to figure out if we are calibrating or not.
        /// </summary>
        public bool IsCalibrating { get { return this.iCalibrationPhase != 0; } }

        /// <summary>
        /// Called to create a new calibration control.
        /// </summary>
        public Calibrate()
        {
            // Load the component from the XAML.
            InitializeComponent();

            // Add the touch references.
            //this.AddHandler(TouchDelegate.NewTouchContactEvent, new TouchContactEventHandler(onTouch));

            // Hide the calibration point.
            this.CalibrationPoint.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Method to begin calibration.
        /// </summary>
        /// <param name="pWiiProvider">A reference to the input provider we want to calibrate.</param>
        public void beginCalibration(WiiProvider pWiiProvider)
        {
            // Store a reference to the WiiInput provider.
            this.pWiiProvider = pWiiProvider;

            // Die if we have no provider.
            if (this.pWiiProvider == null)
                throw new Exception("Cannot begin calibrate without an input provider!");

            // Wipe the rectangles.
            this.pSourceRectangle = new WiiProvider.CalibrationRectangle();
            this.pDestinationRectangle = new WiiProvider.CalibrationRectangle();

            // Disable the transformation step in the provider.
            pWiiProvider.TransformResults = false;

            // Give us an event for the input.
            pEventHandler = new EventHandler<FrameEventArgs>(pWiiProvider_OnNewFrame);
            pWiiProvider.OnNewFrame += pEventHandler;

            // Set our calibration phase to 1.
            this.iCalibrationPhase = 1;

            // Show the calibration point.
            this.CalibrationPoint.Visibility = Visibility.Visible;

            // Ensure the form is at the top, the correct size and visible.
            Canvas.SetTop(this, 0.0);
            Canvas.SetLeft(this, 0.0);
            Canvas.SetZIndex(this, 999);
            this.Width = (double)this.Parent.GetValue(Canvas.ActualWidthProperty);
            this.Height = (double)this.Parent.GetValue(Canvas.ActualHeightProperty);

            // Step into the calibration.
            this.movePoint(0.1, 0.1);
            this.stepCalibration();
        }

        void pWiiProvider_OnNewFrame(object sender, FrameEventArgs e)
        {
            List<WiiContact> lContacts = new List<WiiContact>(e.Contacts);
            if (lContacts.Count > 0)
            {
                // Get the contact.
                WiiContact pContact = lContacts[0];

                // If it is a down contact.
                if (pContact.Type != ContactType.Start)
                    return;

                // Reference the touch contact from the event.
                Vector vPoint = new Vector(pContact.Position.X, pContact.Position.Y);

                // Select what to do based on the calibration phase.
                switch (iCalibrationPhase)
                {
                    case 1:
                        // Get the point and update the rectangle, step the calibration phase over and then break out.
                        pSourceRectangle.TopLeft = vPoint;
                        iCalibrationPhase = 2;
                        this.stepCalibration();
                        break;
                    case 2:
                        // Get the point and update the rectangle, step the calibration phase over and then break out.
                        pSourceRectangle.TopRight = vPoint;
                        iCalibrationPhase = 3;
                        this.stepCalibration();
                        break;
                    case 3:
                        // Get the point and update the rectangle, step the calibration phase over and then break out.
                        pSourceRectangle.BottomLeft = vPoint;
                        iCalibrationPhase = 4;
                        this.stepCalibration();
                        break;
                    case 4:
                        // Get the point and update the rectangle, step the calibration phase over and then break out.
                        pSourceRectangle.BottomRight = vPoint;
                        iCalibrationPhase = 5;
                        this.stepCalibration();
                        break;
                    default:
                        throw new Exception("Unknown calibration phase '" + iCalibrationPhase + "' to handle this touch event!");
                }
            }
        }

        /// <summary>
        /// This is called internally when calibration is finished.
        /// </summary>
        private void finishedCalibration()
        {
            // Die if we have no provider.
            if (this.pWiiProvider == null)
                throw new Exception("Cannot finish calibration without an input provider!");

            // Re-enable the transformation step in the provider.
            pWiiProvider.TransformResults = true;

            // Feed that data into the provider.
            Vector vScreenSize = new Vector(this.ActualWidth, this.ActualHeight);
            this.pWiiProvider.setCalibrationData(pSourceRectangle, pDestinationRectangle, vScreenSize);

            // Detach the event handler.
            this.pWiiProvider.OnNewFrame -= pEventHandler;

            // Dereference the provider.
            this.pWiiProvider = null;

            // Hide the calibration point.
            Dispatcher.BeginInvoke((Action)delegate()
            {
                // Hide the calibration point.
                this.CalibrationPoint.Visibility = Visibility.Hidden;

                // Ensure the form is at the back, the correct size and visible.
                Canvas.SetTop(this, 0.0);
                Canvas.SetLeft(this, 0.0);
                Canvas.SetZIndex(this, -999);
                this.Width = 0;// (double)this.Parent.GetValue(Canvas.ActualWidthProperty);
                this.Height = 0;// (double)this.Parent.GetValue(Canvas.ActualHeightProperty);

                // Raise the event.
                if (OnCalibrationFinished != null)
                    OnCalibrationFinished(pSourceRectangle, pDestinationRectangle, vScreenSize);
            });
        }

        /// <summary>
        /// This is called to step the calibration phase (i.e. it updates the destination rectangle for the next input).
        /// 
        /// </summary>
        private void stepCalibration()
        {
            // Define a point to contain the calibration destination transformed into window space.
            Vector tPoint = new Vector(0, 0);

            // Select what to do based on which phase we are in.
            switch (iCalibrationPhase)
            {
                case 1:
                    // Get the point and update the rectangle, step the calibration phase over and then break out.
                    pDestinationRectangle.TopLeft = this.movePoint(fCalibrationMargin, fCalibrationMargin);
                    break;
                case 2:
                    // Get the point and update the rectangle, step the calibration phase over and then break out.
                    pDestinationRectangle.TopRight = this.movePoint(1.0 - fCalibrationMargin, fCalibrationMargin);
                    break;
                case 3:
                    // Get the point and update the rectangle, step the calibration phase over and then break out.
                    pDestinationRectangle.BottomLeft = this.movePoint(fCalibrationMargin, 1.0 - fCalibrationMargin);
                    break;
                case 4:
                    // Get the point and update the rectangle, step the calibration phase over and then break out.
                    pDestinationRectangle.BottomRight = this.movePoint(1.0 - fCalibrationMargin, 1.0 - fCalibrationMargin);
                    break;
                case 5:
                    // We have finished calibrating.  Set the phase to 0 - disabled.
                    iCalibrationPhase = 0;

                    // Enable the transformation phase in the input provider.
                    this.finishedCalibration();
                    break;
                default:
                    throw new Exception("Unknown calibration phase!");
            }
        }

        /// <summary>
        /// Move the calibration point to a normalised coordinate location.
        /// </summary>
        /// <param name="fNormalX">The normalised X coordinate (between 0 and 1).</param>
        /// <param name="fNormalY">The normalised Y coordinate (between 0 and 1).</param>
        private Vector movePoint(double fNormalX, double fNormalY)
        {
            // Compute the transform the normals into screen space.
            Vector tPoint = new Vector(fNormalX * this.ActualWidth, fNormalY * this.ActualHeight);

            // Ensure it is visible.
            Dispatcher.BeginInvoke(new Action(delegate()
            {
                this.CalibrationPoint.Visibility = Visibility.Visible;

                // Move the canvas element.
                Canvas.SetLeft(this.CalibrationPoint, tPoint.X - (this.CalibrationPoint.ActualWidth / 2));
                Canvas.SetTop(this.CalibrationPoint, tPoint.Y - (this.CalibrationPoint.ActualHeight / 2));

            }), null);
            //this.CalibrationPoint.Visibility = Visibility.Visible;

            // Return the transformed normal.
            return tPoint;
        }
    }
}
