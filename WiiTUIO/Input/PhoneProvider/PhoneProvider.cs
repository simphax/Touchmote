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
            int messageByteCount = 44; // We set the buffer to the size of one message so we never lag behind

            // Create the receiver
            receiver = new OSCReceiver(port,messageByteCount);
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
            OutputFactory.getCurrentProviderHandler().connect();

            netService = new DNSSDService();
            publishedService = netService.Register(0, 0, "Touchmote", "_touchmote._udp", null, null, port, null, null);
        }

        static void ListenLoop()
        {
            try
            {
                while (true)
                {
                        // get the next message 
                        // this will block until one arrives or the socket is closed
                        OSCPacket packet = receiver.Receive();

                        float pitch = (float)packet.Values[0];
                        float roll = (float)packet.Values[1];
                        float yaw = (float)packet.Values[2];
                        bool touchDown = (int)packet.Values[3] == 1;

                        foreach(IOutputHandler outputHandler in outputHandlers)
                        {
                            if(outputHandler is TouchHandler)
                            {
                                TouchHandler touchHandler = (TouchHandler)outputHandler;

                                touchHandler.startUpdate();

                                double xRel = (180 / Math.PI * yaw * -1 + 15) / 30;
                                double yRel = (180 / Math.PI * pitch * -1 + 15) / 30;

                                
                                
                                int x = Convert.ToInt32((float)primaryScreen.Bounds.Width * xRel);
                                int y = Convert.ToInt32((float)primaryScreen.Bounds.Height * yRel);

                                if (x < 0) x = 0;
                                if (y < 0) y = 0;
                                if (x >= primaryScreen.Bounds.Width) x = primaryScreen.Bounds.Width-1;
                                if (y >= primaryScreen.Bounds.Height) y = primaryScreen.Bounds.Height-1;

                                //Console.WriteLine("Set cursor x: " + x + " y: " + y);

                                CursorPos cursorPos = new CursorPos((int)x,(int)y, 0);
                                cursorPos.OutOfReach = false;

                                if(touchDown)
                                {
                                    touchHandler.setButtonDown("touchmaster");
                                }
                                else
                                {
                                    touchHandler.setButtonUp("touchmaster");
                                }
                                touchHandler.setPosition("touch", cursorPos);

                                touchHandler.endUpdate();
                            }
                        }

                        //Well this is weird to call these here but its to minimize latency
                        OutputFactory.getCurrentProviderHandler().processEventFrame();

                        if (Settings.Default.pointer_customCursor)
                        {
                            D3DCursorWindow.Current.RefreshCursors();
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
            OutputFactory.getCurrentProviderHandler().disconnect();
            publishedService.Stop();
            receiver.Close();
            messageRecieveThread.Abort();
        }

    }
}
