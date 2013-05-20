using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WiimoteLib;

namespace WiiTUIO.Provider
{
    public class ScreenPositionCalculator
    {

        private PointF m_FirstSensorPos;
        private PointF m_SecondSensorPos;
        private PointF m_MidSensorPos;

        private int minXPos;
        private int maxXPos;
        private int maxWidth;

        private int minYPos;
        private int maxYPos;
        private int maxHeight;
        private int SBPositionOffset;

        private System.Drawing.Rectangle screenBounds;

        SmoothingBuffer rotationSmoothing;

        public ScreenPositionCalculator()
        {
            this.recalculateScreenBounds();

            this.rotationSmoothing = new SmoothingBuffer(20);

            SystemEvents.DisplaySettingsChanged +=SystemEvents_DisplaySettingsChanged;
        }

        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            recalculateScreenBounds();
        }

        private void recalculateScreenBounds()
        {
            this.screenBounds = Util.ScreenBounds;
            minXPos = -(screenBounds.Width / 3);
            maxXPos = screenBounds.Width + (screenBounds.Width / 3);
            maxWidth = maxXPos - minXPos;
            minYPos = -(screenBounds.Height / 2);
            maxYPos = screenBounds.Height + (screenBounds.Height / 2);
            maxHeight = maxYPos - minYPos;
            SBPositionOffset = (screenBounds.Width / 4);
        }

        public CursorPos CalculateCursorPos(WiimoteChangedEventArgs args)
        {
            int x;
            int y;

            IRState irState = args.WiimoteState.IRState;

            PointF relativePosition = new PointF();

            bool foundMidpoint = false;

            for(int i=0;i<irState.IRSensors.Count() && !foundMidpoint;i++)//IRSensor sensor in irState.IRSensors)
            {
                IRSensor sensor = irState.IRSensors[i];
                if (sensor.Found)
                {
                    for (int j = i + 1; j < irState.IRSensors.Count() && !foundMidpoint; j++)
                    {
                        IRSensor sensor2 = irState.IRSensors[j];
                        if (sensor2.Found)
                        {
                            relativePosition.X = (sensor.Position.X + sensor2.Position.X) / 2.0f;
                            relativePosition.Y = (sensor.Position.Y + sensor2.Position.Y) / 2.0f;
                            foundMidpoint = true;
                        }
                    }
                }
            }

            if (!foundMidpoint)
            {
                CursorPos err = new CursorPos(-1,-1,0);
                
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
            rotationSmoothing.addValue(new System.Windows.Vector(args.WiimoteState.AccelState.Values.X, args.WiimoteState.AccelState.Values.Z));
            System.Windows.Vector smoothedRotation = rotationSmoothing.getSmoothedValue();

            double rotation = -1*(Math.Atan2(smoothedRotation.Y,smoothedRotation.X) - (Math.PI / 2.0));

            relativePosition.X = 1 - relativePosition.X;

            relativePosition.X = relativePosition.X - 0.5F;
            relativePosition.Y = relativePosition.Y - 0.5F;

            relativePosition = this.rotatePoint(relativePosition,rotation);

            relativePosition.X = relativePosition.X + 0.5F;
            relativePosition.Y = relativePosition.Y + 0.5F;

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

            CursorPos result = new CursorPos(x,y,rotation);
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
