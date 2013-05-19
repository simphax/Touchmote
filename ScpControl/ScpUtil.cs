using System;
using System.Collections.Generic;
using System.Text;

using System.IO;
using System.Reflection;
using System.Xml;

namespace ScpControl 
{
    public enum DeviceState       { Disconnected = 0x00, Reserved = 0x01, Connected = 0x02 };
    public enum Ds3Connection     { None = 0x00, USB = 0x01, BTH = 0x02 };
    public enum Ds3Battery : byte { None = 0x00, Dead = 0x01, Low = 0x02, Medium = 0x03, High = 0x04, Full = 0x05, Charging = 0xEE, Charged = 0xEF };
    public enum Ds3PadId :   byte { None = 0xFF, One = 0x00, Two = 0x01, Three = 0x02, Four = 0x03, All = 0x04 };

    public interface IDs3Device 
    {
        Ds3PadId PadId 
        {
            get;
            set;
        }

        Ds3Connection Connection 
        {
            get;
        }

        DeviceState State 
        {
            get;
        }

        Ds3Battery Battery 
        {
            get;
        }

        String Local 
        {
            get;
        }

        String Remote 
        {
            get;
        }

        Boolean Start();

        Boolean Rumble(Byte Big, Byte Small);

        Boolean Pair(Byte[] Master);

        Boolean Disconnect();
    }

    public interface IBthDevice 
    {
        Int32 HCI_Disconnect(BthHandle Handle);

        Int32 HID_Command(Byte[] Handle, Byte[] Channel, Byte[] Data);
    }
        
    public class BthHandle : IEquatable<BthHandle>, IComparable<BthHandle> 
    {
        protected Byte[] m_Handle = new Byte[2] { 0x00, 0x00 };
        protected UInt16 m_Value;

        public BthHandle(Byte Lsb, Byte Msb) 
        {
            m_Handle[0] = Lsb;
            m_Handle[1] = Msb;

            m_Value = (UInt16)(m_Handle[0] | (UInt16)(m_Handle[1] << 8));
        }

        public BthHandle(Byte[] Handle) : this(Handle[0], Handle[1]) 
        {
        }

        public BthHandle(UInt16 Short) : this((Byte)((Short >> 0) & 0xFF), (Byte)((Short >> 8) & 0xFF)) 
        {
        }

        public virtual Byte[] Bytes 
        {
            get { return m_Handle; }
        }

        public virtual UInt16 Short 
        {
            get { return m_Value; }
        }

        public override String ToString() 
        {
            return String.Format("{0:X4}", m_Value);
        }

        #region IEquatable<BthHandle> Members

        public virtual bool Equals(BthHandle other) 
        {
            return m_Value == other.m_Value;
        }

        public virtual bool Equals(Byte Lsb, Byte Msb) 
        {
            return m_Handle[0] == Lsb && m_Handle[1] == Msb;
        }

        public virtual bool Equals(Byte[] other) 
        {
            return Equals(other[0], other[1]);
        }

        #endregion

        #region IComparable<BthHandle> Members

        public virtual int CompareTo(BthHandle other) 
        {
            return m_Value.CompareTo(other.m_Value);
        }

        #endregion
    }

    public class Ds3Null : IDs3Device 
    {
        protected Ds3PadId m_PadId = Ds3PadId.None;

        public Ds3Null(Ds3PadId PadId) 
        {
            m_PadId = PadId;
        }

        public Ds3PadId PadId 
        {
            get { return m_PadId; }
            set { m_PadId = value; }
        }

        public Ds3Connection Connection 
        {
            get { return Ds3Connection.None; }
        }

        public DeviceState State 
        {
            get { return DeviceState.Disconnected; }
        }

        public Ds3Battery Battery 
        {
            get { return Ds3Battery.None; }
        }

        public string Local 
        {
            get { return "00:00:00:00:00:00"; }
        }

        public string Remote 
        {
            get { return "00:00:00:00:00:00"; }
        }

        public bool Start() 
        {
            return true;
        }

        public bool Rumble(Byte Left, Byte Right) 
        {
            return true;
        }

        public bool Pair(Byte[] Master) 
        {
            return true;
        }

        public bool Disconnect() 
        {
            return true;
        }

        public override String ToString() 
        {
            return String.Format("Pad {0} : {1}", 1 + (Int32) PadId, DeviceState.Disconnected);
        }
    }

    public class ArrivalEventArgs : EventArgs 
    {
        protected IDs3Device m_Device = null;
        protected Boolean m_Handled = false;

        public ArrivalEventArgs(IDs3Device Device) 
        {
            m_Device = Device;
        }

        public IDs3Device Device 
        {
            get { return m_Device; }
            set { m_Device = value; }
        }

        public Boolean Handled 
        {
            get { return m_Handled; }
            set { m_Handled = value; }
        }
    }

    public class DebugEventArgs   : EventArgs 
    {
        protected DateTime m_Time = DateTime.Now;
        protected String m_Data = String.Empty;

        public DebugEventArgs(String Data) 
        {
            m_Data = Data;
        }

        public DateTime Time 
        {
            get { return m_Time; }
        }

        public String Data 
        {
            get { return m_Data; }
        }
    }

    public class ReportEventArgs  : EventArgs 
    {
        protected Ds3PadId m_Pad = Ds3PadId.None;
        protected Byte[] m_Report = new Byte[64];

        public ReportEventArgs() 
        {
        }

        public ReportEventArgs(Ds3PadId Pad) 
        {
            m_Pad = Pad;
        }

        public Ds3PadId Pad 
        {
            get { return m_Pad; }
            set { m_Pad = value; }
        }

        public Byte[] Report 
        {
            get { return m_Report; }
        }
    }

    public class Global 
    {
        protected static BackingStore m_Config = new BackingStore();

        protected static Int32 m_IdleTimeout = 600000;

        public static Boolean FlipLX 
        {
            get { return m_Config.LX; }
            set { m_Config.LX = value; }
        }

        public static Boolean FlipLY 
        {
            get { return m_Config.LY; }
            set { m_Config.LY = value; }
        }

        public static Boolean FlipRX 
        {
            get { return m_Config.RX; }
            set { m_Config.RX = value; }
        }

        public static Boolean FlipRY 
        {
            get { return m_Config.RY; }
            set { m_Config.RY = value; }
        }

        public static Boolean DisableLED 
        {
            get { return m_Config.LED; }
            set { m_Config.LED = value; }
        }

        public static Boolean IdleDisconnect 
        {
            get { return m_Config.Idle != 0; }
        }

        public static Int32 IdleTimeout 
        {
            get { return m_Config.Idle; }
            set { m_Config.Idle = value * 60000; }
        }

        public static Byte[] Packed 
        {
            get 
            {
                Byte[] Buffer = new Byte[8];

                Buffer[1] = 0x03;
                Buffer[2] = (Byte)(IdleTimeout / 60000);
                Buffer[3] = (Byte)(FlipLX ? 0x01 : 0x00);
                Buffer[4] = (Byte)(FlipLY ? 0x01 : 0x00);
                Buffer[5] = (Byte)(FlipRX ? 0x01 : 0x00);
                Buffer[6] = (Byte)(FlipRY ? 0x01 : 0x00);
                Buffer[7] = (Byte)(DisableLED ? 0x01 : 0x00);

                return Buffer;
            }
            set 
            {
                IdleTimeout = value[2];
                FlipLX = value[3] == 0x01;
                FlipLY = value[4] == 0x01;
                FlipRX = value[5] == 0x01;
                FlipRY = value[6] == 0x01;
                DisableLED = value[7] == 0x01;
            }
        }

        public static void Load() 
        {
            m_Config.Load();
        }

        public static void Save() 
        {
            m_Config.Save();
        }
    }

    public class HCI 
    {
        public enum Event : byte 
        {
            HCI_Inquiry_Complete_EV                         = 0x01,
            HCI_Inquiry_Result_EV                           = 0x02,
            HCI_Connection_Complete_EV                      = 0x03,
            HCI_Connection_Request_EV                       = 0x04,
            HCI_Disconnection_Complete_EV                   = 0x05,
            HCI_Authentication_Complete_EV                  = 0x06,
            HCI_Remote_Name_Request_Complete_EV             = 0x07,
            HCI_Encryption_Change_EV                        = 0x08,
            HCI_Change_Connection_Link_Key_Complete_EV      = 0x09,
            HCI_Master_Link_Key_Complete_EV                 = 0x0A,
            HCI_Read_Remote_Supported_Features_Complete_EV  = 0x0B,
            HCI_Read_Remote_Version_Information_Complete_EV = 0x0C,
            HCI_QoS_Setup_Complete_EV                       = 0x0D,
            HCI_Command_Complete_EV                         = 0x0E,
            HCI_Command_Status_EV                           = 0x0F,
            HCI_Hardware_Error_EV                           = 0x10,
            HCI_Flush_Occurred_EV                           = 0x11,
            HCI_Role_Change_EV                              = 0x12,
            HCI_Number_Of_Completed_Packets_EV              = 0x13,
            HCI_Mode_Change_EV                              = 0x14,
            HCI_Return_Link_Keys_EV                         = 0x15,
            HCI_PIN_Code_Request_EV                         = 0x16,
            HCI_Link_Key_Request_EV                         = 0x17,
            HCI_Link_Key_Notification_EV                    = 0x18,
            HCI_Loopback_Command_EV                         = 0x19,
            HCI_Data_Buffer_Overflow_EV                     = 0x1A,
            HCI_Max_Slots_Change_EV                         = 0x1B,
            HCI_Read_Clock_Offset_Complete_EV               = 0x1C,
            HCI_Connection_Packet_Type_Changed_EV           = 0x1D,
            HCI_QoS_Violation_EV                            = 0x1E,
            HCI_Page_Scan_Repetition_Mode_Change_EV         = 0x20,
            HCI_Flow_Specification_Complete_EV              = 0x21,
            HCI_Inquiry_Result_With_RSSI_EV                 = 0x22,
            HCI_Read_Remote_Extended_Features_Complete_EV   = 0x23,
            HCI_Synchronous_Connection_Complete_EV          = 0x2C,
            HCI_Synchronous_Connection_Changed_EV           = 0x2D,
        }

        public enum Command : ushort 
        {
            HCI_Accept_Connection_Request   = 0x0409,
            HCI_Reject_Connection_Request   = 0x040A,
            HCI_Remote_Name_Request         = 0x0419,
            HCI_Reset                       = 0x0C03,
            HCI_Write_Scan_Enable           = 0x0C1A,
            HCI_Read_Buffer_Size            = 0x1005,
            HCI_Read_BD_ADDR                = 0x1009,
            HCI_Read_Local_Version_Info     = 0x1001,
            HCI_Disconnect                  = 0x0406,
        }
    }

    public class L2CAP 
    {
        public enum PSM 
        {
            HID_Service   = 0x01,
            HID_Command   = 0x11,
            HID_Interrupt = 0x13,
        }

        public enum Code : byte 
        {
            L2CAP_Reserved               = 0x00,
            L2CAP_Command_Reject         = 0x01,
            L2CAP_Connection_Request     = 0x02,
            L2CAP_Connection_Response    = 0x03,
            L2CAP_Configuration_Request  = 0x04,
            L2CAP_Configuration_Response = 0x05,
            L2CAP_Disconnection_Request  = 0x06,
            L2CAP_Disconnection_Response = 0x07,
            L2CAP_Echo_Request           = 0x08,
            L2CAP_Echo_Response          = 0x09,
            L2CAP_Information_Request    = 0x0A,
            L2CAP_Information_Response   = 0x0B,
        }
    }

    public class BackingStore 
    {
        protected String m_File = Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName + @"\ScpControl.xml";
        protected XmlDocument m_Xdoc = new XmlDocument();

        public Boolean Load() 
        {
            Boolean Loaded = true;

            try
            {
                XmlNode Item;

                m_Xdoc.Load(m_File);

                try { Item = m_Xdoc.SelectSingleNode("/ScpControl/Idle"); Int32.TryParse(Item.InnerText, out m_Idle); }
                catch { }

                try { Item = m_Xdoc.SelectSingleNode("/ScpControl/LX"); Boolean.TryParse(Item.InnerText, out m_LX); }
                catch { }

                try { Item = m_Xdoc.SelectSingleNode("/ScpControl/LY"); Boolean.TryParse(Item.InnerText, out m_LY); }
                catch { }

                try { Item = m_Xdoc.SelectSingleNode("/ScpControl/RX"); Boolean.TryParse(Item.InnerText, out m_RX); }
                catch { }

                try { Item = m_Xdoc.SelectSingleNode("/ScpControl/RY"); Boolean.TryParse(Item.InnerText, out m_RY); }
                catch { }

                try { Item = m_Xdoc.SelectSingleNode("/ScpControl/LED"); Boolean.TryParse(Item.InnerText, out m_LED); }
                catch { }
            }
            catch { Loaded = false; }

            return Loaded;
        }

        public Boolean Save() 
        {
            Boolean Saved = true;

            try
            {
                XmlNode Node, Entry;

                m_Xdoc.RemoveAll();

                Node = m_Xdoc.CreateXmlDeclaration("1.0", "utf-8", String.Empty);
                m_Xdoc.AppendChild(Node);

                Node = m_Xdoc.CreateComment(String.Format(" ScpControl Configuration Data. {0} ", DateTime.Now));
                m_Xdoc.AppendChild(Node);

                Node = m_Xdoc.CreateWhitespace("\r\n");
                m_Xdoc.AppendChild(Node);

                Node = m_Xdoc.CreateNode(XmlNodeType.Element, "ScpControl", null);

                Entry = m_Xdoc.CreateNode(XmlNodeType.Element, "Idle", null); Entry.InnerText = Idle.ToString(); Node.AppendChild(Entry);

                Entry = m_Xdoc.CreateNode(XmlNodeType.Element, "LX", null); Entry.InnerText = LX.ToString(); Node.AppendChild(Entry);
                Entry = m_Xdoc.CreateNode(XmlNodeType.Element, "LY", null); Entry.InnerText = LY.ToString(); Node.AppendChild(Entry);
                Entry = m_Xdoc.CreateNode(XmlNodeType.Element, "RX", null); Entry.InnerText = RX.ToString(); Node.AppendChild(Entry);
                Entry = m_Xdoc.CreateNode(XmlNodeType.Element, "RY", null); Entry.InnerText = RY.ToString(); Node.AppendChild(Entry);

                Entry = m_Xdoc.CreateNode(XmlNodeType.Element, "LED", null); Entry.InnerText = LED.ToString(); Node.AppendChild(Entry);

                m_Xdoc.AppendChild(Node);

                m_Xdoc.Save(m_File);
            }
            catch { Saved = false; }

            return Saved;
        }

        protected Boolean m_LX = false;
        public Boolean LX 
        {
            get { return m_LX; }
            set { m_LX = value; }
        }

        protected Boolean m_LY = false;
        public Boolean LY 
        {
            get { return m_LY; }
            set { m_LY = value; }
        }

        protected Boolean m_RX = false;
        public Boolean RX 
        {
            get { return m_RX; }
            set { m_RX = value; }
        }

        protected Boolean m_RY = false;
        public Boolean RY 
        {
            get { return m_RY; }
            set { m_RY = value; }
        }

        protected Int32 m_Idle = 600000;
        public Int32 Idle 
        {
            get { return m_Idle; }
            set { m_Idle = value; }
        }

        protected Boolean m_LED = false;
        public Boolean LED 
        {
            get { return m_LED; }
            set { m_LED = value; }
        }
    }
}
