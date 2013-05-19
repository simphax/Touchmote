using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;

namespace ScpControl 
{
    public partial class BthDevice : ScpDevice, IBthDevice 
    {
        public const String DS3_BTH_CLASS_GUID = "{2F87C733-60E0-4355-8515-95D6978418B2}";

        protected Byte m_Id = 0x01;

        protected class ConnectionList : SortedDictionary<BthHandle, BthDs3> { }
        protected ConnectionList m_Connected = new ConnectionList();

        protected Byte[] m_Local = new Byte[6] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        protected String m_HCI_Version = String.Empty;
        protected String m_LMP_Version = String.Empty;

        protected Boolean m_bInitialised = false;

        public virtual String Local 
        {
            get { return String.Format("{0:X2}:{1:X2}:{2:X2}:{3:X2}:{4:X2}:{5:X2}", m_Local[5], m_Local[4], m_Local[3], m_Local[2], m_Local[1], m_Local[0]); }
        }
        public virtual String HCI_Version 
        {
            get { return m_HCI_Version; }
            set { m_HCI_Version = value; }
        }
        public virtual String LMP_Version 
        {
            get { return m_LMP_Version; }
            set { m_LMP_Version = value; }
        }

        public event EventHandler<DebugEventArgs>   Debug   = null;
        public event EventHandler<ArrivalEventArgs> Arrival = null;
        public event EventHandler<ReportEventArgs>  Report  = null;

        protected void LogDebug(String Data) 
        {
            DebugEventArgs args = new DebugEventArgs(Data);

            if (Debug != null)
            {
                Debug(this, args);
            }
        }

        protected Boolean LogArrival(IDs3Device Arrived) 
        {
            ArrivalEventArgs args = new ArrivalEventArgs(Arrived);

            if (Arrival != null)
            {
                Arrival(this, args);
            }

            return args.Handled;
        }

        protected DeviceState m_State = DeviceState.Disconnected;
        public DeviceState State 
        {
            get { return m_State; }
        }


        public BthDevice() : base(DS3_BTH_CLASS_GUID) 
        {
            InitializeComponent();
        }

        public BthDevice(IContainer container) : base(DS3_BTH_CLASS_GUID) 
        {
            container.Add(this);

            InitializeComponent();
        }


        public override Boolean Open(int Instance = 0) 
        {
            if (base.Open(Instance))
            {
                m_State = DeviceState.Reserved;
            }

            return State == DeviceState.Reserved;
        }

        public override Boolean Open(String Path) 
        {
            if (base.Open(Path))
            {
                m_State = DeviceState.Reserved;
            }

            return State == DeviceState.Reserved;
        }

        public override Boolean Start() 
        {
            if (IsActive)
            {
                m_State = DeviceState.Connected;

                HCI_Worker.RunWorkerAsync();
                L2CAP_Worker.RunWorkerAsync();
            }

            return State == DeviceState.Connected;
        }

        public override Boolean Stop()  
        {
            if (IsActive)
            {
                m_State = DeviceState.Reserved;

                foreach (BthDs3 Device in m_Connected.Values)
                {
                    Device.Disconnect();
                    Device.Stop();
                }

                HCI_Reset(); Thread.Sleep(250);

                m_Connected.Clear();
            }

            return base.Stop();
        }

        public override Boolean Close() 
        {
            if (IsActive)
            {
                m_State = DeviceState.Disconnected;

                HCI_Reset(); Thread.Sleep(250);

                m_Connected.Clear();
            }

            return base.Close();
        }


        public override String ToString() 
        {
            switch (State)
            {
                case DeviceState.Disconnected:
                    return String.Format("Host Address : Disconnected");

                case DeviceState.Reserved:
                    if (m_bInitialised)
                    {
                        return String.Format("Host Address : {0}\n\nHCI Version  : {1}\n\nLMP Version  : {2}\n\nReserved",
                            Local,
                            m_HCI_Version,
                            m_LMP_Version
                        );
                    }
                    else
                    {
                        return String.Format("Host Address : <Error>");
                    }

                case DeviceState.Connected:
                    if (m_bInitialised)
                    {
                        return String.Format("Host Address : {0}\n\nHCI Version  : {1}\n\nLMP Version  : {2}",
                            Local,
                            m_HCI_Version,
                            m_LMP_Version
                        );
                    }
                    else
                    {
                        return String.Format("Host Address : <Error>");
                    }
            }

            throw new Exception();
        }


        protected virtual BthDs3 Add(Byte Lsb, Byte Msb, String Name) 
        {
            BthDs3 Connection = null;

            if (m_Connected.Count < 4)
            {
                Connection = new BthDs3(this, m_Local, Lsb, Msb);
                Connection.Debug +=new EventHandler<DebugEventArgs>(On_Debug);

                m_Connected.Add(Connection.HCI_Handle, Connection);
            }

            return Connection;
        }

        protected virtual BthDs3 Get(Byte Lsb, Byte Msb) 
        {
            return m_Connected[new BthHandle(Lsb, Msb)];
        }


        protected virtual void Remove(Byte Lsb, Byte Msb) 
        {
            BthHandle Connection = new BthHandle(Lsb, Msb);

            m_Connected[Connection].Stop();
            m_Connected.Remove(Connection);
        }

        protected virtual void OnInitialised(BthDs3 Ds3) 
        {
            if (LogArrival(Ds3))
            {
                Ds3.Report += new EventHandler<ReportEventArgs>(On_Report);
                Ds3.Start();
            }
        }

        protected virtual void OnCompletedCount(Byte Lsb, Byte Msb, UInt16 Count) 
        {
            if (Count > 0) m_Connected[new BthHandle(Lsb, Msb)].Completed();
        }


        protected void On_Debug(object sender, DebugEventArgs e) 
        {
            if (Debug != null)
            {
                Debug(this, e);
            }
        }

        protected void On_Report(object sender, ReportEventArgs e) 
        {
            if (Report != null) Report(sender, e);
        }

        #region Worker Threads
        protected virtual void L2CAP_Worker_Thread(object sender, DoWorkEventArgs e) 
        {
            Thread.Sleep(1);

            StringBuilder debug = new StringBuilder();

            Byte[] Buffer = new Byte[64];
            Byte[] L2_DCID, L2_SCID;

            Int32 Transfered = 0;
            L2CAP.Code Event = L2CAP.Code.L2CAP_Reserved;

            LogDebug(String.Format("-- Bluetooth  : L2CAP_Worker_Thread Starting [{0:X2},{1:X2}]", m_BulkIn, m_BulkOut));

            while (IsActive)
            {
                try
                {
                    if (ReadBulkPipe(Buffer, Buffer.Length, ref Transfered) && Transfered > 0)
                    {
                        BthDs3 Connection = Get(Buffer[0], Buffer[1]);

                        if (Buffer[6] == 0x01 && Buffer[7] == 0x00) // Control Channel
                        {
                            if (Enum.IsDefined(typeof(L2CAP.Code), Buffer[8]))
                            {
                                Event = (L2CAP.Code) Buffer[8];

                                switch (Event)
                                {
                                    case L2CAP.Code.L2CAP_Command_Reject:

                                        LogDebug(String.Format(">> {0} [{1:X2}]", Event, Buffer[8]));
                                        break;

                                    case L2CAP.Code.L2CAP_Connection_Request:

                                        LogDebug(String.Format(">> {0} [{1:X2}] PSM [{2:X2}]", Event, Buffer[8], Buffer[12]));

                                        L2_SCID = new Byte[2] { Buffer[14], Buffer[15] };
                                        L2_DCID = Connection.Set((L2CAP.PSM)Buffer[12], L2_SCID);

                                        L2CAP_Connection_Response(Connection.HCI_Handle.Bytes, Buffer[9], L2_SCID, L2_DCID, 0x00);
                                        LogDebug(String.Format("<< {0} [{1:X2}]", L2CAP.Code.L2CAP_Connection_Response, (Byte) L2CAP.Code.L2CAP_Connection_Response));

                                        L2CAP_Configuration_Request(Connection.HCI_Handle.Bytes, m_Id++, L2_SCID);
                                        LogDebug(String.Format("<< {0} [{1:X2}]", L2CAP.Code.L2CAP_Configuration_Request, (Byte) L2CAP.Code.L2CAP_Configuration_Request));
                                        break;

                                    case L2CAP.Code.L2CAP_Connection_Response:

                                        LogDebug(String.Format(">> {0} [{1:X2}] [{2:X2}]", Event, Buffer[8], Buffer[16]));

                                        if (Buffer[16] == 0) // Success
                                        {
                                            L2_SCID = new Byte[2] { Buffer[12], Buffer[13] };
                                            L2_DCID = new Byte[2] { Buffer[14], Buffer[15] };

                                            UInt16 DCID = (UInt16)(Buffer[15] << 8 | Buffer[14]);

                                            Connection.Set(L2CAP.PSM.HID_Service, L2_SCID[0], L2_SCID[1], DCID);

                                            L2CAP_Configuration_Request(Connection.HCI_Handle.Bytes, m_Id++, L2_SCID);
                                            LogDebug(String.Format("<< {0} [{1:X2}]", L2CAP.Code.L2CAP_Configuration_Request, (Byte) L2CAP.Code.L2CAP_Configuration_Request));
                                        }
                                        break;

                                    case L2CAP.Code.L2CAP_Configuration_Request:

                                        LogDebug(String.Format(">> {0} [{1:X2}]", Event, Buffer[8]));

                                        L2_SCID = Connection.Get_SCID(Buffer[12], Buffer[13]);

                                        L2CAP_Configuration_Response(Connection.HCI_Handle.Bytes, Buffer[9], L2_SCID);
                                        LogDebug(String.Format("<< {0} [{1:X2}]", L2CAP.Code.L2CAP_Configuration_Response, (Byte) L2CAP.Code.L2CAP_Configuration_Response));

                                        if (Connection.SvcStarted)
                                        {
                                            Connection.CanStartHid = true;
                                            Connection.InitReport(Buffer);
                                        }
                                        break;

                                    case L2CAP.Code.L2CAP_Configuration_Response:

                                        LogDebug(String.Format(">> {0} [{1:X2}]", Event, Buffer[8]));

                                        if (Connection.CanStartSvc)
                                        {
                                            UInt16 DCID = BthConnection.DCID++;
                                            L2_DCID = new Byte[2] { (Byte)((DCID >> 0) & 0xFF), (Byte)((DCID >> 8) & 0xFF) };

                                            L2CAP_Connection_Request(Connection.HCI_Handle.Bytes, m_Id++, L2_DCID, L2CAP.PSM.HID_Service);
                                            LogDebug(String.Format("<< {0} [{1:X2}] PSM [{2:X2}]", L2CAP.Code.L2CAP_Connection_Request, (Byte) L2CAP.Code.L2CAP_Connection_Request, (Byte) L2CAP.PSM.HID_Service));
                                        }
                                        break;

                                    case L2CAP.Code.L2CAP_Disconnection_Request:

                                        LogDebug(String.Format(">> {0} [{1:X2}] Handle [{2:X2}{3:X2}]", Event, Buffer[8], Buffer[15], Buffer[14]));

                                        L2_SCID = new Byte[2] { Buffer[14], Buffer[15] };

                                        L2CAP_Disconnection_Response(Connection.HCI_Handle.Bytes, Buffer[9], L2_SCID, L2_SCID);
                                        LogDebug(String.Format("<< {0} [{1:X2}]", L2CAP.Code.L2CAP_Disconnection_Response, (Byte) L2CAP.Code.L2CAP_Disconnection_Response));
                                        break;

                                    case L2CAP.Code.L2CAP_Disconnection_Response:

                                        LogDebug(String.Format(">> {0} [{1:X2}]", Event, Buffer[8]));

                                        if (Connection.CanStartHid)
                                        {
                                            Connection.SvcStarted = false;
                                            OnInitialised(Connection);
                                        }
                                        break;

                                    case L2CAP.Code.L2CAP_Echo_Request:

                                        LogDebug(String.Format(">> {0} [{1:X2}]", Event, Buffer[8]));
                                        break;

                                    case L2CAP.Code.L2CAP_Echo_Response:

                                        LogDebug(String.Format(">> {0} [{1:X2}]", Event, Buffer[8]));
                                        break;

                                    case L2CAP.Code.L2CAP_Information_Request:

                                        LogDebug(String.Format(">> {0} [{1:X2}]", Event, Buffer[8]));
                                        break;

                                    case L2CAP.Code.L2CAP_Information_Response:

                                        LogDebug(String.Format(">> {0} [{1:X2}]", Event, Buffer[8]));
                                        break;

                                    default:
                                        break;
                                }
                            }
                        }
                        else if (Buffer[8] == 0xA1 && Buffer[9] == 0x01 && Transfered == 58) Connection.Parse(Buffer);
                        else if (Connection.InitReport(Buffer))
                        {
                            Connection.CanStartHid = true;

                            L2_DCID = Connection.Get_DCID(L2CAP.PSM.HID_Service);
                            L2_SCID = Connection.Get_SCID(L2CAP.PSM.HID_Service);

                            L2CAP_Disconnection_Request(Connection.HCI_Handle.Bytes, m_Id++, L2_SCID, L2_DCID);
                            LogDebug(String.Format("<< {0} [{1:X2}]", L2CAP.Code.L2CAP_Disconnection_Request, (Byte) L2CAP.Code.L2CAP_Disconnection_Request));
                        }
                    }
                }

                catch (Exception Ex) { Console.WriteLine(Ex.ToString()); }
            }

            LogDebug("-- Bluetooth  : L2CAP_Worker_Thread Exiting");
        }

        protected virtual void HCI_Worker_Thread(object sender, DoWorkEventArgs e) 
        {
            Thread.Sleep(1);

            SortedDictionary<String, String> NameList = new SortedDictionary<String, String>();
            StringBuilder nm = new StringBuilder(), debug = new StringBuilder();

            Boolean bStarted = false;
            String bd = String.Empty;

            Byte[] Buffer = new Byte[512];
            Byte[] BD_Addr = new Byte[6];

            Int32 Transfered = 0;
            HCI.Event Event;

            LogDebug(String.Format("-- Bluetooth  : HCI_Worker_Thread Starting [{0:X2}]", m_IntIn));

            HCI_Reset();
            LogDebug(String.Format("<< {0} [{1:X4}]", HCI.Command.HCI_Reset, (UInt16) HCI.Command.HCI_Reset));

            while (IsActive)
            {
                try
                {
                    if (ReadIntPipe(Buffer, Buffer.Length, ref Transfered) && Transfered > 0)
                    {
                        if (Enum.IsDefined(typeof(HCI.Event), Buffer[0]))
                        {
                            Event = (HCI.Event) Buffer[0];

                            switch (Event)
                            {
                                case HCI.Event.HCI_Command_Complete_EV:

                                    HCI.Command Command = (HCI.Command)(UInt16)(Buffer[3] | Buffer[4] << 8);
                                    LogDebug(String.Format(">> {0} [{1:X2}] [{2:X2}]", Event, Buffer[0], Buffer[5]));

                                    if (Command == HCI.Command.HCI_Reset && Buffer[5] == 0 && !bStarted)
                                    {
                                        bStarted = true; Thread.Sleep(250);

                                        Transfered = HCI_Read_BD_Addr();

                                        LogDebug(String.Format("<< {0} [{1:X4}]", HCI.Command.HCI_Read_BD_ADDR, (UInt16) HCI.Command.HCI_Read_BD_ADDR));
                                    }

                                    if (Command == HCI.Command.HCI_Read_BD_ADDR && Buffer[5] == 0)
                                    {
                                        Transfered = HCI_Read_Buffer_Size();

                                        m_Local = new Byte[] { Buffer[6], Buffer[7], Buffer[8], Buffer[9], Buffer[10], Buffer[11] };

                                        LogDebug(String.Format("<< {0} [{1:X4}]", HCI.Command.HCI_Read_Buffer_Size, (UInt16) HCI.Command.HCI_Read_Buffer_Size));
                                    }

                                    if (Command == HCI.Command.HCI_Read_Buffer_Size && Buffer[5] == 0)
                                    {
                                        LogDebug(String.Format("-- {0:X2}{1:X2}, {2:X2}, {3:X2}{4:X2}, {5:X2}{6:X2}", Buffer[7], Buffer[6], Buffer[8], Buffer[10], Buffer[9], Buffer[12], Buffer[11]));

                                        Transfered = HCI_Read_Local_Version_Info();

                                        LogDebug(String.Format("<< {0} [{1:X4}]", HCI.Command.HCI_Read_Local_Version_Info, (UInt16) HCI.Command.HCI_Read_Local_Version_Info));
                                    }

                                    if (Command == HCI.Command.HCI_Read_Local_Version_Info && Buffer[5] == 0)
                                    {
                                        Transfered = HCI_Write_Scan_Enable();

                                        HCI_Version = String.Format("{0}.{1}", Buffer[6], Buffer[ 8] << 8 | Buffer[ 7]);
                                        LMP_Version = String.Format("{0}.{1}", Buffer[9], Buffer[13] << 8 | Buffer[12]);

                                        LogDebug(String.Format("-- Master {0}, HCI_Version {1}, LMP_Version {2}", Local, HCI_Version, LMP_Version));
                                        LogDebug(String.Format("<< {0} [{1:X4}]", HCI.Command.HCI_Write_Scan_Enable, (UInt16) HCI.Command.HCI_Write_Scan_Enable));
                                    }

                                    if (Command == HCI.Command.HCI_Write_Scan_Enable && Buffer[5] == 0)
                                    {
                                        m_bInitialised = true;
                                    }
                                    break;

                                case HCI.Event.HCI_Connection_Request_EV:

                                    for (int i = 0; i < 6; i++) BD_Addr[i] = Buffer[i + 2];

                                    LogDebug(String.Format(">> {0} [{1:X2}]", Event, Buffer[0]));

                                    Transfered = HCI_Remote_Name_Request(BD_Addr);

                                    LogDebug(String.Format("<< {0} [{1:X4}]", HCI.Command.HCI_Remote_Name_Request, (UInt16) HCI.Command.HCI_Remote_Name_Request));
                                    break;

                                case HCI.Event.HCI_Connection_Complete_EV:

                                    bd = String.Format("{0:X2}:{1:X2}:{2:X2}:{3:X2}:{4:X2}:{5:X2}", Buffer[10], Buffer[9], Buffer[8], Buffer[7], Buffer[6], Buffer[5]);

                                    BthConnection Connection = Add(Buffer[3], (Byte)(Buffer[4] | 0x20), NameList[bd]);

                                    Connection.Remote_Name = NameList[bd]; NameList.Remove(bd);
                                    Connection.BD_Address  = new Byte[] { Buffer[5], Buffer[6], Buffer[7], Buffer[8], Buffer[9], Buffer[10] };

                                    LogDebug(String.Format(">> {0} [{1:X2}]", Event, Buffer[0]));
                                    break;

                                case HCI.Event.HCI_Page_Scan_Repetition_Mode_Change_EV:

                                    LogDebug(String.Format(">> {0} [{1:X2}]", Event, Buffer[0]));
                                    break;

                                case HCI.Event.HCI_Command_Status_EV:

                                    LogDebug(String.Format(">> {0} [{1:X2}] [{2:X2}]", Event, Buffer[0], Buffer[2]));
                                    break;

                                case HCI.Event.HCI_Role_Change_EV:

                                    LogDebug(String.Format(">> {0} [{1:X2}]", Event, Buffer[0]));
                                    break;

                                case HCI.Event.HCI_Disconnection_Complete_EV:

                                    LogDebug(String.Format(">> {0} [{1:X2}]", Event, Buffer[0]));

                                    Remove(Buffer[3], (Byte)(Buffer[4] | 0x20));
                                    break;

                                case HCI.Event.HCI_Number_Of_Completed_Packets_EV:

                                    for (Byte Index = 0, Ptr = 3; Index < Buffer[2]; Index++, Ptr += 4)
                                    {
                                        OnCompletedCount(Buffer[Ptr], (Byte)(Buffer[Ptr + 1] | 0x20), (UInt16)(Buffer[Ptr + 2] | Buffer[Ptr + 3] << 8));
                                    }
                                    break;

                                case HCI.Event.HCI_Remote_Name_Request_Complete_EV:

                                    bd = String.Format("{0:X2}:{1:X2}:{2:X2}:{3:X2}:{4:X2}:{5:X2}", Buffer[8], Buffer[7], Buffer[6], Buffer[5], Buffer[4], Buffer[3]);
                                    nm = new StringBuilder();

                                    for (int Index = 9; Index < Buffer.Length; Index++)
                                    {
                                        if (Buffer[Index] > 0) nm.Append((Char) Buffer[Index]);
                                        else break;
                                    }

                                    String Name = nm.ToString();

                                    LogDebug(String.Format(">> {0} [{1:X2}]", Event, Buffer[0]));
                                    LogDebug(String.Format("-- Remote Name : {0} - {1}", bd, Name));

                                    for (int i = 0; i < 6; i++) BD_Addr[i] = Buffer[i + 3];

                                    if (Name == "PLAYSTATION(R)3 Controller" || Name == "Navigation Controller")
                                    {
                                        NameList.Add(bd, nm.ToString());

                                        Transfered = HCI_Accept_Connection_Request(BD_Addr, 0x00);

                                        LogDebug(String.Format("<< {0} [{1:X4}]", HCI.Command.HCI_Accept_Connection_Request, (UInt16) HCI.Command.HCI_Accept_Connection_Request));
                                    }
                                    else
                                    {
                                       Transfered = HCI_Reject_Connection_Request(BD_Addr, 0x0F);

                                       LogDebug(String.Format("<< {0} [{1:X4}]", HCI.Command.HCI_Reject_Connection_Request, (UInt16) HCI.Command.HCI_Reject_Connection_Request));
                                    }
                                    break;

                                default:
                                    break;
                            }
                        }
                    }
                }
                catch (Exception Ex) { Console.WriteLine(Ex.ToString()); }
            }

            LogDebug("-- Bluetooth  : HCI_Worker_Thread Exiting");
        }
        #endregion

        #region HCI Commands
        protected virtual Int32 HCI_Command(Byte[] Data) 
        {
            Int32 Transfered = 0;

            SendTransfer(0x20, 0x00, 0x0000, Data, ref Transfered);
            return Transfered;
        }

        protected virtual Int32 HCI_Accept_Connection_Request(Byte[] BD_Addr, Byte Role) 
        {
            Byte[] Buffer = new Byte[10];

            Buffer[0] = (Byte)(((UInt32)HCI.Command.HCI_Accept_Connection_Request >> 0) & 0xFF);
            Buffer[1] = (Byte)(((UInt32)HCI.Command.HCI_Accept_Connection_Request >> 8) & 0xFF);
            Buffer[2] = 0x07;
            Buffer[3] = BD_Addr[0];
            Buffer[4] = BD_Addr[1];
            Buffer[5] = BD_Addr[2];
            Buffer[6] = BD_Addr[3];
            Buffer[7] = BD_Addr[4];
            Buffer[8] = BD_Addr[5];
            Buffer[9] = Role;

            return HCI_Command(Buffer);
        }

        protected virtual Int32 HCI_Reject_Connection_Request(Byte[] BD_Addr, Byte Reason) 
        {
            Byte[] Buffer = new Byte[10];

            Buffer[0] = (Byte)(((UInt32)HCI.Command.HCI_Reject_Connection_Request >> 0) & 0xFF);
            Buffer[1] = (Byte)(((UInt32)HCI.Command.HCI_Reject_Connection_Request >> 8) & 0xFF);
            Buffer[2] = 0x07;
            Buffer[3] = BD_Addr[0];
            Buffer[4] = BD_Addr[1];
            Buffer[5] = BD_Addr[2];
            Buffer[6] = BD_Addr[3];
            Buffer[7] = BD_Addr[4];
            Buffer[8] = BD_Addr[5];
            Buffer[9] = Reason;

            return HCI_Command(Buffer);
        }

        protected virtual Int32 HCI_Remote_Name_Request(Byte[] BD_Addr) 
        {
            Byte[] Buffer = new Byte[13];

            Buffer[0] = (Byte)(((UInt32)HCI.Command.HCI_Remote_Name_Request >> 0) & 0xFF);
            Buffer[1] = (Byte)(((UInt32)HCI.Command.HCI_Remote_Name_Request >> 8) & 0xFF);
            Buffer[2] = 0x0A;
            Buffer[3] = BD_Addr[0];
            Buffer[4] = BD_Addr[1];
            Buffer[5] = BD_Addr[2];
            Buffer[6] = BD_Addr[3];
            Buffer[7] = BD_Addr[4];
            Buffer[8] = BD_Addr[5];
            Buffer[9] = 0x01;
            Buffer[10] = 0x00;
            Buffer[11] = 0x00;
            Buffer[12] = 0x00;

            return HCI_Command(Buffer);
        }

        protected virtual Int32 HCI_Reset() 
        {
            Byte[] Buffer = new Byte[3];

            Buffer[0] = (Byte)(((UInt32)HCI.Command.HCI_Reset >> 0) & 0xFF);
            Buffer[1] = (Byte)(((UInt32)HCI.Command.HCI_Reset >> 8) & 0xFF);
            Buffer[2] = 0x00;

            return HCI_Command(Buffer);
        }

        protected virtual Int32 HCI_Write_Scan_Enable() 
        {
            Byte[] Buffer = new Byte[4];

            Buffer[0] = (Byte)(((UInt32)HCI.Command.HCI_Write_Scan_Enable >> 0) & 0xFF);
            Buffer[1] = (Byte)(((UInt32)HCI.Command.HCI_Write_Scan_Enable >> 8) & 0xFF);
            Buffer[2] = 0x01;
            Buffer[3] = 0x02;

            return HCI_Command(Buffer);
        }

        protected virtual Int32 HCI_Read_Local_Version_Info() 
        {
            Byte[] Buffer = new Byte[3];

            Buffer[0] = (Byte)(((UInt32)HCI.Command.HCI_Read_Local_Version_Info >> 0) & 0xFF);
            Buffer[1] = (Byte)(((UInt32)HCI.Command.HCI_Read_Local_Version_Info >> 8) & 0xFF);
            Buffer[2] = 0x00;

            return HCI_Command(Buffer);
        }

        protected virtual Int32 HCI_Read_BD_Addr() 
        {
            Byte[] Buffer = new Byte[3];

            Buffer[0] = (Byte)(((UInt32)HCI.Command.HCI_Read_BD_ADDR >> 0) & 0xFF);
            Buffer[1] = (Byte)(((UInt32)HCI.Command.HCI_Read_BD_ADDR >> 8) & 0xFF);
            Buffer[2] = 0x00;

            return HCI_Command(Buffer);
        }

        protected virtual Int32 HCI_Read_Buffer_Size() 
        {
            Byte[] Buffer = new Byte[3];

            Buffer[0] = (Byte)(((UInt32)HCI.Command.HCI_Read_Buffer_Size >> 0) & 0xFF);
            Buffer[1] = (Byte)(((UInt32)HCI.Command.HCI_Read_Buffer_Size >> 8) & 0xFF);
            Buffer[2] = 0x00;

            return HCI_Command(Buffer);
        }

        public virtual Int32 HCI_Disconnect(BthHandle Handle) 
        {
            Byte[] Buffer = new Byte[6];

            Buffer[0] = (Byte)(((UInt32)HCI.Command.HCI_Disconnect >> 0) & 0xFF);
            Buffer[1] = (Byte)(((UInt32)HCI.Command.HCI_Disconnect >> 8) & 0xFF);
            Buffer[2] = 0x03;
            Buffer[3] = (Byte)(Handle.Bytes[0]);
            Buffer[4] = (Byte)(Handle.Bytes[1] ^ 0x20);
            Buffer[5] = 0x13;

            return HCI_Command(Buffer);
        }
        #endregion

        #region L2CAP Commands
        protected virtual Int32 L2CAP_Command(Byte[] Handle, Byte[] Data) 
        {
            Int32 Transfered = 0;
            Byte[] Buffer = new Byte[64];

            Buffer[0] = Handle[0];
            Buffer[1] = (Byte)(Handle[1] | 0x20);
            Buffer[2] = (Byte)(Data.Length + 4);
            Buffer[3] = 0x00;
            Buffer[4] = (Byte)(Data.Length);
            Buffer[5] = 0x00;
            Buffer[6] = 0x01;
            Buffer[7] = 0x00;

            for (int i = 0; i < Data.Length; i++) Buffer[i + 8] = Data[i];

            WriteBulkPipe(Buffer, Data.Length + 8, ref Transfered);
            return Transfered;
        }

        protected virtual Int32 L2CAP_Connection_Request(Byte[] Handle, Byte Id, Byte[] DCID, L2CAP.PSM Psm) 
        {
            Byte[] Buffer = new Byte[8];

            Buffer[0] = 0x02;
            Buffer[1] = Id;
            Buffer[2] = 0x04;
            Buffer[3] = 0x00;
            Buffer[4] = (Byte) Psm;
            Buffer[5] = 0x00;
            Buffer[6] = DCID[0];
            Buffer[7] = DCID[1];

            return L2CAP_Command(Handle, Buffer);
        }

        protected virtual Int32 L2CAP_Connection_Response(Byte[] Handle, Byte Id, Byte[] DCID, Byte[] SCID, Byte Result) 
        {
            Byte[] Buffer = new Byte[12];

            Buffer[ 0] = 0x03;
            Buffer[ 1] = Id;
            Buffer[ 2] = 0x08;
            Buffer[ 3] = 0x00;
            Buffer[ 4] = SCID[0];
            Buffer[ 5] = SCID[1];
            Buffer[ 6] = DCID[0];
            Buffer[ 7] = DCID[1];
            Buffer[ 8] = Result;
            Buffer[ 9] = 0x00;
            Buffer[10] = 0x00;
            Buffer[11] = 0x00;

            return L2CAP_Command(Handle, Buffer);
        }

        protected virtual Int32 L2CAP_Configuration_Request(Byte[] Handle, Byte Id, Byte[] DCID, Boolean MTU = true) 
        {
            Byte[] Buffer = new Byte[MTU ? 12 : 8];

            Buffer[0] = 0x04;
            Buffer[1] = Id;
            Buffer[2] = (Byte)(MTU ? 0x08 : 0x04);
            Buffer[3] = 0x00;
            Buffer[4] = DCID[0];
            Buffer[5] = DCID[1];
            Buffer[6] = 0x00;
            Buffer[7] = 0x00;

            if (MTU)
            {
                Buffer[ 8] = 0x01;
                Buffer[ 9] = 0x02;
                Buffer[10] = 0x96;
                Buffer[11] = 0x00;
            }

            return L2CAP_Command(Handle, Buffer);
        }

        protected virtual Int32 L2CAP_Configuration_Response(Byte[] Handle, Byte Id, Byte[] SCID) 
        {
            Byte[] Buffer = new Byte[10];

            Buffer[0] = 0x05;
            Buffer[1] = Id;
            Buffer[2] = 0x06;
            Buffer[3] = 0x00;
            Buffer[4] = SCID[0];
            Buffer[5] = SCID[1];
            Buffer[6] = 0x00;
            Buffer[7] = 0x00;
            Buffer[8] = 0x00;
            Buffer[9] = 0x00;

            return L2CAP_Command(Handle, Buffer);
        }

        protected virtual Int32 L2CAP_Disconnection_Request(Byte[] Handle, Byte Id, Byte[] DCID, Byte[] SCID) 
        {
            Byte[] Buffer = new Byte[8];

            Buffer[0] = 0x06;
            Buffer[1] = Id;
            Buffer[2] = 0x04;
            Buffer[3] = 0x00;
            Buffer[4] = DCID[0];
            Buffer[5] = DCID[1];
            Buffer[6] = SCID[0];
            Buffer[7] = SCID[1];

            return L2CAP_Command(Handle, Buffer);
        }

        protected virtual Int32 L2CAP_Disconnection_Response(Byte[] Handle, Byte Id, Byte[] DCID, Byte[] SCID) 
        {
            Byte[] Buffer = new Byte[8];

            Buffer[0] = 0x07;
            Buffer[1] = Id;
            Buffer[2] = 0x04;
            Buffer[3] = 0x00;
            Buffer[4] = DCID[0];
            Buffer[5] = DCID[1];
            Buffer[6] = SCID[0];
            Buffer[7] = SCID[1];

            return L2CAP_Command(Handle, Buffer);
        }
        #endregion

        #region HIDP Commands
        public virtual Int32 HID_Command(Byte[] Handle, Byte[] Channel, Byte[] Data) 
        {
            Int32 Transfered = 0;
            Byte[] Buffer = new Byte[64];

            Buffer[0] = Handle[0];
            Buffer[1] = (Byte)(Handle[1] | 0x20);
            Buffer[2] = (Byte)(Data.Length + 4);
            Buffer[3] = 0x00;
            Buffer[4] = (Byte)(Data.Length);
            Buffer[5] = 0x00;
            Buffer[6] = Channel[0];
            Buffer[7] = Channel[1];

            for (int i = 0; i < Data.Length; i++) Buffer[i + 8] = Data[i];

            WriteBulkPipe(Buffer, Data.Length + 8, ref Transfered);
            return Transfered;
        }
        #endregion
    }
}
