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
    public class TUIOProviderHandler :ITouchProviderHandler
    {

        private Queue<WiiContact> contactQueue;

        private static int iFrame = 0;

        /// <summary>
        /// A reference to an OSC data transmitter.
        /// </summary>
        private OSCTransmitter pUDPWriter = null;

        public TUIOProviderHandler()
        {
            /* old stuff
            this.settingsWindow = new TUIOSettings(this);
            this.settingsWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.settingsWindow.Hide();
             * */
            contactQueue = new Queue<WiiContact>();
        }
        
        public void processEventFrame()
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
                WiiContact contact;
                while (contactQueue.Count > 0)
                {
                    contact = contactQueue.Dequeue();
                    if ((contact.Type == ContactType.Hover || contact.Type == ContactType.EndFromHover))
                    {
                        //No hover yet
                    }
                    else
                    {
                        // Compile the set message.
                        OSCMessage pMessage = new OSCMessage("/tuio/2Dcur");
                        pMessage.Append("set");                 // set
                        pMessage.Append((int)contact.ID);           // session
                        pMessage.Append((float)contact.NormalPosition.X);   // x
                        pMessage.Append((float)contact.NormalPosition.Y);   // y
                        pMessage.Append(0f);                 // dx
                        pMessage.Append(0f);                 // dy
                        pMessage.Append(0f);                 // motion
                        pMessage.Append((float)contact.Size.X);   // height
                        pMessage.Append((float)contact.Size.Y);   // width

                        // Append it to the bundle.
                        pBundle.Append(pMessage);

                        // Append the alive message for this contact to tbe bundle.
                        pMessageAlive.Append((int)contact.ID);
                    }
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

        public void queueContact(WiiContact contact)
        {
            contactQueue.Enqueue(contact);
        }
    }
}
