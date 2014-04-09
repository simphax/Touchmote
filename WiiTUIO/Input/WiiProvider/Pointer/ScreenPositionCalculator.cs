using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WiimoteLib;
using WiiTUIO.Properties;

namespace WiiTUIO.Provider
{
    public class ScreenPositionCalculator
    {

        private int minXPos;
        private int maxXPos;
        private int maxWidth;

        private int minYPos;
        private int maxYPos;
        private int maxHeight;
        private int SBPositionOffset;

        private double smoothedX, smoothedZ, smoothedRotation;
        private int orientation;

        private int leftPoint = -1;

        private CursorPos lastPos;

        private System.Drawing.Rectangle screenBounds;

        public ScreenPositionCalculator()
        {
            this.recalculateScreenBounds();

            SystemEvents.DisplaySettingsChanged +=SystemEvents_DisplaySettingsChanged;

            lastPos = new CursorPos(0, 0, 0);
        }

        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            recalculateScreenBounds();
        }

        private void recalculateScreenBounds()
        {
            this.screenBounds = Util.ScreenBounds;
            minXPos = -(int)(screenBounds.Width * Settings.Default.pointer_marginsLeftRight);
            maxXPos = screenBounds.Width + (int)(screenBounds.Width * Settings.Default.pointer_marginsLeftRight);
            maxWidth = maxXPos - minXPos;
            minYPos = -(int)(screenBounds.Height * Settings.Default.pointer_marginsTopBottom);
            maxYPos = screenBounds.Height + (int)(screenBounds.Height * Settings.Default.pointer_marginsTopBottom);
            maxHeight = maxYPos - minYPos;
            SBPositionOffset = (int)(screenBounds.Width * Settings.Default.pointer_sensorBarPosCompensation);
        }

        private int lastX = Util.ScreenWidth / 2;
        private int lastY = Util.ScreenHeight / 2;

        private double calibratedPitch = -1;
        private double calibratedJaw = -1;

        double lastAccelY = 0;

        double lastPitch = double.NaN;
        double lastJaw = double.NaN;
        
        public CursorPos CalculateCursorPos(WiimoteState wiimoteState)
        {
            
            int x = 0;
            int y = 0;

            double accelY = wiimoteState.AccelState.Values.Y;
            double magic = Math.Abs(accelY * 0.005 + lastAccelY * 0.015);

            double pitch = (wiimoteState.MotionPlusState.RawValues.X / 16384.0);
            double jaw = (wiimoteState.MotionPlusState.RawValues.Z / 16384.0);

            lastAccelY = wiimoteState.AccelState.Values.Y;

            /*
            if (lastMPX != float.NaN)
            {
                float deltaX = wiimoteState.MotionPlusState.Values.X - lastMPX;
                deltaX *= 10;
            }
            if (lastMPY != float.NaN)
            {
                float deltaY = wiimoteState.MotionPlusState.Values.Y - lastMPX;
                deltaY *= 10;
            }

            lastMPX = wiimoteState.MotionPlusState.Values.X;
            lastMPY = wiimoteState.MotionPlusState.Values.Y;
            */

            if (calibratedPitch == -1 || (wiimoteState.ButtonState.Down && wiimoteState.ButtonState.A))
            {
                calibratedPitch = pitch;
                lastX = Util.ScreenWidth / 2;
            }
            else
            {
                //calibratedMPX = (wiimoteState.MotionPlusState.RawValues.X / 16384.0) * 0.01 + calibratedMPX * 0.99;
            }
            if (calibratedJaw == -1 || (wiimoteState.ButtonState.Down && wiimoteState.ButtonState.A))
            {
                calibratedJaw = jaw;
                lastY = Util.ScreenHeight / 2;
            }
            else
            {
                //calibratedMPY = (wiimoteState.MotionPlusState.RawValues.Z / 16384.0) * 0.01 + calibratedMPY * 0.99;
            }
            /*
            if (lastPitch.Equals(double.NaN))
            {
                lastPitch = pitch;
            }
            if (lastJaw.Equals(double.NaN))
            {
                lastJaw = jaw;
            }

            double deltaX = pitch - lastPitch;
            double deltaY = jaw - lastJaw;

            lastPitch = pitch;
            lastJaw = jaw;
            */
            double deltaX = calibratedPitch - pitch;
            double deltaY = calibratedJaw - jaw;
            /*
            if (Math.Abs(pitch) < 0.01)
            {
                deltaX = 0;
            }
            if (Math.Abs(jaw) < 0.01)
            {
                deltaY = 0;
            }
            */
            
            /*
            if (lastMPX == -1)
            {
                lastMPX = (wiimoteState.MotionPlusState.RawValues.X / 16384.0);
            }
            if (lastMPY == -1)
            {
                lastMPY = (wiimoteState.MotionPlusState.RawValues.Y / 16384.0);
            }

            double deltaX = (wiimoteState.MotionPlusState.RawValues.X / 16384.0) - lastMPX;
            double deltaY = (wiimoteState.MotionPlusState.RawValues.Y / 16384.0) + lastMPX;

            lastMPX = (wiimoteState.MotionPlusState.RawValues.X / 16384.0);
            lastMPY = (wiimoteState.MotionPlusState.RawValues.Y / 16384.0);

            Console.WriteLine("DeltaX: " + deltaX);
            Console.WriteLine("DeltaY: " + deltaY);
            */
            /*
            if (Math.Abs(deltaX) <= 0.1)
            {
                calibratedPitch = pitch * 0.01 + calibratedPitch * 0.99;
            }

            if (Math.Abs(deltaY) <= 0.1)
            {
                calibratedJaw = jaw * 0.01 + calibratedJaw * 0.99;
            }
            */
            deltaX *= 500;
            deltaY *= 500;

            x = lastX - Convert.ToInt32(deltaX);
            y = lastY + Convert.ToInt32(deltaY);

            if (x <= 0)
            {
                x = 0;
            }
            else if (x >= Util.ScreenWidth)
            {
                x = Util.ScreenWidth - 1;
            }
            if (y <= 0)
            {
                y = 0;
            }
            else if (y >= Util.ScreenHeight)
            {
                y = Util.ScreenHeight - 1;
            }

            lastX = x;
            lastY = y;

            CursorPos result = new CursorPos(x,y,0);
            result.OutOfReach = false;

            return result;
        }

        public CursorPos CalculateCursorPosIR(WiimoteState wiimoteState)
        {
            int x;
            int y;

            IRState irState = wiimoteState.IRState;

            PointF relativePosition = new PointF();

            bool foundMidpoint = false;

            /*for(int i=0;i<irState.IRSensors.Count() && !foundMidpoint;i++)//IRSensor sensor in irState.IRSensors)
            {
                IRSensor sensor = irState.IRSensors[i];
                if (sensor.Found)
                {
                    for (int j = i + 1; j < irState.IRSensors.Count() && !foundMidpoint; j++)
                    {
                        IRSensor sensor2 = irState.IRSensors[j];
                        if (sensor2.Found)
                        {*/
            if(irState.IRSensors[0].Found && irState.IRSensors[1].Found)
            {
                            foundMidpoint = true;

                            relativePosition.X = (irState.IRSensors[0].Position.X + irState.IRSensors[1].Position.X) / 2.0f;
                            relativePosition.Y = (irState.IRSensors[0].Position.Y + irState.IRSensors[1].Position.Y) / 2.0f;

                            if (Settings.Default.pointer_considerRotation)
                            {
                                //accelSmoothing.addValue(new System.Windows.Vector(args.WiimoteState.AccelState.RawValues.X, args.WiimoteState.AccelState.RawValues.Z));

                                //System.Windows.Vector smoothedRotation = accelSmoothing.getSmoothedValue();
                                /*
                                while (accXhistory.Count >= Settings.Default.pointer_rotationSmoothing)
                                {
                                    accXhistory.Dequeue();
                                }
                                while (accZhistory.Count >= Settings.Default.pointer_rotationSmoothing)
                                {
                                    accZhistory.Dequeue();
                                }

                                accXhistory.Enqueue(args.WiimoteState.AccelState.RawValues.X);
                                accZhistory.Enqueue(args.WiimoteState.AccelState.RawValues.Z);
                                
                                smoothedX = 0;
                                smoothedZ = 0;

                                foreach (double accX in accXhistory)
                                {
                                    smoothedX += accX;
                                }
                                smoothedX /= accXhistory.Count;

                                foreach (double accZ in accZhistory)
                                {
                                    smoothedZ += accZ;
                                }
                                smoothedZ /= accZhistory.Count;
                                */

                                smoothedX = smoothedX * 0.9 + wiimoteState.AccelState.RawValues.X * 0.1;
                                smoothedZ = smoothedZ * 0.9 + wiimoteState.AccelState.RawValues.Z * 0.1;

                                double absx = Math.Abs(smoothedX - 128), absz = Math.Abs(smoothedZ - 128);

                                if (orientation == 0 || orientation == 2) absx -= 5;
                                if (orientation == 1 || orientation == 3) absz -= 5;

                                if (absz >= absx)
                                {
                                    if (absz > 5)
                                        orientation = (smoothedZ > 128) ? 0 : 2;
                                }
                                else
                                {
                                    if (absx > 5)
                                        orientation = (smoothedX > 128) ? 3 : 1;
                                }

                                int l = leftPoint, r;
                                //if (leftPoint == -1)
                                //{
                                    switch (orientation)
                                    {
                                        case 0: l = (irState.IRSensors[0].RawPosition.X < irState.IRSensors[1].RawPosition.X) ? 0 : 1; break;
                                        case 1: l = (irState.IRSensors[0].RawPosition.Y > irState.IRSensors[1].RawPosition.Y) ? 0 : 1; break;
                                        case 2: l = (irState.IRSensors[0].RawPosition.X > irState.IRSensors[1].RawPosition.X) ? 0 : 1; break;
                                        case 3: l = (irState.IRSensors[0].RawPosition.Y < irState.IRSensors[1].RawPosition.Y) ? 0 : 1; break;
                                    }
                                    leftPoint = l;
                                //}
                                r = 1 - l;

                                double dx = irState.IRSensors[r].RawPosition.X - irState.IRSensors[l].RawPosition.X;
                                double dy = irState.IRSensors[r].RawPosition.Y - irState.IRSensors[l].RawPosition.Y;

                                double d = Math.Sqrt(dx * dx + dy * dy);

                                dx /= d;
                                dy /= d;

                                smoothedRotation = 0.7 * smoothedRotation + 0.3 * Math.Atan2(dy, dx);

                                /*
                                while (rotationHistory.Count >= Settings.Default.pointer_rotationSmoothing)
                                {
                                    rotationHistory.Dequeue();
                                }

                                rotationHistory.Enqueue(rotation);

                                double smoothedRotation = 0;
                                foreach (double rot in rotationHistory)
                                {
                                    smoothedRotation += rot;
                                }
                                smoothedRotation /= rotationHistory.Count;
                                */

                                //smoothedRotation = smoothedRotation * 0.9 + rotation * 0.1;
                                //rotation = smoothedRotation;
                            }
                        //}
                    //}
                //}
            }

            if (!foundMidpoint)
            {
                CursorPos err = lastPos;
                err.OutOfReach = true;

                return err;
            }

            int offsetY = 0;

            if (Properties.Settings.Default.pointer_sensorBarPos == "top")
            {
                offsetY = -SBPositionOffset;
            }
            else if (Properties.Settings.Default.pointer_sensorBarPos == "bottom")
            {
                offsetY = SBPositionOffset;
            }

            relativePosition.X = 1 - relativePosition.X;
            
            if (Settings.Default.pointer_considerRotation)
            {
                //accelSmoothing.addValue(new System.Windows.Vector(args.WiimoteState.AccelState.Values.X, args.WiimoteState.AccelState.Values.Z));

                //System.Windows.Vector smoothedRotation = accelSmoothing.getSmoothedValue();

                //rotation = -1 * (Math.Atan2(smoothedRotation.Y, smoothedRotation.X) - (Math.PI / 2.0));

                relativePosition.X = relativePosition.X - 0.5F;
                relativePosition.Y = relativePosition.Y - 0.5F;

                relativePosition = this.rotatePoint(relativePosition, smoothedRotation);

                relativePosition.X = relativePosition.X + 0.5F;
                relativePosition.Y = relativePosition.Y + 0.5F;

                //relativePosition.X = 1 - relativePosition.X;
                //relativePosition.Y = 1 - relativePosition.Y;
            }
            
            x = Convert.ToInt32((float)maxWidth * relativePosition.X + minXPos);
            y = Convert.ToInt32((float)maxHeight * relativePosition.Y + minYPos) + offsetY;

            if (x <= 0)
            {
                x = 0;
            }
            else if (x >= Util.ScreenWidth)
            {
                x = Util.ScreenWidth - 1;
            }
            if (y <= 0)
            {
                y = 0;
            }
            else if (y >= Util.ScreenHeight)
            {
                y = Util.ScreenHeight - 1;
            }

            CursorPos result = new CursorPos(x, y, smoothedRotation);
            lastPos = result;
            return result;
        }

        private PointF rotatePoint(PointF point, double angle)
        {
            double sin = Math.Sin(angle);
            double cos = Math.Cos(angle);

            double xnew = point.X * cos - point.Y * sin;
            double ynew = point.X * sin + point.Y * cos;

            PointF result;
            
            result.X = (float)xnew;
            result.Y = (float)ynew;

            return result;
        }

    }
}
