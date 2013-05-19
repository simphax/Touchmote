using System;
using System.ComponentModel;
using System.Threading;

namespace ScpControl 
{
    public partial class UsbDevice : ScpDevice, IDs3Device 
    {
        public static String DS3_USB_CLASS_GUID = "{E2824A09-DBAA-4407-85CA-C8E8FF5F6FFA}";

        protected ReportEventArgs m_ReportArgs = new ReportEventArgs();

        protected String   m_Instance = String.Empty;
        protected Timer    m_Timer;
        protected Boolean  m_IsDisconnect = false;
        protected DateTime m_Tick = DateTime.Now, m_Disconnect = DateTime.Now;

        public event EventHandler<DebugEventArgs>  Debug  = null;
        public event EventHandler<ReportEventArgs> Report = null;

        protected Byte[] m_Buffer = new Byte[64];
        protected Byte[] m_Master = new Byte[6];
        protected Byte[] m_Local  = new Byte[6];

        protected Byte m_ControllerId  = 0;
        protected Byte m_BatteryStatus = 0;
        protected Byte m_CableStatus   = 0;
        protected Byte m_PlugStatus    = 0;

        protected DeviceState m_State = DeviceState.Disconnected;

        protected UInt32 m_Packet = 0;
        protected Byte[] m_Report = 
        {
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

        protected Byte[] m_Leds   = { 0x02, 0x04, 0x08, 0x10 };
        protected Byte[] m_Enable = { 0x42, 0x0C, 0x00, 0x00 };

        public DeviceState State 
        {
            get { return (DeviceState) m_State; }
        }
        public Ds3PadId PadId 
        {
            get { return (Ds3PadId) m_ControllerId; }
            set 
            {
                m_ControllerId = (Byte) value;
                m_Report[9]    = m_Leds[m_ControllerId];

                m_ReportArgs.Pad = PadId;
            }
        }

        public String Local  
        {
            get { return String.Format("{0:X2}:{1:X2}:{2:X2}:{3:X2}:{4:X2}:{5:X2}", m_Local[0], m_Local[1], m_Local[2], m_Local[3], m_Local[4], m_Local[5]); }
        }
        public String Remote 
        {
            get { return String.Format("{0:X2}:{1:X2}:{2:X2}:{3:X2}:{4:X2}:{5:X2}", m_Master[0], m_Master[1], m_Master[2], m_Master[3], m_Master[4], m_Master[5]); }
        }

        public Ds3Connection Connection 
        {
            get { return Ds3Connection.USB; }
        }
        public Ds3Battery    Battery    
        {
            get { return (Ds3Battery) m_BatteryStatus; }
        }

        public Boolean IsShutdown 
        {
            get { return m_IsDisconnect; }
            set { m_IsDisconnect = value; }
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

        protected UsbDevice(String Guid) : base(Guid) 
        {
            InitializeComponent();
        }


        public UsbDevice() : base(DS3_USB_CLASS_GUID) 
        {
            InitializeComponent();

            m_Timer = new Timer(On_Timer, null, Timeout.Infinite, Timeout.Infinite);
        }

        public UsbDevice(IContainer container) : base(DS3_USB_CLASS_GUID) 
        {
            container.Add(this);

            InitializeComponent();

            m_Timer = new Timer(On_Timer, null, Timeout.Infinite, Timeout.Infinite);
        }


        public override Boolean Open(int Instance = 0)  
        {
            if (base.Open(Instance))
            {
                m_State = DeviceState.Reserved;

                Int32 Transfered = 0;

                if (SendTransfer(0xA1, 0x01, 0x03F5, m_Buffer, ref Transfered))
                {
                    m_Master = new Byte[] { m_Buffer[2], m_Buffer[3], m_Buffer[4], m_Buffer[5], m_Buffer[6], m_Buffer[7] };
                }

                if (SendTransfer(0xA1, 0x01, 0x03F2, m_Buffer, ref Transfered))
                {
                    m_Local = new Byte[] { m_Buffer[4], m_Buffer[5], m_Buffer[6], m_Buffer[7], m_Buffer[8], m_Buffer[9] };
                }
            }

            return State == DeviceState.Reserved;
        }

        public override Boolean Open(String DevicePath) 
        {
            if (base.Open(DevicePath))
            {
                m_State = DeviceState.Reserved;
                GetDeviceInstance(ref m_Instance);

                Int32 Transfered = 0;

                if (SendTransfer(0xA1, 0x01, 0x03F5, m_Buffer, ref Transfered))
                {
                    m_Master = new Byte[] { m_Buffer[2], m_Buffer[3], m_Buffer[4], m_Buffer[5], m_Buffer[6], m_Buffer[7] };
                }

                if (SendTransfer(0xA1, 0x01, 0x03F2, m_Buffer, ref Transfered))
                {
                    m_Local = new Byte[] { m_Buffer[4], m_Buffer[5], m_Buffer[6], m_Buffer[7], m_Buffer[8], m_Buffer[9] };
                }

                LogDebug(String.Format("-- Opened Device - Local [{0}] Remote [{1}]", Local, Remote));
            }

            return State == DeviceState.Reserved;
        }

        public override Boolean Start() 
        {
            if (IsActive)
            {
                Int32 Transfered = 0;

                if (SendTransfer(0x21, 0x09, 0x03F4, m_Enable, ref Transfered))
                {
                    m_State  = DeviceState.Connected;
                    m_Packet = 0;

                    HID_Worker.RunWorkerAsync();

                    LogDebug(String.Format("-- Started Device Instance [{0}]", m_Instance));
                }
            }

            m_Timer.Change(16, 16);

            return State == DeviceState.Connected;
        }

        public override Boolean Stop()  
        {
            if (IsActive)
            {
                m_Timer.Change(Timeout.Infinite, Timeout.Infinite);
                m_State = DeviceState.Reserved;

                Publish();
            }

            return base.Stop();
        }

        public override Boolean Close() 
        {
            if (IsActive)
            {
                m_State = DeviceState.Disconnected;

                Publish();
            }

            return base.Close();
        }


        public virtual void Parse(Byte[] Report) 
        {
            if (Report[0] != 0x01) return;

            m_PlugStatus    = Report[29];
            m_BatteryStatus = Report[30];
            m_CableStatus   = Report[31];

            if (m_Packet++ == 0) Rumble(0, 0);

            m_ReportArgs.Report[2] = m_BatteryStatus;
            m_ReportArgs.Report[3] = m_CableStatus;

            m_ReportArgs.Report[4] = (Byte)(m_Packet >>  0 & 0xFF);
            m_ReportArgs.Report[5] = (Byte)(m_Packet >>  8 & 0xFF);
            m_ReportArgs.Report[6] = (Byte)(m_Packet >> 16 & 0xFF);
            m_ReportArgs.Report[7] = (Byte)(m_Packet >> 24 & 0xFF);

            UInt32 Buttons = (UInt32)((Report[2] << 0) | (Report[3] << 8) | (Report[4] << 16) | (Report[5] << 24));
            Boolean Trigger = false;

            if (((Buttons & (0x1 << 10)) > 0) && ((Buttons & (0x1 << 11)) > 0) && ((Buttons & (0x1 << 16)) > 0))
            {
                Trigger = true; Report[4] ^= 0x1;
            }

            for (int Index = 8; Index < 58; Index++)
            {
                m_ReportArgs.Report[Index] = Report[Index - 8];
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

        public virtual Boolean Pair(Byte[] Master) 
        {
            Int32 Transfered = 0; Byte[] Buffer = { 0x00, 0x00, Master[0], Master[1], Master[2], Master[3], Master[4], Master[5] };

            if (SendTransfer(0x21, 0x09, 0x03F5, Buffer, ref Transfered))
            {
                for (Int32 Index = 0; Index < m_Master.Length; Index++)
                {
                    m_Master[Index] = Master[Index];
                }

                LogDebug(String.Format("++ Paired DS3 [{0}] To BTH Dongle [{1}]", Local, Remote));
                return true;
            }

            LogDebug(String.Format("++ Pair Failed [{0}]", Local));
            return false;
        }

        public virtual Boolean Rumble(Byte Left, Byte Right) 
        {
            lock (this)
            {
                Int32 Transfered = 0;

                m_Report[2] = (Byte)(Right > 0 ? 0x01 : 0x00);
                m_Report[4] = (Byte)(Left);
                m_Report[9] = (Byte)(Global.DisableLED ? 0 : m_Leds[m_ControllerId]);

                return SendTransfer(0x21, 0x09, 0x0201, m_Report, ref Transfered);
            }
        }

        public virtual Boolean Disconnect() 
        {
            return true;
        }


        public override String ToString() 
        {
            switch ((DeviceState) m_State)
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

        protected virtual void HID_Worker_Thread(object sender, DoWorkEventArgs e) 
        {
            Int32 Transfered = 0;
            Byte[] Buffer = new Byte[64];

            LogDebug("-- USB Device : HID_Worker_Thread Starting");

            while (IsActive)
            {
                try
                {
                    if (ReadIntPipe(Buffer, Buffer.Length, ref Transfered) && Transfered > 0)
                    {
                        Parse(Buffer);
                    }
                }
                catch { }
            }

            LogDebug("-- USB Device : HID_Worker_Thread Exiting");
        }

        protected virtual Boolean Shutdown() 
        {
            Stop();

            return RestartDevice(m_Instance);
        }

        public virtual void On_Timer(object State) 
        {
            lock (this)
            {
                DateTime Now = DateTime.Now;

                if (m_IsDisconnect)
                {
                    if ((Now - m_Disconnect).TotalMilliseconds >= 2000)
                    {
                        LogDebug("++ Quick Disconnect Triggered");

                        Shutdown();
                        return;
                    }
                }

                if ((Now - m_Tick).TotalMilliseconds >= 1500 && m_Packet > 0)
                {
                    Int32 Transfered = 0;

                    m_Tick = Now;

                    if (Battery == Ds3Battery.Charging)
                    {
                        m_Report[9] ^= m_Leds[m_ControllerId];
                    }
                    else
                    {
                        m_Report[9] |= m_Leds[m_ControllerId];
                    }

                    if (Global.DisableLED) m_Report[9] = 0;

                    SendTransfer(0x21, 0x09, 0x0201, m_Report, ref Transfered);
                }
            }
        }
    }
}
