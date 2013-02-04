using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCD.System.TouchInjection;
using WiiTUIO.Provider;

namespace WiiTUIO.Output
{
    class TouchInjectProviderHandler : IProviderHandler
    {
        public event Action OnConnect;

        public event Action OnDisconnect;

        private int maxTouchPoints = 256;

        public void connect()
        {
            if (!TCD.System.TouchInjection.TouchInjector.InitializeTouchInjection((uint)maxTouchPoints, TouchFeedback.DEFAULT))
            {
                throw new Exception("Can not initialize touch injection");
            }
            OnConnect();
        }

        public void processEventFrame(Provider.FrameEventArgs e)
        {
            List<PointerTouchInfo> toFire = new List<PointerTouchInfo>();
            foreach (WiiContact contact in e.Contacts)
            {
                ContactType type = contact.Type;
                //make a new pointertouchinfo with all neccessary information
                PointerTouchInfo touch = new PointerTouchInfo();
                touch.PointerInfo.pointerType = PointerInputType.TOUCH;
                touch.TouchFlags = TouchFlags.NONE;
                //contact.Orientation = (uint)cur.getAngleDegrees();//this is only valid for TuioObjects
                touch.Pressure = 32000;
                touch.TouchMasks = TouchMask.CONTACTAREA | TouchMask.ORIENTATION | TouchMask.PRESSURE;
                touch.PointerInfo.PtPixelLocation.X = (int)contact.Position.X;
                touch.PointerInfo.PtPixelLocation.Y = (int)contact.Position.Y;
                touch.PointerInfo.PointerId = (uint)contact.ID;
                touch.ContactArea.left = (int)contact.BoundingRectangle.Left;
                touch.ContactArea.right = (int)contact.BoundingRectangle.Right;
                touch.ContactArea.top = (int)contact.BoundingRectangle.Top;
                touch.ContactArea.bottom = (int)contact.BoundingRectangle.Bottom;
                //set the right flag
                if (type == ContactType.Start)
                    touch.PointerInfo.PointerFlags = PointerFlags.DOWN | PointerFlags.INRANGE | PointerFlags.INCONTACT;
                else if (type == ContactType.Move)
                    touch.PointerInfo.PointerFlags = PointerFlags.UPDATE | PointerFlags.INRANGE | PointerFlags.INCONTACT;
                else if (type == ContactType.End)
                    touch.PointerInfo.PointerFlags = PointerFlags.UP;
                //add it to 'toFire'
                toFire.Add(touch);
            }
            //fire the events
            if (toFire.Count > 0)
            {
                if (!TCD.System.TouchInjection.TouchInjector.InjectTouchInput(toFire.Count, toFire.ToArray()))
                {
                    Console.WriteLine("Could not send touch input");
                }
            }
        }

        public void disconnect()
        {
            OnDisconnect();
        }

        public void showSettingsWindow()
        {
            
        }
    }
}
