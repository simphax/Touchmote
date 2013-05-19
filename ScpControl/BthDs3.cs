using System;
using System.ComponentModel;
using System.Threading;

namespace ScpControl 
{
    public partial class BthDs3 : BthConnection, IDs3Device 
    {
        protected ReportEventArgs m_ReportArgs = new ReportEventArgs();

        public event EventHandler<DebugEventArgs>  Debug  = null;
        public event EventHandler<ReportEventArgs> Report = null;

        protected Byte       m_Init = 0;
        protected Timer      m_Timer;
        protected Boolean    m_Blocked = false, m_IsIdle = true, m_IsDisconnect = false;
        protected UInt32     m_Queued = 0;
        protected DateTime   m_Last = DateTime.Now, m_Idle = DateTime.Now, m_Tick = DateTime.Now, m_Disconnect = DateTime.Now;
        protected IBthDevice m_Device;

        protected Byte[] m_Master = new Byte[6];

        protected Byte m_ControllerId  = 0;
        protected Byte m_BatteryStatus = 0;
        protected Byte m_CableStatus   = 0;
        protected Byte m_PlugStatus    = 0;

        protected DeviceState m_State = DeviceState.Disconnected;

        protected UInt32 m_Packet = 0;
        protected Byte[] m_Report = 
        {
            0x52, 0x01, 
            0x00, 0xFF, 0x00, 0xFF, 0x00, 
            0x00, 0x00, 0x00, 0x00, 0x00,
            0xFF, 0x27, 0x10, 0x00, 0x32,
            0xFF, 0x27, 0x10, 0x00, 0x32,
            0xFF, 0x27, 0x10, 0x00, 0x32,
            0xFF, 0x27, 0x10, 0x00, 0x32, 
	        0x00, 0x00, 0x00, 0x00, 0x00, 
	        0x00, 0x00, 0x00, 0x00,	0x00, 
	        0x00, 0x00, 0x00, 0x00, 0x00, 
	        0x00, 0x00, 0x00,
        };

        protected Byte[][] m_InitReport = new Byte[][]
        {
            new Byte[] { 0x02, 0x00, 0x0F, 0x00, 0x08, 0x35, 0x03, 0x19, 0x12, 0x00, 0x00, 0x03, 0x00 },
            new Byte[] { 0x04, 0x00, 0x10, 0x00, 0x0F, 0x00, 0x01, 0x00, 0x01, 0x00, 0x10, 0x35, 0x06, 0x09, 0x02, 0x01, 0x09, 0x02, 0x02, 0x00 },
            new Byte[] { 0x06, 0x00, 0x11, 0x00, 0x0D, 0x35, 0x03, 0x19, 0x11, 0x24, 0x01, 0x90, 0x35, 0x03, 0x09, 0x02, 0x06, 0x00 },
            new Byte[] { 0x06, 0x00, 0x12, 0x00, 0x0F, 0x35, 0x03, 0x19, 0x11, 0x24, 0x01, 0x90, 0x35, 0x03, 0x09, 0x02, 0x06, 0x02, 0x00, 0x7F },
            new Byte[] { 0x06, 0x00, 0x13, 0x00, 0x0F, 0x35, 0x03, 0x19, 0x11, 0x24, 0x01, 0x90, 0x35, 0x03, 0x09, 0x02, 0x06, 0x02, 0x00, 0x59 },
            new Byte[] { 0x06, 0x00, 0x14, 0x00, 0x0F, 0x35, 0x03, 0x19, 0x11, 0x24, 0x01, 0x80, 0x35, 0x03, 0x09, 0x02, 0x06, 0x02, 0x00, 0x33 },
            new Byte[] { 0x06, 0x00, 0x15, 0x00, 0x0F, 0x35, 0x03, 0x19, 0x11, 0x24, 0x01, 0x90, 0x35, 0x03, 0x09, 0x02, 0x06, 0x02, 0x00, 0x0D },
        };

        protected Byte[] m_Leds   = { 0x02, 0x04, 0x08, 0x10, };
        protected Byte[] m_Enable = { 0x53, 0xF4, 0x42, 0x03, 0x00, 0x00, };

        public DeviceState State 
        {
            get { return m_State; }
        }
        public Ds3PadId PadId 
        {
            get { return (Ds3PadId) m_ControllerId; }
            set 
            {
                m_ControllerId = (Byte) value;
                m_Report[11]   = m_Leds[m_ControllerId];

                m_ReportArgs.Pad = PadId;
            }
        }

        public String Local  
        {
            get { return String.Format("{0:X2}:{1:X2}:{2:X2}:{3:X2}:{4:X2}:{5:X2}", m_Local[5], m_Local[4], m_Local[3], m_Local[2], m_Local[1], m_Local[0]); }
        }
        public String Remote 
        {
            get { return String.Format("{0:X2}:{1:X2}:{2:X2}:{3:X2}:{4:X2}:{5:X2}", m_Master[0], m_Master[1], m_Master[2], m_Master[3], m_Master[4], m_Master[5]); }
        }

        public Ds3Connection Connection 
        {
            get { return Ds3Connection.BTH; }
        }
        public Ds3Battery    Battery    
        {
            get { return (Ds3Battery) m_BatteryStatus; }
        }


        protected virtual void Publish() 
        {
            m_ReportArgs.Report[0] = m_ControllerId;
            m_ReportArgs.Report[1] = (Byte) m_State;

            if (Report != null) Report(this, m_ReportArgs);
        }

        protected virtual void LogDebug(String Data) 
        {
            DebugEventArgs args = new DebugEventArgs(Data);

            if (Debug != null)
            {
                Debug(this, args);
            }
        }


        public BthDs3() 
        {
            InitializeComponent();
        }

        public BthDs3(IContainer container) 
        {
            container.Add(this);

            InitializeComponent();
        }

        public BthDs3(IBthDevice Device, Byte[] Master, Byte Lsb, Byte Msb) : base(new BthHandle(Lsb, Msb)) 
        {
            InitializeComponent();

            m_Device = Device;
            m_Master = Master;

            m_Timer = new Timer(On_Timer, null, Timeout.Infinite, Timeout.Infinite);
        }


        public virtual Boolean Start() 
        {
            CanStartHid = false;
            m_State = DeviceState.Connected;

            m_Queued = 3; m_Blocked = true; m_Last = DateTime.Now;
            m_Device.HID_Command(HCI_Handle.Bytes, Get_SCID(L2CAP.PSM.HID_Command), m_Enable);

            m_Timer.Change(16, 16);

            return m_State == DeviceState.Connected;
        }

        public virtual Boolean Stop()  
        {
            if (m_State == DeviceState.Connected)
            {
                m_Timer.Change(Timeout.Infinite, Timeout.Infinite);

                m_State = DeviceState.Reserved;
                m_Packet = 0;

                Publish();
            }

            return m_State == DeviceState.Reserved;
        }

        public virtual Boolean Close() 
        {
            if (m_State == DeviceState.Connected)
            {
                m_Timer.Change(Timeout.Infinite, Timeout.Infinite);
                m_Packet = 0;

                Publish();
            }

            m_State = DeviceState.Disconnected;

            return m_State == DeviceState.Disconnected;
        }


        public virtual void Parse(Byte[] Report) 
        {
            if (Report[10] == 0xFF) return;

            m_PlugStatus    = Report[38];
            m_BatteryStatus = Report[39];
            m_CableStatus   = Report[40];

            if (m_Packet == 0) Rumble(0, 0); m_Packet++;

            m_ReportArgs.Report[2] = m_BatteryStatus;
            m_ReportArgs.Report[3] = m_CableStatus;

            m_ReportArgs.Report[4] = (Byte)(m_Packet >>  0 & 0xFF);
            m_ReportArgs.Report[5] = (Byte)(m_Packet >>  8 & 0xFF);
            m_ReportArgs.Report[6] = (Byte)(m_Packet >> 16 & 0xFF);
            m_ReportArgs.Report[7] = (Byte)(m_Packet >> 24 & 0xFF);

            UInt32  Buttons = (UInt32)((Report[11] << 0) | (Report[12] << 8) | (Report[13] << 16) | (Report[14] << 24));
            Boolean Active = false, Trigger = false;

            if (((Buttons & (0x1 << 10)) > 0) && ((Buttons & (0x1 << 11)) > 0) && ((Buttons & (0x1 << 16)) > 0))
            {
                Trigger = true; Report[13] ^= 0x1;
            }

            for (Int32 Index = 8; Index < 58; Index++)
            {
                m_ReportArgs.Report[Index] = Report[Index + 1];
            }

            for (Int32 Index = 11; Index < 15 && !Active; Index++)
            {
                if (Report[Index] != 0) Active = true;
            }

            for (Int32 Index = 15; Index < 19 && !Active; Index++)
            {
                if (Report[Index] < 117 || Report[Index] > 137) Active = true;
            }

            for (Int32 Index = 23; Index < 35 && !Active; Index++)
            {
                if (Report[Index] != 0) Active = true;
            }

            if (Active)
            {
                m_IsIdle = false;
            }
            else if (!m_IsIdle)
            {
                m_IsIdle = true; m_Idle = DateTime.Now;
            }

            if (Trigger && !m_IsDisconnect)
            {
                m_IsDisconnect = true; m_Disconnect = DateTime.Now;
            }
            else if (!Trigger && m_IsDisconnect)
            {
                m_IsDisconnect = false;
            }

            Publish();
        }

        public virtual Boolean Rumble(Byte Left, Byte Right) 
        {
            lock (this)
            {
                m_Report[4] = (Byte)(Right > 0 ? 0x01 : 0x00);
                m_Report[6] = Left;

                m_Queued = 3;
            }
            return true;
        }

        public virtual Boolean Pair(byte[] Master) 
        {
            return false;
        }

        public virtual Boolean Disconnect() 
        {
            return m_Device.HCI_Disconnect(m_HCI_Handle) > 0;
        }


        public virtual Boolean InitReport(Byte[] Report) 
        {
            Boolean retVal = false;

            if (m_Init < 7)
            {
                m_Device.HID_Command(HCI_Handle.Bytes, Get_SCID(L2CAP.PSM.HID_Service), m_InitReport[m_Init++]);
            }
            else if (m_Init == 7)
            {
                m_Init++; retVal = true;
            }

            return retVal;
        }


        public override String ToString() 
        {
            switch (m_State)
            {
                case DeviceState.Disconnected:

                    return String.Format("Pad {0} : Disconnected", m_ControllerId + 1);

                case DeviceState.Reserved:

                    return String.Format("Pad {0} : {1} - Reserved", m_ControllerId + 1, Local);

                case DeviceState.Connected:

                    return String.Format("Pad {0} : {1} - {2} {3:X8} {4}", m_ControllerId + 1,
                        Local,
                        Connection,
                        m_Packet,
                        Battery
                        );
            }

            throw new Exception();
        }

        public virtual void Completed() 
        {
            lock (this)
            {
                m_Blocked = false;
            }
        }

        public virtual void On_Timer(object State) 
        {
            lock (this)
            {
                if (m_State == DeviceState.Connected)
                {
                    DateTime Now = DateTime.Now;

                    if (m_IsIdle && Global.IdleDisconnect)
                    {
                        if ((Now - m_Idle).TotalMilliseconds >= Global.IdleTimeout)
                        {
                            LogDebug("++ Idle Disconnect Triggered");

                            m_IsDisconnect = false;
                            m_IsIdle = false;

                            Disconnect();
                            return;
                        }
                    }
                    else if (m_IsDisconnect)
                    {
                        if ((Now - m_Disconnect).TotalMilliseconds >= 2000)
                        {
                            LogDebug("++ Quick Disconnect Triggered");

                            m_IsDisconnect = false;
                            m_IsIdle = false;

                            Disconnect();
                            return;
                        }
                    }

                    if ((Now - m_Tick).TotalMilliseconds >= 500 && m_Packet > 0)
                    {
                        m_Tick = Now;

                        if (m_Queued == 0) m_Queued = 1;

                        if (Battery < Ds3Battery.Medium)
                        {
                            m_Report[11] ^= m_Leds[m_ControllerId];
                        }
                        else
                        {
                            m_Report[11] |= m_Leds[m_ControllerId];
                        }
                    }

                    if (Global.DisableLED) m_Report[11] = 0;

                    if (!m_Blocked && m_Queued > 0)
                    {
                        if ((Now - m_Last).TotalMilliseconds >= 125)
                        {
                            m_Last = Now; m_Blocked = true; m_Queued--;

                            m_Device.HID_Command(HCI_Handle.Bytes, Get_SCID(L2CAP.PSM.HID_Command), m_Report);
                        }
                    }
                }
            }
        }
    }
}
