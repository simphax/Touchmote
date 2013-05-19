using System;
using System.ComponentModel;
using System.Text;

using System.Net;
using System.Net.Sockets;
using System.Threading;

using System.Management;
using System.Text.RegularExpressions;
using System.Reflection;

namespace ScpControl 
{
    public partial class RootHub : Component 
    {
        protected volatile Boolean m_Started = false, m_Suspended = false;

        protected BthHub    bthHub = new BthHub();
        protected UsbHub    usbHub = new UsbHub();
        protected BusDevice scpBus = new BusDevice();

        protected Byte[][] m_XInput = { new Byte[2] { 0, 0 }, new Byte[2] { 0, 0 }, new Byte[2] { 0, 0 }, new Byte[2] { 0, 0 }};
        protected Byte[][] m_Native = { new Byte[2] { 0, 0 }, new Byte[2] { 0, 0 }, new Byte[2] { 0, 0 }, new Byte[2] { 0, 0 }};

        protected IDs3Device[] m_Pad = { new Ds3Null(Ds3PadId.One), new Ds3Null(Ds3PadId.Two), new Ds3Null(Ds3PadId.Three), new Ds3Null(Ds3PadId.Four) };
        protected String[] m_Reserved = new String[] { String.Empty, String.Empty, String.Empty, String.Empty };

        protected IPEndPoint m_ServerEp = new IPEndPoint(IPAddress.Loopback, 26760);
        protected UdpClient  m_Server   = new UdpClient();

        protected IPEndPoint m_ClientEp = new IPEndPoint(IPAddress.Loopback, 26761);
        protected UdpClient  m_Client   = new UdpClient();

        public event EventHandler<DebugEventArgs> Debug = null;

        protected virtual Boolean LogDebug(String Data) 
        {
            DebugEventArgs args = new DebugEventArgs(Data);

            On_Debug(this, args);

            return true;
        }

        public IDs3Device[] Pad 
        {
            get { return m_Pad; }
        }

        public String  Dongle   
        {
            get { return bthHub.Dongle; }
        }
        public String  Master   
        {
            get { return bthHub.Master; }
        }
        public Boolean Pairable 
        {
            get { return m_Started && bthHub.Pairable; }
        }


        public RootHub() 
        {
            InitializeComponent();

            bthHub.Debug += new EventHandler<DebugEventArgs>(On_Debug);
            usbHub.Debug += new EventHandler<DebugEventArgs>(On_Debug);

            bthHub.Arrival += new EventHandler<ArrivalEventArgs>(On_Arrival);
            usbHub.Arrival += new EventHandler<ArrivalEventArgs>(On_Arrival);

            bthHub.Report += new EventHandler<ReportEventArgs>(On_Report);
            usbHub.Report += new EventHandler<ReportEventArgs>(On_Report);
        }

        public RootHub(IContainer container) 
        {
            container.Add(this);
            InitializeComponent();

            bthHub.Debug += new EventHandler<DebugEventArgs>(On_Debug);
            usbHub.Debug += new EventHandler<DebugEventArgs>(On_Debug);

            bthHub.Arrival += new EventHandler<ArrivalEventArgs>(On_Arrival);
            usbHub.Arrival += new EventHandler<ArrivalEventArgs>(On_Arrival);

            bthHub.Report += new EventHandler<ReportEventArgs>(On_Report);
            usbHub.Report += new EventHandler<ReportEventArgs>(On_Report);
        }


        public virtual Boolean Open()  
        {
            bool Opened = false;

            LogDebug(String.Format("++ {0} {1}", Assembly.GetExecutingAssembly().Location, Assembly.GetExecutingAssembly().GetName().Version.ToString()));
            LogDebug(String.Format("++ {0}", OSInfo()));

            Opened |= scpBus.Open();
            Opened |= usbHub.Open();
            Opened |= bthHub.Open();

            Global.Load();
            return Opened;
        }

        public virtual Boolean Start() 
        {
            if (!m_Started)
            {
                m_Started |= scpBus.Start();
                m_Started |= usbHub.Start();
                m_Started |= bthHub.Start();

                if (m_Started) UDP_Worker.RunWorkerAsync();
            }

            return m_Started;
        }

        public virtual Boolean Stop()  
        {
            if (m_Started)
            {
                m_Started = false;
                m_Server.Close();

                usbHub.Stop();
                bthHub.Stop();
                scpBus.Stop();
            }

            return !m_Started;
        }

        public virtual Boolean Close() 
        {
            if (m_Started)
            {
                m_Started = false;
                m_Server.Close();

                usbHub.Close();
                bthHub.Close();
                scpBus.Close();
            }

            Global.Save();

            return !m_Started;
        }


        public virtual Boolean Suspend() 
        {
            m_Suspended = true;

            for (Int32 Index = 0; Index < m_Pad.Length; Index++) m_Pad[Index].Disconnect();

            scpBus.Unplug(0);

            usbHub.Suspend();
            bthHub.Suspend();

            LogDebug("++ Suspended");
            return true;
        }

        public virtual Boolean Resume()  
        {
            LogDebug("++ Resumed");

            for (Int32 Index = 0; Index < m_Pad.Length; Index++)
            {
                if (m_Pad[Index].State != DeviceState.Disconnected)
                {
                    scpBus.Plugin(Index + 1);
                }
            }

            usbHub.Resume();
            bthHub.Resume();

            m_Suspended = false;
            return true;
        }


        public virtual Ds3PadId Notify(ScpDevice.Notified Notification, String Class, String Path) 
        {
            if (!m_Suspended)
            {
                if (Class == UsbDevice.DS3_USB_CLASS_GUID)
                {
                    return usbHub.Notify(Notification, Class, Path);
                }

                if (Class == BthDevice.DS3_BTH_CLASS_GUID)
                {
                    bthHub.Notify(Notification, Class, Path);
                }
            }

            return Ds3PadId.None;
        }

        protected virtual void UDP_Worker_Thread(object sender, DoWorkEventArgs e)  
        {
            Byte Serial;

            Thread.Sleep(1);

            IPEndPoint Remote = new IPEndPoint(IPAddress.Any, 0);

            m_Server = new UdpClient(m_ServerEp);

            LogDebug("-- Controller : UDP_Worker_Thread Starting");

            while (m_Started)
            {
                try
                {
                    Byte[] Buffer = m_Server.Receive(ref Remote);

                    switch (Buffer[1])
                    {
                        case 0x00:

                            LogDebug(String.Format("-- Received : Status Request [{0}]", Remote));

                            Buffer[2] = (Byte) Pad[0].State;
                            Buffer[3] = (Byte) Pad[1].State;
                            Buffer[4] = (Byte) Pad[2].State;
                            Buffer[5] = (Byte) Pad[3].State;

                            m_Server.Send(Buffer, Buffer.Length, Remote);
                            break;

                        case 0x01:

                            Serial = Buffer[0];

                            if (Pad[Serial].State == DeviceState.Connected)
                            {
                                if (Buffer[2] != m_Native[Serial][0] || Buffer[3] != m_Native[Serial][1])
                                {
                                    m_Native[Serial][0] = Buffer[2];
                                    m_Native[Serial][1] = Buffer[3];

                                    Pad[Buffer[0]].Rumble(Buffer[2], Buffer[3]);
                                }
                            }
                            break;

                        case 0x02:
                            {
                                StringBuilder sb = new StringBuilder();

                                sb.Append(Dongle); sb.Append('^');

                                sb.Append(Pad[0].ToString()); sb.Append('^');
                                sb.Append(Pad[1].ToString()); sb.Append('^');
                                sb.Append(Pad[2].ToString()); sb.Append('^');
                                sb.Append(Pad[3].ToString()); sb.Append('^');

                                Byte[] Data = Encoding.Unicode.GetBytes(sb.ToString());

                                m_Server.Send(Data, Data.Length, Remote);
                            }
                            break;

                        case 0x03:
                            {
                                LogDebug(String.Format("-- Received : Setting Request [{0}]", Remote));

                                Byte[] Data = Global.Packed;

                                m_Server.Send(Data, Data.Length, Remote);
                            }
                            break;

                        case 0x04:
                            {
                                LogDebug(String.Format("-- Received : Setting Config [{0}] [{1},{2},{3},{4},{5},{6}]", Remote, Buffer[2], Buffer[3], Buffer[4], Buffer[5], Buffer[6], Buffer[7]));

                                Global.Packed = Buffer;
                            }
                            break;
                    }
                }
                catch { }
            }

            LogDebug("-- Controller : UDP_Worker_Thread Exiting");
        }

        protected virtual void On_Debug  (object sender, DebugEventArgs e)   
        {
            if (Debug != null) Debug(sender, e);
        }

        protected virtual void On_Arrival(object sender, ArrivalEventArgs e) 
        {
            Boolean    bFound = false;
            IDs3Device Arrived = e.Device;

            for (Int32 Index = 0; Index < m_Pad.Length && !bFound; Index++)
            {
                if (Arrived.Local == m_Reserved[Index])
                {
                    if (m_Pad[Index].State == DeviceState.Connected)
                    {
                        if (m_Pad[Index].Connection == Ds3Connection.BTH)
                        {
                            m_Pad[Index].Disconnect();
                        }
                        
                        if (m_Pad[Index].Connection == Ds3Connection.USB)
                        {
                            Arrived.Disconnect();

                            e.Handled = false;
                            return;
                        }
                    }

                    bFound = true;

                    Arrived.PadId = (Ds3PadId) Index;
                    m_Pad[Index]  = Arrived;
                }
            }

            for (Int32 Index = 0; Index < m_Pad.Length && !bFound; Index++)
            {
                if (m_Pad[Index].State == DeviceState.Disconnected)
                {
                    bFound = true;
                    m_Reserved[Index] = Arrived.Local;

                    Arrived.PadId = (Ds3PadId) Index;
                    m_Pad[Index]  = Arrived;
                }
            }

            if (bFound) scpBus.Plugin((int) Arrived.PadId + 1);
            e.Handled = bFound;
        }

        protected virtual void On_Report (object sender, ReportEventArgs e)  
        {
            Byte[] Rumble = new Byte[ 8];
            Byte[] Report = new Byte[28];

            Int32 Serial = scpBus.Parse(e.Report, Report);

            if (scpBus.Report(Report, Rumble) && (DeviceState) e.Report[1] == DeviceState.Connected)
            {
                Byte Big   = (Byte)(Rumble[3]);
                Byte Small = (Byte)(Rumble[4] > 0 ? 1 : 0);

                if (Rumble[1] == 0x08 && (Big != m_XInput[Serial][0] || Small != m_XInput[Serial][1]))
                {
                    m_XInput[Serial][0] = Big;
                    m_XInput[Serial][1] = Small;

                    Pad[Serial].Rumble(Big, Small);
                }
            }

            if ((DeviceState) e.Report[1] != DeviceState.Connected)
            {
                m_XInput[Serial][0] = m_XInput[Serial][1] = 0;
                m_Native[Serial][0] = m_Native[Serial][1] = 0;
            }

            m_Client.Send(e.Report, e.Report.Length, m_ClientEp);
        }


        protected String OSInfo() 
        {
            String Info = String.Empty;

            try
            {
                using (ManagementObjectSearcher mos = new ManagementObjectSearcher("SELECT * FROM  Win32_OperatingSystem"))
                {
                    foreach (ManagementObject mo in mos.Get())
                    {
                        try
                        {
                            Info = Regex.Replace(mo.GetPropertyValue("Caption").ToString(), "[^A-Za-z0-9 ]", "").Trim();

                            try
                            {
                                Object spv = mo.GetPropertyValue("ServicePackMajorVersion");

                                if (spv != null && spv.ToString() != "0")
                                {
                                    Info += " Service Pack " + spv.ToString();
                                }
                            }
                            catch { }

                            Info = String.Format("{0} ({1} {2})", Info, System.Environment.OSVersion.Version.ToString(), System.Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE"));

                        }
                        catch { }

                        mo.Dispose();
                    }
                }
            }
            catch { }

            return Info;
        }
    }
}
