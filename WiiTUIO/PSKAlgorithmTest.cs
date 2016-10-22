using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiimoteLib;
using WiiTUIO.Output;
using WiiTUIO.Output.Handlers;
using WiiTUIO.Output.Handlers.Touch;
using WiiTUIO.Provider;

namespace WiiTUIO
{
    class TouchProviderHandler : ITouchProviderHandler
    {
        public event Action OnConnect;
        public event Action OnDisconnect;

        public void connect()
        {
            //Console.WriteLine("connect");
        }

        public void disconnect()
        {
            //Console.WriteLine("disconnect");
        }

        public void processEventFrame()
        {
            //Console.WriteLine("process event frame");
        }

        public void queueContact(WiiContact contact)
        {
            //Console.WriteLine(contact);
        }
    }

    class PSKAlgorithmTest
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Press enter to continue");
            Console.ReadLine();
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            

            ScreenPositionCalculator screenPositionCalculator = new ScreenPositionCalculator();
            PointF pos1;
            pos1.X = 10;
            pos1.Y = 10;
            Point ppos1;
            ppos1.X = 10;
            ppos1.Y = 10;
            PointF pos2;
            pos2.X = 20;
            pos2.Y = 20;
            Point ppos2;
            ppos2.X = 20;
            ppos2.Y = 20;
            IRSensor sensor1 = new IRSensor();
            sensor1.Position = pos1;
            sensor1.Found = true;
            sensor1.Size = 1;
            sensor1.RawPosition = ppos1;
            IRSensor sensor2 = new IRSensor();
            sensor2.Position = pos2;
            sensor2.Found = true;
            sensor1.Size = 1;
            sensor1.RawPosition = ppos2;
            IRState irState = new IRState();
            irState.IRSensors = new IRSensor[2] { sensor1, sensor2 };
            irState.Mode = IRMode.Extended;
            WiimoteState wiimoteStateMock = new WiimoteState();
            wiimoteStateMock.IRState = irState;
            wiimoteStateMock.NunchukState = new NunchukState();
            wiimoteStateMock.AccelCalibrationInfo = new AccelCalibrationInfo();
            AccelState accelState = new AccelState();
            accelState.Values = new Point3F()
            {
                X = 5, Y = 5, Z = 5
            };
            accelState.RawValues = new Point3()
            {
                X = 5,
                Y = 5,
                Z = 5
            };
            wiimoteStateMock.AccelState = accelState;

            wiimoteStateMock.BalanceBoardState = new BalanceBoardState();
            wiimoteStateMock.Battery = 1;
            wiimoteStateMock.BatteryRaw = 1;
            wiimoteStateMock.ButtonState = new ButtonState();
            wiimoteStateMock.ClassicControllerState = new ClassicControllerState();
            wiimoteStateMock.DrumsState = new DrumsState();
            wiimoteStateMock.Extension = true;
            wiimoteStateMock.ExtensionType = ExtensionType.Nunchuk;
            wiimoteStateMock.GuitarState = new GuitarState();
            wiimoteStateMock.LEDState = new LEDState();
            wiimoteStateMock.NunchukState = new NunchukState();
            wiimoteStateMock.Rumble = false;

            CursorPos cursorPos = new CursorPos(10, 10, 0.5, 0.5, 0);

            TouchHandler touchHandler = new TouchHandler(new TouchProviderHandler(), 1);
            touchHandler.connect();
            for (int j = 0; j < 20; j++)
            {
                stopwatch.Reset();
                stopwatch.Start();
                for (int i = 0; i < 1000000; i++)
                {
                    touchHandler.startUpdate();
                    touchHandler.setPosition("touch", screenPositionCalculator.CalculateCursorPos(wiimoteStateMock));
                    touchHandler.endUpdate();
                }
                stopwatch.Stop();
                Console.WriteLine(stopwatch.ElapsedMilliseconds);
            }
            Console.ReadLine();
        }
    }
}
