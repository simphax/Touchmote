using System;
using System.ComponentModel;

namespace ScpControl 
{
    public partial class BthHub : ScpHub 
    {
        protected BthDevice Device;

        public String  Dongle   
        {
            get { return Device.ToString(); }
        }
        public String  Master   
        {
            get { return Device.Local; }
        }
        public Boolean Pairable 
        {
            get { return m_Started && Device.State == DeviceState.Connected; }
        }


        public BthHub() 
        {
            InitializeComponent();
        }

        public BthHub(IContainer container) 
        {
            container.Add(this);

            InitializeComponent();
        }


        public override Boolean Open()  
        {
            Device = new BthDevice();

            Device.Arrival += new EventHandler<ArrivalEventArgs>(On_Arrival);
            Device.Debug   += new EventHandler<DebugEventArgs>  (On_Debug);
            Device.Report  += new EventHandler<ReportEventArgs> (On_Report);
            
            if (!Device.Open()) Device.Close();

            return true;
        }

        public override Boolean Start() 
        {
            m_Started = true;

            if (Device.State == DeviceState.Reserved)
            {
                Device.Start();
            }

            return m_Started;
        }

        public override Boolean Stop()  
        {
            m_Started = false;

            if (Device.State == DeviceState.Connected)
            {
                Device.Stop();
            }

            return !m_Started;
        }

        public override Boolean Close() 
        {
            m_Started = false;

            return Device.Close();
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
                        if (Device.State != DeviceState.Connected)
                        {
                            BthDevice Arrived = new BthDevice();

                            if (Arrived.Open(Path))
                            {
                                LogDebug(String.Format("-- Device Arrival [{0}]", Arrived.Local, Path));

                                Device.Close();
                                Device = Arrived;

                                Device.Arrival += new EventHandler<ArrivalEventArgs>(On_Arrival);
                                Device.Debug   += new EventHandler<DebugEventArgs>  (On_Debug  );
                                Device.Report  += new EventHandler<ReportEventArgs> (On_Report );

                                if (m_Started) Device.Start();
                                break;
                            }

                            Arrived.Close();
                        }
                    }
                    break;

                case ScpDevice.Notified.Removal:

                    if (Device.Path == Path)
                    {
                        LogDebug(String.Format("-- Device Removal [{0}]", Device.Local, Path));

                        Device.Stop();
                    }
                    break;
            }

            return Ds3PadId.None;
        }
    }
}
