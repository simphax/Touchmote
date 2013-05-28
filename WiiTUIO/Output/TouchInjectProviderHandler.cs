using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TCD.System.TouchInjection;
using WiiTUIO.Properties;
using WiiTUIO.Provider;

namespace WiiTUIO.Output
{
    class TouchInjectProviderHandler : IProviderHandler
    {
        public event Action OnConnect;

        public event Action OnDisconnect;

        private int maxTouchPoints = 256;

        private Mutex touchscreenMutex = new Mutex();

        public void connect()
        {
            TouchFeedback feedback = Settings.Default.pointer_customCursor ?  TouchFeedback.NONE : TouchFeedback.INDIRECT;
            if (!TCD.System.TouchInjection.TouchInjector.InitializeTouchInjection((uint)maxTouchPoints, feedback))
            {
                throw new Exception("Can not initialize touch injection");
            }

            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;

            OnConnect();
        }

        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            Launcher.Launch("", "ResetTouchInjection.exe", "", null);
        }

        public void processEventFrame(Provider.FrameEventArgs e)
        {
            touchscreenMutex.WaitOne();
            List<PointerTouchInfo> toFire = new List<PointerTouchInfo>();
            //Console.WriteLine("Recieved " + e.Contacts.Count() + " contacts");
            foreach (WiiContact contact in e.Contacts)
            {
                /*
                if (contact.Type != ContactType.Hover)
                {
                    Console.WriteLine("Recieved contact " + contact.ID + " type "+contact.Type.ToString() + " position "  + contact.Position.ToString());
                }
                */
                ContactType type = contact.Type;
                //make a new pointertouchinfo with all neccessary information
                PointerTouchInfo touch = new PointerTouchInfo();
                touch.PointerInfo.pointerType = PointerInputType.TOUCH;
                touch.TouchFlags = TouchFlags.NONE;
                //contact.Orientation = (uint)cur.getAngleDegrees();//this is only valid for TuioObjects
                touch.Pressure = 0;
                touch.TouchMasks = TouchMask.NONE;//.CONTACTAREA;// | TouchMask.ORIENTATION | TouchMask.PRESSURE;
                touch.PointerInfo.PtPixelLocation.X = (int)contact.Position.X;
                touch.PointerInfo.PtPixelLocation.Y = (int)contact.Position.Y;
                touch.PointerInfo.PointerId = (uint)contact.ID;
                touch.PointerInfo.PerformanceCount = e.Timestamp;
                /*
                touch.ContactArea.left = (int)contact.BoundingRectangle.Left;
                touch.ContactArea.right = (int)contact.BoundingRectangle.Right;
                touch.ContactArea.top = (int)contact.BoundingRectangle.Top;
                touch.ContactArea.bottom = (int)contact.BoundingRectangle.Bottom;
                */
                //set the right flag
                if (type == ContactType.Start)
                    touch.PointerInfo.PointerFlags = PointerFlags.DOWN | PointerFlags.INRANGE | PointerFlags.INCONTACT;
                else if (type == ContactType.Move)
                    touch.PointerInfo.PointerFlags = PointerFlags.UPDATE | PointerFlags.INRANGE | PointerFlags.INCONTACT;
                else if (type == ContactType.End)
                    touch.PointerInfo.PointerFlags = PointerFlags.UP;
                else if (type == ContactType.EndToHover)
                    touch.PointerInfo.PointerFlags = PointerFlags.UP | PointerFlags.INRANGE;
                else if (type == ContactType.Hover)
                    touch.PointerInfo.PointerFlags = PointerFlags.UPDATE | PointerFlags.INRANGE;
                else if (type == ContactType.EndFromHover)
                    touch.PointerInfo.PointerFlags = PointerFlags.UPDATE;
                //add it to 'toFire'
                toFire.Add(touch);
            }
            //fire the events
            if (toFire.Count > 0)
            {
                if (!TCD.System.TouchInjection.TouchInjector.InjectTouchInput(toFire.Count, toFire.ToArray()))
                {
                    Console.WriteLine("Could not send touch input, count " + toFire.Count);
                }
            }
            touchscreenMutex.ReleaseMutex();
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
