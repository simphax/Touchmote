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

        public ScreenPositionCalculator()
        {
            minXPos = -(Util.ScreenWidth / 3);
            maxXPos = Util.ScreenWidth + (Util.ScreenWidth / 3);
            maxWidth = maxXPos - minXPos;
            minYPos = -(Util.ScreenHeight / 2);
            maxYPos = Util.ScreenHeight + (Util.ScreenHeight / 2);
            maxHeight = maxYPos - minYPos;
            SBPositionOffset = (Util.ScreenWidth / 4);
        }

        public Point GetPosition(WiimoteChangedEventArgs args)
        {
            int x;
            int y;

            IRState irState = args.WiimoteState.IRState;

            PointF relativePosition = new PointF();

            bool foundMidpoint = false;

            for(int i=0;i<irState.IRSensors.Count() && !foundMidpoint;i++)//IRSensor sensor in irState.IRSensors)
            {
                IRSensor sensor = irState.IRSensors[i];
                if (sensor.Found && sensor.Size > 0)
                {
                    for (int j = i + 1; j < irState.IRSensors.Count() && !foundMidpoint; j++)
                    {
                        IRSensor sensor2 = irState.IRSensors[j];
                        if (sensor2.Found && sensor2.Size > 0)
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
                Point err = new Point();
                err.X = -1;
                err.Y = -1;
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

            x = Convert.ToInt32((float)maxWidth * (1.0F - relativePosition.X) + minXPos);
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

            Point point = new Point();
            point.X = x;
            point.Y = y;
            return point;
        }

    }
}
