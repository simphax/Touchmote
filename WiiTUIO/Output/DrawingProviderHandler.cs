using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using WiiTUIO.Provider;

namespace WiiTUIO.Output
{
    class DrawingProviderHandler : IProviderHandler
    {
        public event Action OnConnect;

        public event Action OnDisconnect;

        Graphics graphic;

        public void connect()
        {
            graphic = Graphics.FromHwnd(IntPtr.Zero);
            OnConnect();
        }

        public void processEventFrame(Provider.FrameEventArgs e)
        {

            foreach (WiiContact contact in e.Contacts)
            {
                Color color = Color.Blue;
                switch (contact.Type)
                {
                    case ContactType.Start:
                        color = Color.Green;
                        break;
                    case ContactType.Move:
                        color = Color.Yellow;
                        break;
                    case ContactType.End:
                        color = Color.Red;
                        break;
                }

                Brush brush = new SolidBrush(color);
                graphic.DrawEllipse(Pens.Black, (float)contact.Position.X - 10, (float)contact.Position.Y - 10, 20, 20);
                graphic.FillEllipse(brush, (float)contact.Position.X - 10, (float)contact.Position.Y - 10, 20, 20);

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
