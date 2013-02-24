using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WiimoteLib;

namespace WiiTUIO.Provider
{
    public static class ScreenPositionCalculator
    {

        private static PointF m_FirstSensorPos;
        private static PointF m_SecondSensorPos;
        private static PointF m_MidSensorPos;

        public static Point GetPosition(WiimoteChangedEventArgs args)
        {
            int minXPos = -(Util.ScreenWidth / 3);
            int maxXPos = Util.ScreenWidth + (Util.ScreenWidth / 3);
            int maxWidth = maxXPos - minXPos;
            int x;
            int minYPos = -(Util.ScreenHeight / 2);
            int maxYPos = Util.ScreenHeight + (Util.ScreenHeight / 2);
            int maxHeight = maxYPos - minYPos;
            int y;

            IRState irState = args.WiimoteState.IRState;

            PointF relativePosition = new PointF();

            bool foundMidpoint = false;

            foreach (IRSensor sensor in irState.IRSensors)
            {
                foreach (IRSensor sensor2 in irState.IRSensors)
                {
                    if (sensor.Found && sensor2.Found && sensor.Size > 0 && sensor2.Size > 0)
                    {
                        relativePosition.X = (sensor.Position.X + sensor2.Position.X) / 2.0f;
                        relativePosition.Y = (sensor.Position.Y + sensor2.Position.Y) / 2.0f;
                        foundMidpoint = true;
                        break;
                    }
                }
                if (foundMidpoint)
                {
                    break;
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
                offsetY = -(Util.ScreenWidth / 4);
            }
            else if (Properties.Settings.Default.pointer_sensorBarPos == "bottom")
            {
                offsetY = (Util.ScreenWidth / 4);
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

        public struct RelativePoint
        {
            public float X;
            public float Y;
        }

        public static RelativePoint GetRelativePosition(WiimoteChangedEventArgs args)
        {
            int minXPos = 0;
            int maxXPos = Util.ScreenWidth;
            int maxWidth = maxXPos - minXPos;
            float x;
            int minYPos = 0;
            int maxYPos = Util.ScreenHeight;
            int maxHeight = maxYPos - minYPos;
            float y;

            PointF relativePosition = new PointF();
            if (args.WiimoteState.IRState.IRSensors[0].Found && args.WiimoteState.IRState.IRSensors[1].Found)
            {
                relativePosition = args.WiimoteState.IRState.Midpoint;
            }
            else if (args.WiimoteState.IRState.IRSensors[0].Found)
            {
                relativePosition.X = m_MidSensorPos.X + (args.WiimoteState.IRState.IRSensors[0].Position.X - m_FirstSensorPos.X);
                relativePosition.Y = m_MidSensorPos.Y + (args.WiimoteState.IRState.IRSensors[0].Position.Y - m_FirstSensorPos.Y);
            }
            else if (args.WiimoteState.IRState.IRSensors[1].Found)
            {
                relativePosition.X = m_MidSensorPos.X + (args.WiimoteState.IRState.IRSensors[1].Position.X - m_SecondSensorPos.X);
                relativePosition.Y = m_MidSensorPos.Y + (args.WiimoteState.IRState.IRSensors[1].Position.Y - m_SecondSensorPos.Y);
            }

            //Remember for next run
            m_FirstSensorPos = args.WiimoteState.IRState.IRSensors[0].Position;
            m_SecondSensorPos = args.WiimoteState.IRState.IRSensors[1].Position;
            m_MidSensorPos = relativePosition;

            x = 1.0F - relativePosition.X - 0.5F;//Convert.ToInt32((float)maxWidth * (1.0F - relativePosition.X)) + minXPos;
            y = relativePosition.Y - 0.5F;//Convert.ToInt32((float)maxHeight * relativePosition.Y) + minYPos;
            /*
            if (x < 0)
            {
                x = 0;
            }
            else if (x > Util.ScreenWidth)
            {
                x = Util.ScreenWidth;
            }
            if (y < 0)
            {
                y = 0;
            }
            else if (y > Util.ScreenHeight)
            {
                y = Util.ScreenHeight;
            }
            */
            RelativePoint point = new RelativePoint();
            point.X = x;
            point.Y = y;
            return point;
        }
    }
}
