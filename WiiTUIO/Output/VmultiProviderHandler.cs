using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TCD.System.TouchInjection;
using WiiTUIO.Properties;
using WiiTUIO.Provider;
using VMultiDllWrapper;

namespace WiiTUIO.Output
{
    class VmultiProviderHandler : ITouchProviderHandler
    {
        private Queue<WiiContact> contactQueue;

        public event Action OnConnect;
        public event Action OnDisconnect;

        private Mutex touchscreenMutex = new Mutex();

        public VmultiProviderHandler()
        {
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;

            contactQueue = new Queue<WiiContact>();
        }

        public void connect()
        {
            if (OnConnect != null)
            {
                OnConnect();
            }
        }

        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {

        }

        public void processEventFrame()
        {
            touchscreenMutex.WaitOne();
            List<MultitouchPointerInfo> toFire = new List<MultitouchPointerInfo>();

            ulong timestamp = (ulong)Stopwatch.GetTimestamp();
            WiiContact contact;
            while (contactQueue.Count > 0)
            {
                contact = contactQueue.Dequeue();
                if (Settings.Default.pointer_customCursor && (contact.Type == ContactType.Hover || contact.Type == ContactType.EndFromHover))
                {
                    //If we are using the custom cursor and it's more than 1 touchpoints, we skip the hovering because otherwise it's not working with edge guestures for example.
                }
                else
                {
                    ContactType type = contact.Type;

                    MultitouchPointerInfo pointerInfo = new MultitouchPointerInfo();

                    pointerInfo.X = contact.NormalPosition.X;
                    pointerInfo.Y = contact.NormalPosition.Y;

                    pointerInfo.Down = type == ContactType.Start || type == ContactType.Move;

                    pointerInfo.ID = (byte)contact.ID;

                    toFire.Add(pointerInfo);
                }
            }
            //fire the events
            if (toFire.Count > 0)
            {
                MultitouchReport report = new MultitouchReport(toFire);
                if (!VmultiDevice.Current.updateMultitouch(report))
                {
                    Console.WriteLine("Could not send touch input, count " + toFire.Count);
                }
            }
            touchscreenMutex.ReleaseMutex();
        }

        public void disconnect()
        {
            VmultiDevice.Current.updateMultitouch(new MultitouchReport(new List<MultitouchPointerInfo>())); //Release all touches
            //VmultiDevice.Current.disconnect();

            if (OnConnect != null)
            {
                OnDisconnect();
            }
        }

        public void queueContact(WiiContact contact)
        {
            contactQueue.Enqueue(contact);
        }
    }
}
