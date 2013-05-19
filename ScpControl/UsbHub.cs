using System;
using System.ComponentModel;

namespace ScpControl 
{
    public partial class UsbHub : ScpHub 
    {
        protected UsbDevice[] Device = new UsbDevice[4];


        public UsbHub() 
        {
            InitializeComponent();
        }

        public UsbHub(IContainer container) 
        {
            container.Add(this);

            InitializeComponent();
        }


        public override Boolean Open()  
        {
            for (Byte Pad = 0; Pad < Device.Length; Pad++)
            {
                Device[Pad] = new UsbDevice();

                Device[Pad].PadId = (Ds3PadId) Pad;
            }

            for (Byte Pad = 0; Pad < Device.Length; Pad++)
            {
                try
                {
                    if (Device[Pad].Open(Pad))
                    {
                        if (LogArrival(Device[Pad]))
                        {
                            Device[Pad].Debug  += new EventHandler<DebugEventArgs> (On_Debug );
                            Device[Pad].Report += new EventHandler<ReportEventArgs>(On_Report);
                        }
                    }
                    else Device[Pad].Close();
                }
                catch { }
            }

            return base.Open();
        }

        public override Boolean Start() 
        {
            m_Started = true;

            try
            {
                for (Int32 Index = 0; Index < Device.Length; Index++)
                {
                    if (Device[Index].State == DeviceState.Reserved)
                    {
                        Device[Index].Start();
                    }
                }
            }
            catch { }

            return base.Start();
        }

        public override Boolean Stop()  
        {
            m_Started = false;

            try
            {
                for (Int32 Index = 0; Index < Device.Length; Index++)
                {
                    if (Device[Index].State == DeviceState.Connected)
                    {
                        Device[Index].Stop();
                    }
                }
            }
            catch { }

            return base.Stop();
        }

        public override Boolean Close() 
        {
            m_Started = false;

            try
            {
                for (Int32 Index = 0; Index < Device.Length; Index++)
                {
                    if (Device[Index].State == DeviceState.Connected)
                    {
                        Device[Index].Close();
                    }
                }
            }
            catch { }

            return base.Close();
        }


        public override Boolean Suspend() 
        {
            Stop();
            Close();

            return base.Suspend();
        }

        public override Boolean Resume()  
        {
            Open();
            Start();

            return base.Resume();
        }


        public override Ds3PadId Notify(ScpDevice.Notified Notification, String Class, String Path) 
        {
            LogDebug(String.Format("++ Notify [{0}] [{1}] [{2}]", Notification, Class, Path));

            switch (Notification)
            {
                case ScpDevice.Notified.Arrival:
                    {
                        UsbDevice Arrived = new UsbDevice();

                        if (Arrived.Open(Path))
                        {
                            LogDebug(String.Format("-- Device Arrival [{0}]", Arrived.Local, Path));

                            if (LogArrival(Arrived))
                            {
                                if (Device[(Byte) Arrived.PadId].IsShutdown)
                                {
                                    Device[(Byte) Arrived.PadId].IsShutdown = false;

                                    Device[(Byte) Arrived.PadId].Close();
                                    Device[(Byte) Arrived.PadId] = Arrived;

                                    return Arrived.PadId;
                                }
                                else
                                {
                                    Arrived.Debug  += new EventHandler<DebugEventArgs> (On_Debug );
                                    Arrived.Report += new EventHandler<ReportEventArgs>(On_Report);

                                    Device[(Byte) Arrived.PadId].Close();
                                    Device[(Byte) Arrived.PadId] = Arrived;

                                    if (m_Started) Arrived.Start();
                                    return Arrived.PadId;
                                }
                            }
                        }

                        Arrived.Close();
                    }
                    break;

                case ScpDevice.Notified.Removal:
                    {
                        for (Int32 Index = 0; Index < Device.Length; Index++)
                        {
                            if (Device[Index].State == DeviceState.Connected && Path == Device[Index].Path)
                            {
                                LogDebug(String.Format("-- Device Removal [{0}]", Device[Index].Local, Path));

                                Device[Index].Stop();
                            }
                        }
                    }
                    break;
            }

            return Ds3PadId.None;
        }
    }
}
