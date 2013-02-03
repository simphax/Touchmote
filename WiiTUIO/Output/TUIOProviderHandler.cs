using OSC.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using WiiTUIO.Properties;
using WiiTUIO.Provider;

namespace WiiTUIO.Output
{
    public class TUIOProviderHandler :IProviderHandler
    {

        private static int iFrame = 0;

        /// <summary>
        /// A reference to an OSC data transmitter.
        /// </summary>
        private OSCTransmitter pUDPWriter = null;

        private Window settingsWindow = null;

        public TUIOProviderHandler()
        {
            this.settingsWindow = new TUIOSettings(this);
            this.settingsWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.settingsWindow.Hide();
        }
        
        public void processEventFrame(FrameEventArgs e)
        {
            // Create an new TUIO Bundle
                OSCBundle pBundle = new OSCBundle();

                // Create a fseq message and save it.  This is to associate a unique frame id with a bundle of SET and ALIVE.
                OSCMessage pMessageFseq = new OSCMessage("/tuio/2Dcur");
                pMessageFseq.Append("fseq");
                pMessageFseq.Append(++iFrame);//(int)e.Timestamp);
                pBundle.Append(pMessageFseq);

                // Create a alive message.
                OSCMessage pMessageAlive = new OSCMessage("/tuio/2Dcur");
                pMessageAlive.Append("alive");

                // Now we want to take the raw frame data and draw points based on its data.
                foreach (WiiContact pContact in e.Contacts)
                {
                    // Compile the set message.
                    OSCMessage pMessage = new OSCMessage("/tuio/2Dcur");
                    pMessage.Append("set");                 // set
                    pMessage.Append((int)pContact.ID);           // session
                    pMessage.Append((float)pContact.NormalPosition.X);   // x
                    pMessage.Append((float)pContact.NormalPosition.Y);   // y
                    pMessage.Append(0f);                 // dx
                    pMessage.Append(0f);                 // dy
                    pMessage.Append(0f);                 // motion
                    pMessage.Append((float)pContact.Size.X);   // height
                    pMessage.Append((float)pContact.Size.Y);   // width

                    // Append it to the bundle.
                    pBundle.Append(pMessage);

                    // Append the alive message for this contact to tbe bundle.
                    pMessageAlive.Append((int)pContact.ID);
                }

                // Save the alive message.
                pBundle.Append(pMessageAlive);

                // Send the message off.
                this.pUDPWriter.Send(pBundle);
        }

        public event Action OnConnect;

        public event Action OnDisconnect;

        public void connect()
        {
            // Reconnect with the new API.
            pUDPWriter = new OSCTransmitter(WiiTUIO.Properties.Settings.Default.tuio_IP, WiiTUIO.Properties.Settings.Default.tuio_port);
            pUDPWriter.Connect();
            if (OnConnect != null)
            {
                OnConnect();
            }
        }

        public void disconnect()
        {
            if (pUDPWriter != null)
                pUDPWriter.Close();
            pUDPWriter = null;
            if (OnDisconnect != null)
            {
                OnDisconnect();
            }
        }


        public void showSettingsWindow()
        {
            this.settingsWindow.Show();
        }
    }
}
