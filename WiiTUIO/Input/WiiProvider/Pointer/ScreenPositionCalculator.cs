using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
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

        private Screen primaryScreen;

        public ScreenPositionCalculator()
        {
            this.primaryScreen = DeviceUtils.DeviceUtil.GetScreen(Settings.Default.primaryMonitor);
            this.recalculateScreenBounds(this.primaryScreen);

            Settings.Default.PropertyChanged += SettingsChanged;
            SystemEvents.DisplaySettingsChanged +=SystemEvents_DisplaySettingsChanged;

            lastPos = new CursorPos(0, 0, 0, 0, 0);

        }

        private void SettingsChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "primaryMonitor")
            {
                this.primaryScreen = DeviceUtils.DeviceUtil.GetScreen(Settings.Default.primaryMonitor);
                Console.WriteLine("Setting primary monitor for screen position calculator to "+this.primaryScreen.Bounds);
                this.recalculateScreenBounds(this.primaryScreen);
            }
        }

        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            this.primaryScreen = DeviceUtils.DeviceUtil.GetScreen(Settings.Default.primaryMonitor);
            recalculateScreenBounds(this.primaryScreen);
        }

        private void recalculateScreenBounds(Screen screen)
        {
            Console.WriteLine("Setting primary monitor for screen position calculator to " + this.primaryScreen.Bounds);
            minXPos = -(int)(screen.Bounds.Width * Settings.Default.pointer_marginsLeftRight);
            maxXPos = screen.Bounds.Width + (int)(screen.Bounds.Width * Settings.Default.pointer_marginsLeftRight);
            maxWidth = maxXPos - minXPos;
            minYPos = -(int)(screen.Bounds.Height * Settings.Default.pointer_marginsTopBottom);
            maxYPos = screen.Bounds.Height + (int)(screen.Bounds.Height * Settings.Default.pointer_marginsTopBottom);
            maxHeight = maxYPos - minYPos;
            SBPositionOffset = (int)(screen.Bounds.Height * Settings.Default.pointer_sensorBarPosCompensation);
        }

        public CursorPos CalculateCursorPos(WiimoteState wiimoteState)
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
            else if (x >= primaryScreen.Bounds.Width)
            {
                x = primaryScreen.Bounds.Width - 1;
            }
            if (y <= 0)
            {
                y = 0;
            }
            else if (y >= primaryScreen.Bounds.Height)
            {
                y = primaryScreen.Bounds.Height - 1;
            }

            CursorPos result = new CursorPos(x, y, relativePosition.X, relativePosition.Y, smoothedRotation);
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
