using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows;
using WiimoteLib;
using System.Runtime.InteropServices;
using System.Drawing;
using WindowsInput;
using WiiTUIO.Properties;
using System.Windows.Controls;
using System.Threading;
using WiiTUIO.Output.Handlers.Touch;
using WiiTUIO.Output;
using WiiTUIO.Output.Handlers;
using OSC.NET;
using System.Windows.Forms;
using Bonjour;

namespace WiiTUIO.Provider
{
    /// <summary>
    /// The WiiProvider implements <see cref="IProvider"/> in order to offer a type of object which uses the Wiimote to generate new event frames.
    /// </summary>
    public class PhoneProvider : IProvider
    {
        public event Action<int, int> OnConnect;
        public event Action<int, int> OnDisconnect;
        public event Action<WiimoteStatus> OnStatusUpdate;
        public event EventHandler<FrameEventArgs> OnNewFrame;

        private static OSCReceiver receiver;
        private static Thread messageRecieveThread;

        //Bonjour
        private static DNSSDService netService;
        private static DNSSDService publishedService;

        private static List<IOutputHandler> outputHandlers;
        private static Screen primaryScreen;

        public void start()
        {
            primaryScreen = DeviceUtils.DeviceUtil.GetScreen(Settings.Default.primaryMonitor);

            // This is the port we are going to listen on 
            ushort port = 3560;
            //int messageByteCount = 248;//44; // We set the buffer to the size of one message so we never lag behind

            // Create the receiver
            receiver = new OSCReceiver(port);
            receiver.Connect();

            // Create a thread to do the listening
            messageRecieveThread = new Thread(new ThreadStart(ListenLoop));

            // Start the listen thread
            messageRecieveThread.Start();

            HandlerFactory handlerFactory = new HandlerFactory();

            outputHandlers = handlerFactory.getOutputHandlers(1);

            foreach (IOutputHandler handler in outputHandlers)
            {
                handler.connect();
            }
            TouchOutputFactory.getCurrentProviderHandler().connect();

            netService = new DNSSDService();
            publishedService = netService.Register(0, 0, "Touchmote", "_touchmote._udp", null, null, port, null, null);
        }


        private static float pitch,roll,yaw,lastYaw,lastPitch;
        private static bool touchDown;

        private static int lastSentMessageId;

        private static Dictionary<int,Vector> offsetTouches;

        static void ListenLoop()
        {
            try
            {
                lastYaw = 0;
                lastPitch = 0;
                offsetTouches = new Dictionary<int,Vector>();
                while (true)
                {
                    // get the next message 
                    // this will block until one arrives or the socket is closed
                    OSCPacket packets = receiver.Receive(); //Should result in a OSCBundle

                    foreach (OSCMessage packet in packets.Values)
                    {
                        if (packet.Address == "/tmote/begin")
                        {
                            foreach (IOutputHandler outputHandler in outputHandlers)
                            {
                                if (outputHandler is TouchHandler)
                                {
                                    TouchHandler touchHandler = (TouchHandler)outputHandler;

                                    touchHandler.startUpdate();
                                }
                            }

                            pitch = 0;
                            roll = 0;
                            yaw = 0;
                            touchDown = false;
                            offsetTouches.Clear();
                        }
                        else if (packet.Address == "/tmote/motion")
                        {
                            pitch = (float)packet.Values[1];
                            roll = (float)packet.Values[2];
                            yaw = (float)packet.Values[3];
                            /*
                            if (lastYaw == 0)
                            {
                                lastYaw = yaw;
                            }

                            // kalman filtering
                            float q = 1.0f;   // process noise
                            float r = 1.0f;   // sensor noise
                            float p = 1.0f;   // estimated error
                            float k = 0.5f;   // kalman filter gain

                            float x = lastYaw;
                            p = p + q;
                            k = p / (p + r);
                            x = x + k * (yaw - x);
                            p = (1 - k) * p;
                            lastYaw = x;

                            if (lastPitch == 0)
                            {
                                lastPitch = pitch;
                            }

                            // kalman filtering
                            q = 1.0f;   // process noise
                            r = 1.0f;   // sensor noise
                            p = 1.0f;   // estimated error
                            k = 0.5f;   // kalman filter gain

                            x = lastPitch;
                            p = p + q;
                            k = p / (p + r);
                            x = x + k * (pitch - x);
                            p = (1 - k) * p;
                            lastPitch = x;
                             * */
                            lastYaw = yaw;
                            lastPitch = pitch;
                        }
                        else if (packet.Address == "/tmote/relCur")
                        {
                            offsetTouches[(int)packet.Values[1]] = new Vector((float)packet.Values[2], (float)packet.Values[3]);
                        }
                        else if (packet.Address == "/tmote/buttons")
                        {
                            touchDown = (int)packet.Values[1] == 1;
                        }
                        else if (packet.Address == "/tmote/end")
                        {
                            foreach (IOutputHandler outputHandler in outputHandlers)
                            {
                                if (outputHandler is PhoneTouchHandler)
                                {
                                    PhoneTouchHandler touchHandler = (PhoneTouchHandler)outputHandler;

                                    double xRel = (180 / Math.PI * lastYaw * -1 + 15) / 30;
                                    double yRel = (180 / Math.PI * lastPitch * -1 + 15) / 30;

                                    if(offsetTouches.Count > 0)
                                    {
                                        foreach (KeyValuePair<int, Vector> entry in offsetTouches)
                                        {
                                            int touchId = entry.Key;
                                            Vector offset = entry.Value;

                                            double xRelOff = xRel + offset.X * 0.5;
                                            double yRelOff = yRel + offset.Y * 0.8;

                                            int x = Convert.ToInt32((float)primaryScreen.Bounds.Width * xRelOff);
                                            int y = Convert.ToInt32((float)primaryScreen.Bounds.Height * yRelOff);

                                            if (x < 0) x = 0;
                                            if (y < 0) y = 0;
                                            if (x >= primaryScreen.Bounds.Width) x = primaryScreen.Bounds.Width - 1;
                                            if (y >= primaryScreen.Bounds.Height) y = primaryScreen.Bounds.Height - 1;

                                            //Console.WriteLine("Set cursor x: " + x + " y: " + y);

                                            CursorPos cursorPos = new CursorPos((int)x, (int)y, 0);
                                            cursorPos.OutOfReach = false;

                                            if (touchDown)
                                            {
                                                touchHandler.setButtonDown("touch" + touchId);
                                            }
                                            else
                                            {
                                                touchHandler.setButtonUp("touch0");
                                                touchHandler.setButtonUp("touch1");
                                                touchHandler.setButtonUp("touch2");
                                                touchHandler.setButtonUp("touch3");
                                            }
                                            touchHandler.setPosition("touch" + touchId, cursorPos);
                                        }
                                    }
                                    else
                                    {
                                        int x = Convert.ToInt32((float)primaryScreen.Bounds.Width * xRel);
                                        int y = Convert.ToInt32((float)primaryScreen.Bounds.Height * yRel);

                                        if (x < 0) x = 0;
                                        if (y < 0) y = 0;
                                        if (x >= primaryScreen.Bounds.Width) x = primaryScreen.Bounds.Width - 1;
                                        if (y >= primaryScreen.Bounds.Height) y = primaryScreen.Bounds.Height - 1;

                                        //Console.WriteLine("Set cursor x: " + x + " y: " + y);

                                        CursorPos cursorPos = new CursorPos((int)x, (int)y, 0);
                                        cursorPos.OutOfReach = false;

                                        if (touchDown)
                                        {
                                            touchHandler.setButtonDown("touch0");
                                        }
                                        else
                                        {
                                            touchHandler.setButtonUp("touch0");
                                            touchHandler.setButtonUp("touch1");
                                            touchHandler.setButtonUp("touch2");
                                            touchHandler.setButtonUp("touch3");
                                        }
                                        touchHandler.setPosition("touch0", cursorPos);
                                    }

                                    touchHandler.endUpdate();
                                }
                            }

                            //Well this is weird to call these here but its to minimize latency
                            TouchOutputFactory.getCurrentProviderHandler().processEventFrame();

                            if (Settings.Default.pointer_customCursor)
                            {
                                D3DCursorWindow.Current.RefreshCursors();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in listen loop");
                Console.WriteLine(ex.Message);
            }
        }

        public void stop()
        {
            foreach (IOutputHandler handler in outputHandlers)
            {
                handler.disconnect();
            }
            TouchOutputFactory.getCurrentProviderHandler().disconnect();
            publishedService.Stop();
            receiver.Close();
            messageRecieveThread.Abort();
        }

    }
}
