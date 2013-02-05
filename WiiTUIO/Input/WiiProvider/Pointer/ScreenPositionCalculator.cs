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
            int minYPos = -(Util.ScreenHeight / 3);
            int maxYPos = Util.ScreenHeight + (Util.ScreenHeight / 3);
            int maxHeight = maxYPos - minYPos;
            int y;

            PointF relativePosition = new PointF();
           if (args.WiimoteState.IRState.IRSensors[0].Found && args.WiimoteState.IRState.IRSensors[1].Found)
            {
                relativePosition = args.WiimoteState.IRState.Midpoint;
            }
               /*
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
            */

           if (relativePosition.X == 0 && relativePosition.Y == 0)
           {
               Point p = new Point();
               p.X = -1;
               p.Y = -1;
               return p;
           }

            x = Convert.ToInt32((float)maxWidth * (1.0F - relativePosition.X) + minXPos);
            y = Convert.ToInt32((float)maxHeight * relativePosition.Y + minYPos);
            
            if (x <= 0)
            {
                x = 0;
            }
            else if (x >= Util.ScreenWidth)
            {
                x = Util.ScreenWidth-1;
            }
            if (y <= 0)
            {
                y = 0;
            }
            else if (y >= Util.ScreenHeight)
            {
                y = Util.ScreenHeight-1;
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

            x = 1.0F - relativePosition.X -0.5F;//Convert.ToInt32((float)maxWidth * (1.0F - relativePosition.X)) + minXPos;
            y = relativePosition.Y -0.5F;//Convert.ToInt32((float)maxHeight * relativePosition.Y) + minYPos;
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
