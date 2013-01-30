//////////////////////////////////////////////////////////////////////////////////
//	Wiimote.cs
//	Managed Wiimote Library
//	Written by Brian Peek (http://www.brianpeek.com/)
//	for MSDN's Coding4Fun (http://msdn.microsoft.com/coding4fun/)
//	Visit http://blogs.msdn.com/coding4fun/archive/2007/03/14/1879033.aspx
//  and http://www.codeplex.com/WiimoteLib
//	for more information
//////////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Collections.Generic;
using Microsoft.Win32.SafeHandles;
using System.Threading;

namespace WiimoteLib
{
	/// <summary>
	/// Implementation of Wiimote
	/// </summary>
	public class Wiimote : IDisposable
	{
		/// <summary>
		/// Event raised when Wiimote state is changed
		/// </summary>
		public event EventHandler<WiimoteChangedEventArgs> WiimoteChanged;

		/// <summary>
		/// Event raised when an extension is inserted or removed
		/// </summary>
		public event EventHandler<WiimoteExtensionChangedEventArgs> WiimoteExtensionChanged;

		// VID = Nintendo, PID = Wiimote
		private const int VID = 0x057e;
		private const int PID = 0x0306;

		// sure, we could find this out the hard way using HID, but trust me, it's 22
		private const int REPORT_LENGTH = 22;

        // timeout for reading writing to the Wiimote
        private const int WIIMOTE_TIMEOUT = 1000;

        // used for the polling mechanism
        private const int POLLING_DUETIME = 5000;
        private const int POLLING_PERIOD = 1000;

        // Wiimote output commands
		private enum OutputReport : byte
		{
			LEDs			= 0x11,
			DataReportType	= 0x12,
			IR				= 0x13,
			Status			= 0x15,
			WriteMemory		= 0x16,
			ReadMemory		= 0x17,
			IR2				= 0x1a,
		};

		// Wiimote registers
        private const int REGISTER_WIIMOTE_INIT		= 0x0016;

        private const int REGISTER_IR				= 0x04b00030;
		private const int REGISTER_IR_SENSITIVITY_1	= 0x04b00000;
		private const int REGISTER_IR_SENSITIVITY_2	= 0x04b0001a;
		private const int REGISTER_IR_MODE			= 0x04b00033;

		private const int REGISTER_EXTENSION_INIT_1			= 0x04a400f0;
		private const int REGISTER_EXTENSION_INIT_2			= 0x04a400fb;
		private const int REGISTER_EXTENSION_TYPE			= 0x04a400fa;
		private const int REGISTER_EXTENSION_TYPE_2			= 0x04a400fe;
		private const int REGISTER_EXTENSION_CALIBRATION	= 0x04a40020;

        private const int REGISTER_MOTIONPLUS_INIT          = 0x04a600fe;
		private const int REGISTER_MOTIONPLUS_TYPE			= 0x04a600fa;
        private const int REGISTER_MOTIONPLUS_CALIBRATION   = 0x04a40020;     // After  Init -> 0x04a40020      // Before Init -> 0x04a60020;

		// MotionPlus speed scaling
        private const double MOTIONPLUS_LOWSPEED_SCALING = 20.0; // RawValues / 20 = x degree
        private const double MOTIONPLUS_HIGHSPEED_SCALING = 4.0; // RawValues / 4 = x degree

        // length between board sensors
		private const int BSL = 43;

		// width between board sensors
		private const int BSW = 24;

		// read/write handle to the device
		private SafeFileHandle mHandle;

		// a pretty .NET stream to read/write from/to
		private FileStream mStream;

		// read data buffer
		private byte[] mReadBuff;

		// address to read from
		private int mAddress;

		// size of requested read
		private short mSize;

		// current state of controller
		private readonly WiimoteState mWiimoteState = new WiimoteState();

		// event for read data processing
		private readonly AutoResetEvent mReadDone = new AutoResetEvent(false);
		private readonly AutoResetEvent mWriteDone = new AutoResetEvent(false);

        // event for status report
        private readonly AutoResetEvent mStatusDone = new AutoResetEvent(false);
        private readonly AutoResetEvent mStatusRequest = new AutoResetEvent(false);

        // lock ressources to avoid exceptions and timeouts
        private static Mutex mReadMutex = new Mutex();
        private static Mutex mWriteMutex = new Mutex();
        private static Mutex mInitMutex = new Mutex();
        private static Mutex mGetStatusMutex = new Mutex();
        private static Mutex mStatusMutex = new Mutex();

		// use a different method to write reports
		private bool mAltWriteMethod;

		// HID device path of this Wiimote
		private string mDevicePath = string.Empty;

		// unique ID
		private readonly Guid mID = Guid.NewGuid();

		// delegate used for enumerating found Wiimotes
		internal delegate bool WiimoteFoundDelegate(string devicePath);

		// kilograms to pounds
		private const float KG2LB = 2.20462262f;

        // calibrate MotionPlus the next time
        private bool mCalibMotionPlus = false;
        // number of rawvalues used for calibration
        private int mCalibMotionPlusNumValues = 10;
        // list contains last gyrostates for calibration
        private List<GyroState> mCalibMotionPlusList = new List<GyroState>();

        // counter for MotionPlus extension connect/disconnect reports
        private uint mCounterExtensionConnect = 0;
        private uint mCounterExtensionDisconnect = 0;

        // used for the polling mechanism
        private TimerCallback timerDelegate;
        private Timer stateTimer;

		/// <summary>
		/// Default constructor
		/// </summary>
		public Wiimote()
		{
		}

		internal Wiimote(string devicePath)
		{
			mDevicePath = devicePath;
		}

		/// <summary>
		/// Connect to the first-found Wiimote
		/// </summary>
		/// <exception cref="WiimoteNotFoundException">Wiimote not found in HID device list</exception>
		public void Connect()
		{
			if(string.IsNullOrEmpty(mDevicePath))
				FindWiimote(WiimoteFound);
			else
				OpenWiimoteDeviceHandle(mDevicePath);
		}

		internal static void FindWiimote(WiimoteFoundDelegate wiimoteFound)
		{
			int index = 0;
			bool found = false;
			Guid guid;
			SafeFileHandle mHandle;

			// get the GUID of the HID class
			HIDImports.HidD_GetHidGuid(out guid);

			// get a handle to all devices that are part of the HID class
			// Fun fact:  DIGCF_PRESENT worked on my machine just fine.  I reinstalled Vista, and now it no longer finds the Wiimote with that parameter enabled...
			IntPtr hDevInfo = HIDImports.SetupDiGetClassDevs(ref guid, null, IntPtr.Zero, HIDImports.DIGCF_DEVICEINTERFACE);// | HIDImports.DIGCF_PRESENT);

			// create a new interface data struct and initialize its size
			HIDImports.SP_DEVICE_INTERFACE_DATA diData = new HIDImports.SP_DEVICE_INTERFACE_DATA();
			diData.cbSize = Marshal.SizeOf(diData);

			// get a device interface to a single device (enumerate all devices)
			while(HIDImports.SetupDiEnumDeviceInterfaces(hDevInfo, IntPtr.Zero, ref guid, index, ref diData))
			{
				UInt32 size;

				// get the buffer size for this device detail instance (returned in the size parameter)
				HIDImports.SetupDiGetDeviceInterfaceDetail(hDevInfo, ref diData, IntPtr.Zero, 0, out size, IntPtr.Zero);

				// create a detail struct and set its size
				HIDImports.SP_DEVICE_INTERFACE_DETAIL_DATA diDetail = new HIDImports.SP_DEVICE_INTERFACE_DETAIL_DATA();

				// yeah, yeah...well, see, on Win x86, cbSize must be 5 for some reason.  On x64, apparently 8 is what it wants.
				// someday I should figure this out.  Thanks to Paul Miller on this...
				diDetail.cbSize = (uint)(IntPtr.Size == 8 ? 8 : 5);

				// actually get the detail struct
				if(HIDImports.SetupDiGetDeviceInterfaceDetail(hDevInfo, ref diData, ref diDetail, size, out size, IntPtr.Zero))
				{
					Debug.WriteLine(string.Format("{0}: {1} - {2}", index, diDetail.DevicePath, Marshal.GetLastWin32Error()));

					// open a read/write handle to our device using the DevicePath returned
					mHandle = HIDImports.CreateFile(diDetail.DevicePath, FileAccess.ReadWrite, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, HIDImports.EFileAttributes.Overlapped, IntPtr.Zero);

					// create an attributes struct and initialize the size
					HIDImports.HIDD_ATTRIBUTES attrib = new HIDImports.HIDD_ATTRIBUTES();
					attrib.Size = Marshal.SizeOf(attrib);

					// get the attributes of the current device
					if(HIDImports.HidD_GetAttributes(mHandle.DangerousGetHandle(), ref attrib))
					{
						// if the vendor and product IDs match up
						if(attrib.VendorID == VID && attrib.ProductID == PID)
						{
							// it's a Wiimote
							Debug.WriteLine("Found one!");
							found = true;

							// fire the callback function...if the callee doesn't care about more Wiimotes, break out
							if(!wiimoteFound(diDetail.DevicePath))
								break;
						}
					}
					mHandle.Close();
				}
				else
				{
					// failed to get the detail struct
					throw new WiimoteException("SetupDiGetDeviceInterfaceDetail failed on index " + index);
				}

				// move to the next device
				index++;
			}

			// clean up our list
			HIDImports.SetupDiDestroyDeviceInfoList(hDevInfo);

			// if we didn't find a Wiimote, throw an exception
			if(!found)
				throw new WiimoteNotFoundException("No Wiimotes found in HID device list.");
		}

		private bool WiimoteFound(string devicePath)
		{
			mDevicePath = devicePath;

			// if we didn't find a Wiimote, throw an exception
			OpenWiimoteDeviceHandle(mDevicePath);

			return false;
		}

		private void OpenWiimoteDeviceHandle(string devicePath)
		{
			// open a read/write handle to our device using the DevicePath returned
			mHandle = HIDImports.CreateFile(devicePath, FileAccess.ReadWrite, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, HIDImports.EFileAttributes.Overlapped, IntPtr.Zero);

			// create an attributes struct and initialize the size
			HIDImports.HIDD_ATTRIBUTES attrib = new HIDImports.HIDD_ATTRIBUTES();
			attrib.Size = Marshal.SizeOf(attrib);

			// get the attributes of the current device
			if(HIDImports.HidD_GetAttributes(mHandle.DangerousGetHandle(), ref attrib))
			{
				// if the vendor and product IDs match up
				if(attrib.VendorID == VID && attrib.ProductID == PID)
				{
					// create a nice .NET FileStream wrapping the handle above
					mStream = new FileStream(mHandle, FileAccess.ReadWrite, REPORT_LENGTH, true);

					// start an async read operation on it
					BeginAsyncRead();

					// read the calibration info from the controller
					try
					{
						ReadWiimoteCalibration();
					}
					catch
					{
						// if we fail above, try the alternate HID writes
						mAltWriteMethod = true;
						ReadWiimoteCalibration();
					}

					// force a status check 
                    // to get the state of any extensions plugged in at startup
                    // to set the data report type
                    GetStatus();

                    // enable status polling
                    // to detect motionplus
                    // to update the battery state
                    timerDelegate = new TimerCallback(CheckStatus);
                    stateTimer = new System.Threading.Timer(timerDelegate, true, POLLING_DUETIME, POLLING_PERIOD);
				}
				else
				{
					// otherwise this isn't the controller, so close up the file handle
					mHandle.Close();				
					throw new WiimoteException("Attempted to open a non-Wiimote device.");
				}
			}
		}

        /// <summary>
        /// Polling the status
        /// </summary>
        public void CheckStatus(Object stateInfo)
        {
            //Debug.WriteLine("Polling status");
            try
            {
                GetStatus();
            }
            catch
            {
                // Don't matter - we get the next one
            }
        }

        /// <summary>
        /// Calibrates the motionPlus
        /// </summary>
        public void CalibrateMotionPlus()
        {
            Debug.WriteLine("CalibrateMotionPlus");

            // Setup the calibration
            // Calibration is done within ParseExtension
            mCalibMotionPlus = true;
            mCalibMotionPlusNumValues = 10;
            mCalibMotionPlusList = new List<GyroState>();
        }

		/// <summary>
		/// Disconnect from the controller and stop reading data from it
		/// </summary>
		public void Disconnect()
		{
			// close up the stream and handle
			if(mStream != null)
				mStream.Close();

			if(mHandle != null)
				mHandle.Close();
		}

		/// <summary>
		/// Start reading asynchronously from the controller
		/// </summary>
		private void BeginAsyncRead()
		{
			// if the stream is valid and ready
			if(mStream != null && mStream.CanRead)
			{
				// setup the read and the callback
				byte[] buff = new byte[REPORT_LENGTH];
                try
                {
                    mStream.BeginRead(buff, 0, REPORT_LENGTH, new AsyncCallback(OnReadData), buff);
                }
                catch
                {
                    // this happens sometimes if application is closing
                    return;
                }
			}
		}

		/// <summary>
		/// Callback when data is ready to be processed
		/// </summary>
		/// <param name="ar">State information for the callback</param>
		private void OnReadData(IAsyncResult ar)
		{
            //Debug.WriteLine("OnReadData: " + System.Threading.Thread.CurrentThread.ManagedThreadId.ToString());

            // start reading again
            BeginAsyncRead();

			// grab the byte buffer
			byte[] buff = (byte[])ar.AsyncState;

            try
            {
                // end the current read
                mStream.EndRead(ar);

                // parse it
                if (ParseInputReport(buff))
                {
                    // post an event
                    if (WiimoteChanged != null)
                        WiimoteChanged(this, new WiimoteChangedEventArgs(mWiimoteState));
                }
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Exception: OperationCanceledException");
            }
            catch (IOException)
            {
                Debug.WriteLine("Exception: IOException - Wiimote disconnected?");
            }
		}

		/// <summary>
		/// Parse a report sent by the Wiimote
		/// </summary>
		/// <param name="buff">Data buffer to parse</param>
		/// <returns>Returns a boolean noting whether an event needs to be posted</returns>
		private bool ParseInputReport(byte[] buff)
		{
            InputReport type = (InputReport)buff[0];

			switch(type)
			{
				case InputReport.Buttons:
					ParseButtons(buff);
					break;
				case InputReport.ButtonsAccel:
					ParseButtons(buff);
					ParseAccel(buff);
					break;
				case InputReport.IRAccel:
					ParseButtons(buff);
					ParseAccel(buff);
					ParseIR(buff);
					break;
				case InputReport.ButtonsExtension:
					ParseButtons(buff);
					ParseExtension(buff, 3);
					break;
				case InputReport.ExtensionAccel:
					ParseButtons(buff);
					ParseAccel(buff);
					ParseExtension(buff, 6);
					break;
				case InputReport.IRExtensionAccel:
					ParseButtons(buff);
					ParseAccel(buff);
					ParseIR(buff);
					ParseExtension(buff, 16);
					break;
				case InputReport.Status:
                    ParseButtons(buff);
                    ParseStatus(buff);
					break;
				case InputReport.ReadData:
					ParseButtons(buff);
					ParseReadData(buff);
					break;
				case InputReport.OutputReportAck:
                    ParseButtons(buff);
                    ParseOutputReportAck(buff);
					break;
				default:
					Debug.WriteLine("Unknown report type: " + type.ToString("x"));
					return false;
			}

			return true;
		}
                
        /// <summary>
		/// Parse status report
		/// </summary>
		/// <param name="buff">Data buffer</param>
        private void ParseStatus(byte[] buff)
        {
            bool requestedStatus = false;
            if (mStatusRequest.WaitOne(0))
            {
                // this was our requested status
                mStatusDone.Set();
                requestedStatus = true;
            }

            mStatusMutex.WaitOne();

            Debug.WriteLine("******** STATUS ********");

            mWiimoteState.BatteryRaw = buff[6];
            mWiimoteState.Battery = (((100.0f * 48.0f * (float)((int)buff[6] / 48.0f))) / 192.0f);

            // get the real LED values in case the values from SetLEDs() somehow becomes out of sync, which really shouldn't be possible
            mWiimoteState.LEDState.LED1 = (buff[3] & 0x10) != 0;
            mWiimoteState.LEDState.LED2 = (buff[3] & 0x20) != 0;
            mWiimoteState.LEDState.LED3 = (buff[3] & 0x40) != 0;
            mWiimoteState.LEDState.LED4 = (buff[3] & 0x80) != 0;

            // extension connected?
            // no detection of MotionPlus
            //bool extension = (buff[3] & 0x02) != 0;
            //Debug.WriteLine("Extension, Old: " + mWiimoteState.Extension + ", New: " + extension);

            ExtensionType extension = IdentifyExtension();
            Debug.WriteLine("ExtensionType, Old: " + mWiimoteState.ExtensionType.ToString() + ", New: " + extension.ToString());
            if (extension != mWiimoteState.ExtensionType)
            {
                // extension identifier has changed
                InitializeExtension(extension);
            }

            if (!requestedStatus)
            { 
                // this was a not requested status report 

                // if this report is received when not requested, 
                // the application 'MUST' send report 0x12 to change the data reporting mode, 
                // otherwise no further data reports will be received. 
                SetReportType(mWiimoteState.InputReport, true);
            }

            mStatusMutex.ReleaseMutex();
        }

        /// <summary>
		/// Identify extension when plugged in
		/// </summary>
        private ExtensionType IdentifyExtension()
        {
            Debug.WriteLine("IdentifyExtension");

            ExtensionType type = ExtensionType.None;

            // identify MotionPlus
            byte[] buff;
            long extension;
            long motionplus;
            int retry = 3;
            do
            {
                try
                {
                    buff = ReadData(REGISTER_EXTENSION_TYPE, 6);
                    extension = ((long)buff[0] << 40) | ((long)buff[1] << 32) | ((long)buff[2]) << 24 | ((long)buff[3]) << 16 | ((long)buff[4]) << 8 | buff[5];
                    break;
                }
                catch
                {
                    Debug.WriteLine("Exception: Reading extension register");
                    extension = 0;
                    retry--;
                }
            }
            while (retry > 0);

            retry = 3;
            do
            {
                try
                {
                    buff = ReadData(REGISTER_MOTIONPLUS_TYPE, 6);
                    motionplus = ((long)buff[0] << 40) | ((long)buff[1] << 32) | ((long)buff[2]) << 24 | ((long)buff[3]) << 16 | ((long)buff[4]) << 8 | buff[5];
                    break;
                }
                catch
                {
                    Debug.WriteLine("Exception: Reading motionplus register");
                    motionplus = 0;
                    retry--;
                }
            }
            while (retry > 0);

            Debug.WriteLine("EX_ID: " + string.Format("{0:x}", extension) + " - MP_ID: " + string.Format("{0:x}", motionplus));

            switch ((ExtensionID)extension)
            {
                case ExtensionID.None:
                    switch ((MotionPlusID)motionplus)
                    {
                        case MotionPlusID.MotionPlusInactive:
                        case MotionPlusID.MotionPlusNoLongerActive:
                        case MotionPlusID.MotionPlusNunchukNoLongerActive:
                        case MotionPlusID.MotionPlusClassicControllerNoLongerActive:
                            type = ExtensionType.MotionPlus;
                            break;
                        default:
                            type = ExtensionType.None;
                            break;
                    }
                    break;

                case ExtensionID.PartiallyInserted:
                    switch ((MotionPlusID)motionplus)
                    {
                        case MotionPlusID.MotionPlusInactive:
                        case MotionPlusID.MotionPlusNoLongerActive:
                        case MotionPlusID.MotionPlusNunchukNoLongerActive:
                        case MotionPlusID.MotionPlusClassicControllerNoLongerActive:
                            type = ExtensionType.MotionPlusPartiallyInserted;
                            break;
                        default:
                            type = ExtensionType.PartiallyInserted;
                            break;
                    }
                    break;

                case ExtensionID.Guitar:
                    type = ExtensionType.Guitar;
                    break;

                case ExtensionID.BalanceBoard:
                    type = ExtensionType.BalanceBoard;
                    break;

                case ExtensionID.Drums:
                    type = ExtensionType.Drums;
                    break;

                case ExtensionID.TaikoDrum:
                    type = ExtensionType.TaikoDrum;
                    break;

                case ExtensionID.Nunchuk:
                    switch ((MotionPlusID)motionplus)
                    {
                        case MotionPlusID.MotionPlusInactive:
                            type = ExtensionType.MotionPlusNunchuk;
                            break;
                        case MotionPlusID.MotionPlusNoLongerActive:
                        case MotionPlusID.MotionPlusNunchukNoLongerActive:
                            type = ExtensionType.NunchukThroughMotionPlus;
                            break;
                        case MotionPlusID.MotionPlusClassicControllerNoLongerActive:
                            type = ExtensionType.MotionPlusPartiallyInserted;
                            break;
                        default:
                            type = ExtensionType.Nunchuk;
                            break;
                    }
                    break;

                case ExtensionID.ClassicController:
                    switch ((MotionPlusID)motionplus)
                    {
                        case MotionPlusID.MotionPlusInactive:
                            type = ExtensionType.MotionPlusClassicController;
                            break;
                        case MotionPlusID.MotionPlusNoLongerActive:
                        case MotionPlusID.MotionPlusClassicControllerNoLongerActive:
                            type = ExtensionType.ClassicControllerThroughMotionPlus;
                            break;
                        case MotionPlusID.MotionPlusNunchukNoLongerActive:
                            type = ExtensionType.MotionPlusPartiallyInserted;
                            break;
                        default:
                            type = ExtensionType.ClassicController;
                            break;
                    }
                    break;

                case ExtensionID.MotionPlus:
                    type = ExtensionType.MotionPlus;
                    break;

                case ExtensionID.MotionPlusNunchuk:
                    type = ExtensionType.MotionPlusNunchuk;
                    break;

                case ExtensionID.MotionPlusClassicController:
                    type = ExtensionType.MotionPlusClassicController;
                    break;

                default:
                    type = ExtensionType.None;
                    break;
            }

            return (ExtensionType)type;
        }

		/// <summary>
		/// Handles setting up an extension when plugged in
		/// </summary>
        private void InitializeExtension(ExtensionType type)
		{          
            mInitMutex.WaitOne();
            mStatusMutex.WaitOne();

            Debug.WriteLine("InitializeExtension: " + type.ToString());
 
            switch ((ExtensionType)type)
            {
                case ExtensionType.None:
                    mWiimoteState.Extension = false;
                    mWiimoteState.ExtensionType = ExtensionType.None;
                    mWiimoteState.InputReport = InputReport.IRAccel;
                    break;

                case ExtensionType.BalanceBoard:
                    // activate BalanceBoard
                    WriteData(REGISTER_EXTENSION_INIT_1, 0x55);
                    WriteData(REGISTER_EXTENSION_INIT_2, 0x00);
                    ReadExtensionCalibration(type);
                    mWiimoteState.Extension = true;
                    mWiimoteState.ExtensionType = type;
                    mWiimoteState.InputReport = InputReport.ButtonsExtension;
                    break;

                case ExtensionType.PartiallyInserted:
                case ExtensionType.Nunchuk:
                case ExtensionType.ClassicController:
                case ExtensionType.Guitar:
                case ExtensionType.Drums:
                case ExtensionType.TaikoDrum:
                    // activate unknown extension plugged into wiimote
                    WriteData(REGISTER_EXTENSION_INIT_1, 0x55);
                    WriteData(REGISTER_EXTENSION_INIT_2, 0x00);
                    // now we can read the identifier of the extension
                    type = IdentifyExtension();
                    ReadExtensionCalibration(type);
                    mWiimoteState.Extension = true;
                    mWiimoteState.ExtensionType = type;
                    mWiimoteState.InputReport = InputReport.IRExtensionAccel;
                    break;

                case ExtensionType.MotionPlus:
                    // activate MotionPlus
                    // MotionPlus is mapped to 0x04a60000!
                    // this will sent a status message automatically
                    WriteData(REGISTER_MOTIONPLUS_INIT, 0x04);
                    ReadExtensionCalibration(type);
                    mWiimoteState.Extension = true;
                    mWiimoteState.ExtensionType = type;
                    mWiimoteState.InputReport = InputReport.IRExtensionAccel;
                    break;

                case ExtensionType.MotionPlusPartiallyInserted:
                    // activate unknown extension plugged into MotionPlus
                    // this will deactivate MotionPlus
                    // this will sent a status message automatically
                    WriteData(REGISTER_EXTENSION_INIT_1, 0x55);
                    // the remapping takes some time
                    Thread.Sleep(500);
                    mWiimoteState.ExtensionType = type;
                    type = IdentifyExtension();
                    // activate pass-through
                    if (type == ExtensionType.NunchukThroughMotionPlus || type == ExtensionType.MotionPlusNunchuk)
                    {
                        // Nunchuk extension plugged into MotionPlus is activated
                        ReadExtensionCalibration(ExtensionType.Nunchuk);
                        // activate pass-through
                        InitializeExtension(ExtensionType.MotionPlusNunchuk);
                    }
                    else if (type == ExtensionType.ClassicControllerThroughMotionPlus || type == ExtensionType.MotionPlusClassicController)
                    {
                        // ClassicController extension plugged into MotionPlus is activated
                        ReadExtensionCalibration(ExtensionType.ClassicController);
                        // activate pass-through
                        InitializeExtension(ExtensionType.MotionPlusClassicController);
                    }
                    else
                    {
                        // another extension detected
                        InitializeExtension(type);
                    }
                    break;

                case ExtensionType.MotionPlusNunchuk:
                    // check previous state
                    if (mWiimoteState.ExtensionType == ExtensionType.MotionPlusPartiallyInserted)
                    {
                        // Nunchuk extension plugged into MotionPlus is activated
                        // activate pass-through
                        WriteData(REGISTER_MOTIONPLUS_INIT, 0x05);
                        //Thread.Sleep(500);
                        type = IdentifyExtension();
                        ReadExtensionCalibration(type);
                        mWiimoteState.Extension = true;
                        mWiimoteState.ExtensionType = type;
                        mWiimoteState.InputReport = InputReport.IRExtensionAccel;
                    }
                    else
                    {
                        InitializeExtension(ExtensionType.MotionPlusPartiallyInserted);
                    }
                    break;

                case ExtensionType.MotionPlusClassicController:
                    // check previous state
                    if (mWiimoteState.ExtensionType == ExtensionType.MotionPlusPartiallyInserted)
                    {
                        // ClassicController extension plugged into MotionPlus is activated
                        // activate pass-through
                        WriteData(REGISTER_MOTIONPLUS_INIT, 0x07);
                        //Thread.Sleep(500);
                        type = IdentifyExtension();
                        ReadExtensionCalibration(type);
                        mWiimoteState.Extension = true;
                        mWiimoteState.ExtensionType = type;
                        mWiimoteState.InputReport = InputReport.IRExtensionAccel;
                    }
                    else
                    {
                        InitializeExtension(ExtensionType.MotionPlusPartiallyInserted);
                    }
                    break;

                case ExtensionType.MotionPlusPassThroughDisable:
                    // check previous state
                    if (mWiimoteState.ExtensionType == ExtensionType.MotionPlusNunchuk || mWiimoteState.ExtensionType == ExtensionType.MotionPlusClassicController)
                    {
                        // activate MotionPlus
                        // MotionPlus is already mapped as normal extension 0x04a40000!
                        // this will sent a status message automatically
                        WriteData(REGISTER_EXTENSION_TYPE_2, 0x04);
                        mWiimoteState.Extension = true;
                        mWiimoteState.ExtensionType = ExtensionType.MotionPlus;
                        mWiimoteState.InputReport = InputReport.IRExtensionAccel;
                    }
                    else
                    {
                        InitializeExtension(ExtensionType.MotionPlus);
                    }
                    break; 

                case ExtensionType.NunchukThroughMotionPlus:
                case ExtensionType.ClassicControllerThroughMotionPlus:
                    // check previous state
                    if (mWiimoteState.ExtensionType == ExtensionType.MotionPlus || mWiimoteState.ExtensionType == ExtensionType.MotionPlusNunchuk || mWiimoteState.ExtensionType == ExtensionType.MotionPlusClassicController)
                    {
                        // activate unknown extension plugged into MotionPlus
                        // this will deactivate MotionPlus
                        // this will sent a status message automatically
                        WriteData(REGISTER_EXTENSION_INIT_1, 0x55);
                        Thread.Sleep(500);
                        type = IdentifyExtension();
                        ReadExtensionCalibration(type);
                        mWiimoteState.Extension = true;
                        mWiimoteState.ExtensionType = type;
                        mWiimoteState.InputReport = InputReport.IRExtensionAccel;
                    }
                    else
                    {
                        InitializeExtension(ExtensionType.MotionPlusPartiallyInserted);
                    }
                    break;
                
                default:
                    {
                        mInitMutex.ReleaseMutex();
                        mStatusMutex.ReleaseMutex();
                        throw new WiimoteException("Unknown extension controller found: " + type.ToString("x"));
                    }
            }
            mInitMutex.ReleaseMutex();
            mStatusMutex.ReleaseMutex();
            ExtensionChanged();
        }

        /// <summary>
        /// Read extension calibration data
        /// </summary>
        private void ReadExtensionCalibration(ExtensionType type)    
        {
            Debug.WriteLine("ReadExtensionCalibration: " + type.ToString());

            byte[] buff;

			switch(type)
			{
				case ExtensionType.Nunchuk:
                case ExtensionType.NunchukThroughMotionPlus:
					buff = ReadData(REGISTER_EXTENSION_CALIBRATION, 16);

                    if (buff.Length == 16)
                    {
                        mWiimoteState.NunchukState.CalibrationInfo.AccelCalibration.X0 = (uint)((buff[0] << 2) | (buff[3] & 0x03));
                        mWiimoteState.NunchukState.CalibrationInfo.AccelCalibration.Y0 = (uint)((buff[1] << 2) | (buff[3] >> 2 & 0x03));
                        mWiimoteState.NunchukState.CalibrationInfo.AccelCalibration.Z0 = (uint)((buff[2] << 2) | (buff[3] >> 4 & 0x03));
                        mWiimoteState.NunchukState.CalibrationInfo.AccelCalibration.XG = (uint)((buff[4] << 2) | (buff[3] & 0x03));
                        mWiimoteState.NunchukState.CalibrationInfo.AccelCalibration.YG = (uint)((buff[5] << 2) | (buff[3] >> 2 & 0x03));
                        mWiimoteState.NunchukState.CalibrationInfo.AccelCalibration.ZG = (uint)((buff[6] << 2) | (buff[3] >> 4 & 0x03));

                        mWiimoteState.NunchukState.CalibrationInfo.MaxX = buff[8];
                        mWiimoteState.NunchukState.CalibrationInfo.MinX = buff[9];
                        mWiimoteState.NunchukState.CalibrationInfo.MidX = buff[10];
                        mWiimoteState.NunchukState.CalibrationInfo.MaxY = buff[11];
                        mWiimoteState.NunchukState.CalibrationInfo.MinY = buff[12];
                        mWiimoteState.NunchukState.CalibrationInfo.MidY = buff[13];
                    }
					break;

				case ExtensionType.ClassicController:
                case ExtensionType.ClassicControllerThroughMotionPlus:
					buff = ReadData(REGISTER_EXTENSION_CALIBRATION, 16);

                    if (buff.Length == 16)
                    {
                        mWiimoteState.ClassicControllerState.CalibrationInfo.MaxXL = (byte)(buff[0] >> 2);
                        mWiimoteState.ClassicControllerState.CalibrationInfo.MinXL = (byte)(buff[1] >> 2);
                        mWiimoteState.ClassicControllerState.CalibrationInfo.MidXL = (byte)(buff[2] >> 2);
                        mWiimoteState.ClassicControllerState.CalibrationInfo.MaxYL = (byte)(buff[3] >> 2);
                        mWiimoteState.ClassicControllerState.CalibrationInfo.MinYL = (byte)(buff[4] >> 2);
                        mWiimoteState.ClassicControllerState.CalibrationInfo.MidYL = (byte)(buff[5] >> 2);

                        mWiimoteState.ClassicControllerState.CalibrationInfo.MaxXR = (byte)(buff[6] >> 3);
                        mWiimoteState.ClassicControllerState.CalibrationInfo.MinXR = (byte)(buff[7] >> 3);
                        mWiimoteState.ClassicControllerState.CalibrationInfo.MidXR = (byte)(buff[8] >> 3);
                        mWiimoteState.ClassicControllerState.CalibrationInfo.MaxYR = (byte)(buff[9] >> 3);
                        mWiimoteState.ClassicControllerState.CalibrationInfo.MinYR = (byte)(buff[10] >> 3);
                        mWiimoteState.ClassicControllerState.CalibrationInfo.MidYR = (byte)(buff[11] >> 3);

                        // this doesn't seem right...
                        //					mWiimoteState.ClassicControllerState.AccelCalibrationInfo.MinTriggerL = (byte)(buff[12] >> 3);
                        //					mWiimoteState.ClassicControllerState.AccelCalibrationInfo.MaxTriggerL = (byte)(buff[14] >> 3);
                        //					mWiimoteState.ClassicControllerState.AccelCalibrationInfo.MinTriggerR = (byte)(buff[13] >> 3);
                        //					mWiimoteState.ClassicControllerState.AccelCalibrationInfo.MaxTriggerR = (byte)(buff[15] >> 3);
                        mWiimoteState.ClassicControllerState.CalibrationInfo.MinTriggerL = 0;
                        mWiimoteState.ClassicControllerState.CalibrationInfo.MaxTriggerL = 31;
                        mWiimoteState.ClassicControllerState.CalibrationInfo.MinTriggerR = 0;
                        mWiimoteState.ClassicControllerState.CalibrationInfo.MaxTriggerR = 31;
                    }
					break;

				case ExtensionType.BalanceBoard:
					buff = ReadData(REGISTER_EXTENSION_CALIBRATION, 32);

                    if (buff.Length == 32)
                    {
                        mWiimoteState.BalanceBoardState.CalibrationInfo.Kg0.TopRight = (short)((short)buff[4] << 8 | buff[5]);
                        mWiimoteState.BalanceBoardState.CalibrationInfo.Kg0.BottomRight = (short)((short)buff[6] << 8 | buff[7]);
                        mWiimoteState.BalanceBoardState.CalibrationInfo.Kg0.TopLeft = (short)((short)buff[8] << 8 | buff[9]);
                        mWiimoteState.BalanceBoardState.CalibrationInfo.Kg0.BottomLeft = (short)((short)buff[10] << 8 | buff[11]);

                        mWiimoteState.BalanceBoardState.CalibrationInfo.Kg17.TopRight = (short)((short)buff[12] << 8 | buff[13]);
                        mWiimoteState.BalanceBoardState.CalibrationInfo.Kg17.BottomRight = (short)((short)buff[14] << 8 | buff[15]);
                        mWiimoteState.BalanceBoardState.CalibrationInfo.Kg17.TopLeft = (short)((short)buff[16] << 8 | buff[17]);
                        mWiimoteState.BalanceBoardState.CalibrationInfo.Kg17.BottomLeft = (short)((short)buff[18] << 8 | buff[19]);

                        mWiimoteState.BalanceBoardState.CalibrationInfo.Kg34.TopRight = (short)((short)buff[20] << 8 | buff[21]);
                        mWiimoteState.BalanceBoardState.CalibrationInfo.Kg34.BottomRight = (short)((short)buff[22] << 8 | buff[23]);
                        mWiimoteState.BalanceBoardState.CalibrationInfo.Kg34.TopLeft = (short)((short)buff[24] << 8 | buff[25]);
                        mWiimoteState.BalanceBoardState.CalibrationInfo.Kg34.BottomLeft = (short)((short)buff[26] << 8 | buff[27]);
                    }
					break;

                case ExtensionType.MotionPlus:
                case ExtensionType.MotionPlusNunchuk:
                case ExtensionType.MotionPlusClassicController:
                    buff = ReadData(REGISTER_MOTIONPLUS_CALIBRATION, 32);

                    if (buff.Length == 32)
                    {
                        // gyro calibration - seems to be OK but not very accurat
                        mWiimoteState.MotionPlusState.CalibrationInfo.GyroCalibration.Yaw0 = (uint)((buff[0] << 6) | (buff[1] >> 2));
                        mWiimoteState.MotionPlusState.CalibrationInfo.GyroCalibration.Roll0 = (uint)((buff[2] << 6) | buff[3] >> 2);
                        mWiimoteState.MotionPlusState.CalibrationInfo.GyroCalibration.Pitch0 = (uint)((buff[4] << 6) | buff[5] >> 2);

                        // this doesn't seem right...
                        //mWiimoteState.MotionPlusState.CalibrationInfo.GyroCalibration.YawG = (uint)((buff[16] << 6) | buff[17] >> 2);
                        //mWiimoteState.MotionPlusState.CalibrationInfo.GyroCalibration.RollG = (uint)((buff[18] << 6) | buff[19] >> 2);
                        //mWiimoteState.MotionPlusState.CalibrationInfo.GyroCalibration.PitchG = (uint)((buff[20] << 6) | buff[21] >> 2);
                    }
                    break;
                case ExtensionType.Guitar:
				case ExtensionType.Drums:
				case ExtensionType.TaikoDrum:
					// there appears to be no calibration for these controllers
					break;
			}
		}

        /// <summary>
        /// Fire the extension changed event
        /// </summary>
        private void ExtensionChanged()
        {
            Debug.WriteLine("ExtensionChanged: " + mWiimoteState.ExtensionType.ToString());

            // only fire the extension changed event if we have a real extension (i.e. not a balance board)
            //if (WiimoteExtensionChanged != null && mWiimoteState.ExtensionType != ExtensionType.BalanceBoard)
            //    WiimoteExtensionChanged(this, new WiimoteExtensionChangedEventArgs(mWiimoteState.ExtensionType, mWiimoteState.Extension));

            if (WiimoteExtensionChanged != null)
                WiimoteExtensionChanged(this, new WiimoteExtensionChangedEventArgs(mWiimoteState.ExtensionType, mWiimoteState.Extension));
        }

		/// <summary>
		/// Decrypts data sent from the extension to the Wiimote
		/// </summary>
		/// <param name="buff">Data buffer</param>
		/// <returns>Byte array containing decoded data</returns>
		private byte[] DecryptBuffer(byte[] buff)
		{
			for(int i = 0; i < buff.Length; i++)
				buff[i] = (byte)(((buff[i] ^ 0x17) + 0x17) & 0xff);

			return buff;
		}

		/// <summary>
		/// Parses a standard button report into the ButtonState struct
		/// </summary>
		/// <param name="buff">Data buffer</param>
		private void ParseButtons(byte[] buff)
		{
			mWiimoteState.ButtonState.A		= (buff[2] & 0x08) != 0;
			mWiimoteState.ButtonState.B		= (buff[2] & 0x04) != 0;
			mWiimoteState.ButtonState.Minus	= (buff[2] & 0x10) != 0;
			mWiimoteState.ButtonState.Home	= (buff[2] & 0x80) != 0;
			mWiimoteState.ButtonState.Plus	= (buff[1] & 0x10) != 0;
			mWiimoteState.ButtonState.One	= (buff[2] & 0x02) != 0;
			mWiimoteState.ButtonState.Two	= (buff[2] & 0x01) != 0;
			mWiimoteState.ButtonState.Up	= (buff[1] & 0x08) != 0;
			mWiimoteState.ButtonState.Down	= (buff[1] & 0x04) != 0;
			mWiimoteState.ButtonState.Left	= (buff[1] & 0x01) != 0;
			mWiimoteState.ButtonState.Right	= (buff[1] & 0x02) != 0;
		}

		/// <summary>
		/// Parse accelerometer data
		/// </summary>
		/// <param name="buff">Data buffer</param>
		private void ParseAccel(byte[] buff)
		{
			mWiimoteState.AccelState.RawValues.X = ((buff[3]<<2) | (buff[1]>>5&0x03));
			mWiimoteState.AccelState.RawValues.Y = ((buff[4]<<2) | (buff[2]>>5&0x02));
			mWiimoteState.AccelState.RawValues.Z = ((buff[5]<<2) | (buff[2]>>5&0x02));

			mWiimoteState.AccelState.Values.X = (float)((float)mWiimoteState.AccelState.RawValues.X - ((int)mWiimoteState.AccelCalibrationInfo.X0)) / 
											((float)mWiimoteState.AccelCalibrationInfo.XG - ((int)mWiimoteState.AccelCalibrationInfo.X0));
			mWiimoteState.AccelState.Values.Y = (float)((float)mWiimoteState.AccelState.RawValues.Y - mWiimoteState.AccelCalibrationInfo.Y0) /
											((float)mWiimoteState.AccelCalibrationInfo.YG - mWiimoteState.AccelCalibrationInfo.Y0);
			mWiimoteState.AccelState.Values.Z = (float)((float)mWiimoteState.AccelState.RawValues.Z - mWiimoteState.AccelCalibrationInfo.Z0) /
											((float)mWiimoteState.AccelCalibrationInfo.ZG - mWiimoteState.AccelCalibrationInfo.Z0);
		}

		/// <summary>
		/// Parse IR data from report
		/// </summary>
		/// <param name="buff">Data buffer</param>
		private void ParseIR(byte[] buff)
		{
			mWiimoteState.IRState.IRSensors[0].RawPosition.X = buff[6] | ((buff[8] >> 4) & 0x03) << 8;
			mWiimoteState.IRState.IRSensors[0].RawPosition.Y = buff[7] | ((buff[8] >> 6) & 0x03) << 8;

			switch(mWiimoteState.IRState.Mode)
			{
				case IRMode.Basic:
					mWiimoteState.IRState.IRSensors[1].RawPosition.X = buff[9]  | ((buff[8] >> 0) & 0x03) << 8;
					mWiimoteState.IRState.IRSensors[1].RawPosition.Y = buff[10] | ((buff[8] >> 2) & 0x03) << 8;

					mWiimoteState.IRState.IRSensors[2].RawPosition.X = buff[11] | ((buff[13] >> 4) & 0x03) << 8;
					mWiimoteState.IRState.IRSensors[2].RawPosition.Y = buff[12] | ((buff[13] >> 6) & 0x03) << 8;

					mWiimoteState.IRState.IRSensors[3].RawPosition.X = buff[14] | ((buff[13] >> 0) & 0x03) << 8;
					mWiimoteState.IRState.IRSensors[3].RawPosition.Y = buff[15] | ((buff[13] >> 2) & 0x03) << 8;

					mWiimoteState.IRState.IRSensors[0].Size = 0x00;
					mWiimoteState.IRState.IRSensors[1].Size = 0x00;
					mWiimoteState.IRState.IRSensors[2].Size = 0x00;
					mWiimoteState.IRState.IRSensors[3].Size = 0x00;

					mWiimoteState.IRState.IRSensors[0].Found = !(buff[6] == 0xff && buff[7] == 0xff);
					mWiimoteState.IRState.IRSensors[1].Found = !(buff[9] == 0xff && buff[10] == 0xff);
					mWiimoteState.IRState.IRSensors[2].Found = !(buff[11] == 0xff && buff[12] == 0xff);
					mWiimoteState.IRState.IRSensors[3].Found = !(buff[14] == 0xff && buff[15] == 0xff);
					break;
				case IRMode.Extended:
					mWiimoteState.IRState.IRSensors[1].RawPosition.X = buff[9]  | ((buff[11] >> 4) & 0x03) << 8;
					mWiimoteState.IRState.IRSensors[1].RawPosition.Y = buff[10] | ((buff[11] >> 6) & 0x03) << 8;
					mWiimoteState.IRState.IRSensors[2].RawPosition.X = buff[12] | ((buff[14] >> 4) & 0x03) << 8;
					mWiimoteState.IRState.IRSensors[2].RawPosition.Y = buff[13] | ((buff[14] >> 6) & 0x03) << 8;
					mWiimoteState.IRState.IRSensors[3].RawPosition.X = buff[15] | ((buff[17] >> 4) & 0x03) << 8;
					mWiimoteState.IRState.IRSensors[3].RawPosition.Y = buff[16] | ((buff[17] >> 6) & 0x03) << 8;

					mWiimoteState.IRState.IRSensors[0].Size = buff[8] & 0x0f;
					mWiimoteState.IRState.IRSensors[1].Size = buff[11] & 0x0f;
					mWiimoteState.IRState.IRSensors[2].Size = buff[14] & 0x0f;
					mWiimoteState.IRState.IRSensors[3].Size = buff[17] & 0x0f;

					mWiimoteState.IRState.IRSensors[0].Found = !(buff[6] == 0xff && buff[7] == 0xff && buff[8] == 0xff);
					mWiimoteState.IRState.IRSensors[1].Found = !(buff[9] == 0xff && buff[10] == 0xff && buff[11] == 0xff);
					mWiimoteState.IRState.IRSensors[2].Found = !(buff[12] == 0xff && buff[13] == 0xff && buff[14] == 0xff);
					mWiimoteState.IRState.IRSensors[3].Found = !(buff[15] == 0xff && buff[16] == 0xff && buff[17] == 0xff);
					break;
			}

			mWiimoteState.IRState.IRSensors[0].Position.X = (float)(mWiimoteState.IRState.IRSensors[0].RawPosition.X / 1023.5f);
			mWiimoteState.IRState.IRSensors[1].Position.X = (float)(mWiimoteState.IRState.IRSensors[1].RawPosition.X / 1023.5f);
			mWiimoteState.IRState.IRSensors[2].Position.X = (float)(mWiimoteState.IRState.IRSensors[2].RawPosition.X / 1023.5f);
			mWiimoteState.IRState.IRSensors[3].Position.X = (float)(mWiimoteState.IRState.IRSensors[3].RawPosition.X / 1023.5f);

			mWiimoteState.IRState.IRSensors[0].Position.Y = (float)(mWiimoteState.IRState.IRSensors[0].RawPosition.Y / 767.5f);
			mWiimoteState.IRState.IRSensors[1].Position.Y = (float)(mWiimoteState.IRState.IRSensors[1].RawPosition.Y / 767.5f);
			mWiimoteState.IRState.IRSensors[2].Position.Y = (float)(mWiimoteState.IRState.IRSensors[2].RawPosition.Y / 767.5f);
			mWiimoteState.IRState.IRSensors[3].Position.Y = (float)(mWiimoteState.IRState.IRSensors[3].RawPosition.Y / 767.5f);

			if(mWiimoteState.IRState.IRSensors[0].Found && mWiimoteState.IRState.IRSensors[1].Found)
			{
				mWiimoteState.IRState.RawMidpoint.X = (mWiimoteState.IRState.IRSensors[1].RawPosition.X + mWiimoteState.IRState.IRSensors[0].RawPosition.X) / 2;
				mWiimoteState.IRState.RawMidpoint.Y = (mWiimoteState.IRState.IRSensors[1].RawPosition.Y + mWiimoteState.IRState.IRSensors[0].RawPosition.Y) / 2;
		
				mWiimoteState.IRState.Midpoint.X = (mWiimoteState.IRState.IRSensors[1].Position.X + mWiimoteState.IRState.IRSensors[0].Position.X) / 2.0f;
				mWiimoteState.IRState.Midpoint.Y = (mWiimoteState.IRState.IRSensors[1].Position.Y + mWiimoteState.IRState.IRSensors[0].Position.Y) / 2.0f;
			}
			else
				mWiimoteState.IRState.Midpoint.X = mWiimoteState.IRState.Midpoint.Y = 0.0f;
		}

		/// <summary>
		/// Parse data from an extension controller
		/// </summary>
		/// <param name="buff">Data buffer</param>
		/// <param name="offset">Offset into data buffer</param>
		private void ParseExtension(byte[] buff, int offset)
		{
			switch(mWiimoteState.ExtensionType)
			{
				case ExtensionType.Nunchuk:
					mWiimoteState.NunchukState.RawJoystick.X = buff[offset];
					mWiimoteState.NunchukState.RawJoystick.Y = buff[offset + 1];

                    mWiimoteState.NunchukState.AccelState.RawValues.X = (buff[offset + 2] << 2) | (buff[offset + 5] >> 2 & 0x03);
                    mWiimoteState.NunchukState.AccelState.RawValues.Y = (buff[offset + 3] << 2) | (buff[offset + 5] >> 4 & 0x03);
                    mWiimoteState.NunchukState.AccelState.RawValues.Z = (buff[offset + 4] << 2) | (buff[offset + 5] >> 6 & 0x03);

					mWiimoteState.NunchukState.C = (buff[offset + 5] & 0x02) == 0;
					mWiimoteState.NunchukState.Z = (buff[offset + 5] & 0x01) == 0;

					mWiimoteState.NunchukState.AccelState.Values.X = (float)((float)mWiimoteState.NunchukState.AccelState.RawValues.X - mWiimoteState.NunchukState.CalibrationInfo.AccelCalibration.X0) / 
													((float)mWiimoteState.NunchukState.CalibrationInfo.AccelCalibration.XG - mWiimoteState.NunchukState.CalibrationInfo.AccelCalibration.X0);
					mWiimoteState.NunchukState.AccelState.Values.Y = (float)((float)mWiimoteState.NunchukState.AccelState.RawValues.Y - mWiimoteState.NunchukState.CalibrationInfo.AccelCalibration.Y0) /
													((float)mWiimoteState.NunchukState.CalibrationInfo.AccelCalibration.YG - mWiimoteState.NunchukState.CalibrationInfo.AccelCalibration.Y0);
					mWiimoteState.NunchukState.AccelState.Values.Z = (float)((float)mWiimoteState.NunchukState.AccelState.RawValues.Z - mWiimoteState.NunchukState.CalibrationInfo.AccelCalibration.Z0) /
													((float)mWiimoteState.NunchukState.CalibrationInfo.AccelCalibration.ZG - mWiimoteState.NunchukState.CalibrationInfo.AccelCalibration.Z0);

					if(mWiimoteState.NunchukState.CalibrationInfo.MaxX != 0x00)
						mWiimoteState.NunchukState.Joystick.X = (float)((float)mWiimoteState.NunchukState.RawJoystick.X - mWiimoteState.NunchukState.CalibrationInfo.MidX) / 
												((float)mWiimoteState.NunchukState.CalibrationInfo.MaxX - mWiimoteState.NunchukState.CalibrationInfo.MinX);

					if(mWiimoteState.NunchukState.CalibrationInfo.MaxY != 0x00)
						mWiimoteState.NunchukState.Joystick.Y = (float)((float)mWiimoteState.NunchukState.RawJoystick.Y - mWiimoteState.NunchukState.CalibrationInfo.MidY) / 
												((float)mWiimoteState.NunchukState.CalibrationInfo.MaxY - mWiimoteState.NunchukState.CalibrationInfo.MinY);
					break;

				case ExtensionType.ClassicController:
					mWiimoteState.ClassicControllerState.RawJoystickL.X = (byte)(buff[offset] & 0x3f);
					mWiimoteState.ClassicControllerState.RawJoystickL.Y = (byte)(buff[offset + 1] & 0x3f);
					mWiimoteState.ClassicControllerState.RawJoystickR.X = (byte)((buff[offset + 2] >> 7) | (buff[offset + 1] & 0xc0) >> 5 | (buff[offset] & 0xc0) >> 3);
					mWiimoteState.ClassicControllerState.RawJoystickR.Y = (byte)(buff[offset + 2] & 0x1f);

					mWiimoteState.ClassicControllerState.RawTriggerL = (byte)(((buff[offset + 2] & 0x60) >> 2) | (buff[offset + 3] >> 5));
					mWiimoteState.ClassicControllerState.RawTriggerR = (byte)(buff[offset + 3] & 0x1f);

					mWiimoteState.ClassicControllerState.ButtonState.TriggerR	= (buff[offset + 4] & 0x02) == 0;
					mWiimoteState.ClassicControllerState.ButtonState.Plus		= (buff[offset + 4] & 0x04) == 0;
					mWiimoteState.ClassicControllerState.ButtonState.Home		= (buff[offset + 4] & 0x08) == 0;
					mWiimoteState.ClassicControllerState.ButtonState.Minus		= (buff[offset + 4] & 0x10) == 0;
					mWiimoteState.ClassicControllerState.ButtonState.TriggerL	= (buff[offset + 4] & 0x20) == 0;
					mWiimoteState.ClassicControllerState.ButtonState.Down		= (buff[offset + 4] & 0x40) == 0;
					mWiimoteState.ClassicControllerState.ButtonState.Right		= (buff[offset + 4] & 0x80) == 0;

					mWiimoteState.ClassicControllerState.ButtonState.Up			= (buff[offset + 5] & 0x01) == 0;
					mWiimoteState.ClassicControllerState.ButtonState.Left		= (buff[offset + 5] & 0x02) == 0;
					mWiimoteState.ClassicControllerState.ButtonState.ZR			= (buff[offset + 5] & 0x04) == 0;
					mWiimoteState.ClassicControllerState.ButtonState.X			= (buff[offset + 5] & 0x08) == 0;
					mWiimoteState.ClassicControllerState.ButtonState.A			= (buff[offset + 5] & 0x10) == 0;
					mWiimoteState.ClassicControllerState.ButtonState.Y			= (buff[offset + 5] & 0x20) == 0;
					mWiimoteState.ClassicControllerState.ButtonState.B			= (buff[offset + 5] & 0x40) == 0;
					mWiimoteState.ClassicControllerState.ButtonState.ZL			= (buff[offset + 5] & 0x80) == 0;

					if(mWiimoteState.ClassicControllerState.CalibrationInfo.MaxXL != 0x00)
						mWiimoteState.ClassicControllerState.JoystickL.X = (float)((float)mWiimoteState.ClassicControllerState.RawJoystickL.X - mWiimoteState.ClassicControllerState.CalibrationInfo.MidXL) / 
						(float)(mWiimoteState.ClassicControllerState.CalibrationInfo.MaxXL - mWiimoteState.ClassicControllerState.CalibrationInfo.MinXL);

					if(mWiimoteState.ClassicControllerState.CalibrationInfo.MaxYL != 0x00)
						mWiimoteState.ClassicControllerState.JoystickL.Y = (float)((float)mWiimoteState.ClassicControllerState.RawJoystickL.Y - mWiimoteState.ClassicControllerState.CalibrationInfo.MidYL) / 
						(float)(mWiimoteState.ClassicControllerState.CalibrationInfo.MaxYL - mWiimoteState.ClassicControllerState.CalibrationInfo.MinYL);

					if(mWiimoteState.ClassicControllerState.CalibrationInfo.MaxXR != 0x00)
						mWiimoteState.ClassicControllerState.JoystickR.X = (float)((float)mWiimoteState.ClassicControllerState.RawJoystickR.X - mWiimoteState.ClassicControllerState.CalibrationInfo.MidXR) / 
						(float)(mWiimoteState.ClassicControllerState.CalibrationInfo.MaxXR - mWiimoteState.ClassicControllerState.CalibrationInfo.MinXR);

					if(mWiimoteState.ClassicControllerState.CalibrationInfo.MaxYR != 0x00)
						mWiimoteState.ClassicControllerState.JoystickR.Y = (float)((float)mWiimoteState.ClassicControllerState.RawJoystickR.Y - mWiimoteState.ClassicControllerState.CalibrationInfo.MidYR) / 
						(float)(mWiimoteState.ClassicControllerState.CalibrationInfo.MaxYR - mWiimoteState.ClassicControllerState.CalibrationInfo.MinYR);

					if(mWiimoteState.ClassicControllerState.CalibrationInfo.MaxTriggerL != 0x00)
						mWiimoteState.ClassicControllerState.TriggerL = (mWiimoteState.ClassicControllerState.RawTriggerL) / 
						(float)(mWiimoteState.ClassicControllerState.CalibrationInfo.MaxTriggerL - mWiimoteState.ClassicControllerState.CalibrationInfo.MinTriggerL);

					if(mWiimoteState.ClassicControllerState.CalibrationInfo.MaxTriggerR != 0x00)
						mWiimoteState.ClassicControllerState.TriggerR = (mWiimoteState.ClassicControllerState.RawTriggerR) / 
						(float)(mWiimoteState.ClassicControllerState.CalibrationInfo.MaxTriggerR - mWiimoteState.ClassicControllerState.CalibrationInfo.MinTriggerR);
					break;

				case ExtensionType.Guitar:
					mWiimoteState.GuitarState.GuitarType = ((buff[offset] & 0x80) == 0) ? GuitarType.GuitarHeroWorldTour : GuitarType.GuitarHero3;

					mWiimoteState.GuitarState.ButtonState.Plus		= (buff[offset + 4] & 0x04) == 0;
					mWiimoteState.GuitarState.ButtonState.Minus		= (buff[offset + 4] & 0x10) == 0;
					mWiimoteState.GuitarState.ButtonState.StrumDown	= (buff[offset + 4] & 0x40) == 0;

					mWiimoteState.GuitarState.ButtonState.StrumUp		= (buff[offset + 5] & 0x01) == 0;
					mWiimoteState.GuitarState.FretButtonState.Yellow	= (buff[offset + 5] & 0x08) == 0;
					mWiimoteState.GuitarState.FretButtonState.Green		= (buff[offset + 5] & 0x10) == 0;
					mWiimoteState.GuitarState.FretButtonState.Blue		= (buff[offset + 5] & 0x20) == 0;
					mWiimoteState.GuitarState.FretButtonState.Red		= (buff[offset + 5] & 0x40) == 0;
					mWiimoteState.GuitarState.FretButtonState.Orange	= (buff[offset + 5] & 0x80) == 0;

					// it appears the joystick values are only 6 bits
					mWiimoteState.GuitarState.RawJoystick.X	= (buff[offset + 0] & 0x3f);
					mWiimoteState.GuitarState.RawJoystick.Y	= (buff[offset + 1] & 0x3f);

					// and the whammy bar is only 5 bits
					mWiimoteState.GuitarState.RawWhammyBar			= (byte)(buff[offset + 3] & 0x1f);

					mWiimoteState.GuitarState.Joystick.X			= (float)(mWiimoteState.GuitarState.RawJoystick.X - 0x1f) / 0x3f;	// not fully accurate, but close
					mWiimoteState.GuitarState.Joystick.Y			= (float)(mWiimoteState.GuitarState.RawJoystick.Y - 0x1f) / 0x3f;	// not fully accurate, but close
					mWiimoteState.GuitarState.WhammyBar				= (float)(mWiimoteState.GuitarState.RawWhammyBar) / 0x0a;	// seems like there are 10 positions?

					mWiimoteState.GuitarState.TouchbarState.Yellow	= false;
					mWiimoteState.GuitarState.TouchbarState.Green	= false;
					mWiimoteState.GuitarState.TouchbarState.Blue	= false;
					mWiimoteState.GuitarState.TouchbarState.Red		= false;
					mWiimoteState.GuitarState.TouchbarState.Orange	= false;

					switch(buff[offset + 2] & 0x1f)
					{
						case 0x04:
							mWiimoteState.GuitarState.TouchbarState.Green = true;
							break;
						case 0x07:
							mWiimoteState.GuitarState.TouchbarState.Green = true;
							mWiimoteState.GuitarState.TouchbarState.Red = true;
							break;
						case 0x0a:
							mWiimoteState.GuitarState.TouchbarState.Red = true;
							break;
						case 0x0c:
						case 0x0d:
							mWiimoteState.GuitarState.TouchbarState.Red = true;
							mWiimoteState.GuitarState.TouchbarState.Yellow = true;
							break;
						case 0x12:
						case 0x13:
							mWiimoteState.GuitarState.TouchbarState.Yellow = true;
							break;
						case 0x14:
						case 0x15:
							mWiimoteState.GuitarState.TouchbarState.Yellow = true;
							mWiimoteState.GuitarState.TouchbarState.Blue = true;
							break;
						case 0x17:
						case 0x18:
							mWiimoteState.GuitarState.TouchbarState.Blue = true;
							break;
						case 0x1a:
							mWiimoteState.GuitarState.TouchbarState.Blue = true;
							mWiimoteState.GuitarState.TouchbarState.Orange = true;
							break;
						case 0x1f:
							mWiimoteState.GuitarState.TouchbarState.Orange = true;
							break;
					}
					break;

				case ExtensionType.Drums:
					// it appears the joystick values are only 6 bits
					mWiimoteState.DrumsState.RawJoystick.X	= (buff[offset + 0] & 0x3f);
					mWiimoteState.DrumsState.RawJoystick.Y	= (buff[offset + 1] & 0x3f);

					mWiimoteState.DrumsState.Plus			= (buff[offset + 4] & 0x04) == 0;
					mWiimoteState.DrumsState.Minus			= (buff[offset + 4] & 0x10) == 0;

					mWiimoteState.DrumsState.Pedal			= (buff[offset + 5] & 0x04) == 0;
					mWiimoteState.DrumsState.Blue			= (buff[offset + 5] & 0x08) == 0;
					mWiimoteState.DrumsState.Green			= (buff[offset + 5] & 0x10) == 0;
					mWiimoteState.DrumsState.Yellow			= (buff[offset + 5] & 0x20) == 0;
					mWiimoteState.DrumsState.Red			= (buff[offset + 5] & 0x40) == 0;
					mWiimoteState.DrumsState.Orange			= (buff[offset + 5] & 0x80) == 0;

					mWiimoteState.DrumsState.Joystick.X		= (float)(mWiimoteState.DrumsState.RawJoystick.X - 0x1f) / 0x3f;	// not fully accurate, but close
					mWiimoteState.DrumsState.Joystick.Y		= (float)(mWiimoteState.DrumsState.RawJoystick.Y - 0x1f) / 0x3f;	// not fully accurate, but close

					if((buff[offset + 2] & 0x40) == 0)
					{
						int pad = (buff[offset + 2] >> 1) & 0x1f;
						int velocity = (buff[offset + 3] >> 5);

						if(velocity != 7)
						{
							switch(pad)
							{
								case 0x1b:
									mWiimoteState.DrumsState.PedalVelocity = velocity;
									break;
								case 0x19:
									mWiimoteState.DrumsState.RedVelocity = velocity;
									break;
								case 0x11:
									mWiimoteState.DrumsState.YellowVelocity = velocity;
									break;
								case 0x0f:
									mWiimoteState.DrumsState.BlueVelocity = velocity;
									break;
								case 0x0e:
									mWiimoteState.DrumsState.OrangeVelocity = velocity;
									break;
								case 0x12:
									mWiimoteState.DrumsState.GreenVelocity = velocity;
									break;
							}
						}
					}
					break;

				case ExtensionType.BalanceBoard:
					mWiimoteState.BalanceBoardState.SensorValuesRaw.TopRight = (short)((short)buff[offset + 0] << 8 | buff[offset + 1]);
					mWiimoteState.BalanceBoardState.SensorValuesRaw.BottomRight = (short)((short)buff[offset + 2] << 8 | buff[offset + 3]);
					mWiimoteState.BalanceBoardState.SensorValuesRaw.TopLeft = (short)((short)buff[offset + 4] << 8 | buff[offset + 5]);
					mWiimoteState.BalanceBoardState.SensorValuesRaw.BottomLeft = (short)((short)buff[offset + 6] << 8 | buff[offset + 7]);

					mWiimoteState.BalanceBoardState.SensorValuesKg.TopLeft = GetBalanceBoardSensorValue(mWiimoteState.BalanceBoardState.SensorValuesRaw.TopLeft, mWiimoteState.BalanceBoardState.CalibrationInfo.Kg0.TopLeft, mWiimoteState.BalanceBoardState.CalibrationInfo.Kg17.TopLeft, mWiimoteState.BalanceBoardState.CalibrationInfo.Kg34.TopLeft);
					mWiimoteState.BalanceBoardState.SensorValuesKg.TopRight = GetBalanceBoardSensorValue(mWiimoteState.BalanceBoardState.SensorValuesRaw.TopRight, mWiimoteState.BalanceBoardState.CalibrationInfo.Kg0.TopRight, mWiimoteState.BalanceBoardState.CalibrationInfo.Kg17.TopRight, mWiimoteState.BalanceBoardState.CalibrationInfo.Kg34.TopRight);
					mWiimoteState.BalanceBoardState.SensorValuesKg.BottomLeft = GetBalanceBoardSensorValue(mWiimoteState.BalanceBoardState.SensorValuesRaw.BottomLeft, mWiimoteState.BalanceBoardState.CalibrationInfo.Kg0.BottomLeft, mWiimoteState.BalanceBoardState.CalibrationInfo.Kg17.BottomLeft, mWiimoteState.BalanceBoardState.CalibrationInfo.Kg34.BottomLeft);
					mWiimoteState.BalanceBoardState.SensorValuesKg.BottomRight = GetBalanceBoardSensorValue(mWiimoteState.BalanceBoardState.SensorValuesRaw.BottomRight, mWiimoteState.BalanceBoardState.CalibrationInfo.Kg0.BottomRight, mWiimoteState.BalanceBoardState.CalibrationInfo.Kg17.BottomRight, mWiimoteState.BalanceBoardState.CalibrationInfo.Kg34.BottomRight);

					mWiimoteState.BalanceBoardState.SensorValuesLb.TopLeft = (mWiimoteState.BalanceBoardState.SensorValuesKg.TopLeft * KG2LB);
					mWiimoteState.BalanceBoardState.SensorValuesLb.TopRight = (mWiimoteState.BalanceBoardState.SensorValuesKg.TopRight * KG2LB);
					mWiimoteState.BalanceBoardState.SensorValuesLb.BottomLeft = (mWiimoteState.BalanceBoardState.SensorValuesKg.BottomLeft * KG2LB);
					mWiimoteState.BalanceBoardState.SensorValuesLb.BottomRight = (mWiimoteState.BalanceBoardState.SensorValuesKg.BottomRight * KG2LB);

					mWiimoteState.BalanceBoardState.WeightKg = (mWiimoteState.BalanceBoardState.SensorValuesKg.TopLeft + mWiimoteState.BalanceBoardState.SensorValuesKg.TopRight + mWiimoteState.BalanceBoardState.SensorValuesKg.BottomLeft + mWiimoteState.BalanceBoardState.SensorValuesKg.BottomRight) / 4.0f;
					mWiimoteState.BalanceBoardState.WeightLb = (mWiimoteState.BalanceBoardState.SensorValuesLb.TopLeft + mWiimoteState.BalanceBoardState.SensorValuesLb.TopRight + mWiimoteState.BalanceBoardState.SensorValuesLb.BottomLeft + mWiimoteState.BalanceBoardState.SensorValuesLb.BottomRight) / 4.0f;

					float Kx = (mWiimoteState.BalanceBoardState.SensorValuesKg.TopLeft + mWiimoteState.BalanceBoardState.SensorValuesKg.BottomLeft) / (mWiimoteState.BalanceBoardState.SensorValuesKg.TopRight + mWiimoteState.BalanceBoardState.SensorValuesKg.BottomRight);
					float Ky = (mWiimoteState.BalanceBoardState.SensorValuesKg.TopLeft + mWiimoteState.BalanceBoardState.SensorValuesKg.TopRight) / (mWiimoteState.BalanceBoardState.SensorValuesKg.BottomLeft + mWiimoteState.BalanceBoardState.SensorValuesKg.BottomRight);

					mWiimoteState.BalanceBoardState.CenterOfGravity.X = ((float)(Kx - 1) / (float)(Kx + 1)) * (float)(-BSL / 2);
					mWiimoteState.BalanceBoardState.CenterOfGravity.Y = ((float)(Ky - 1) / (float)(Ky + 1)) * (float)(-BSW / 2);
					break;

				case ExtensionType.TaikoDrum:
					mWiimoteState.TaikoDrumState.OuterLeft  = (buff[offset + 5] & 0x20) == 0;
					mWiimoteState.TaikoDrumState.InnerLeft  = (buff[offset + 5] & 0x40) == 0;
					mWiimoteState.TaikoDrumState.InnerRight = (buff[offset + 5] & 0x10) == 0;
					mWiimoteState.TaikoDrumState.OuterRight = (buff[offset + 5] & 0x08) == 0;
					break;

				case ExtensionType.MotionPlus:
                case ExtensionType.MotionPlusNunchuk:
                case ExtensionType.MotionPlusClassicController:
                    mWiimoteState.MotionPlusState.ExtensionConnected = ((buff[offset + 4] & 0x01)) == 1;
                    mWiimoteState.MotionPlusState.PassThrough = ((buff[offset + 5] & 0x02) >> 1) == 0;

                    if (mWiimoteState.MotionPlusState.ExtensionConnected == true)
                    {
                        mCounterExtensionDisconnect = 0;

                        if (mWiimoteState.ExtensionType == ExtensionType.MotionPlus)
                        {
                            // count the connect reports
                            mCounterExtensionConnect++;

                            // tolerate a few false statements
                            // sometimes the Wiimote gives the wrong state
                            if (mCounterExtensionConnect == 10)
                            {
                                // generate status message for connected extension plugged into MotionPlus
                                // we have to do it manually - because no status message will notify us
                                Debug.WriteLine("Extension connected to MotionPlus");

                                // GetStatus is not possible - because only the activated MotionPlus will be detected
                                // to identify the extension plugged into MotionPlus 
                                // we have to manually activate it as standalone extension 
                                //mWiimoteState.ExtensionType = ExtensionType.MotionPlusPartiallyInserted;
                                InitializeExtension(ExtensionType.MotionPlusPartiallyInserted);
                            }
                        }
                    }
                    else
                    {
                        mCounterExtensionConnect = 0;

                        if (mWiimoteState.ExtensionType == ExtensionType.MotionPlusNunchuk || mWiimoteState.ExtensionType == ExtensionType.MotionPlusClassicController)
                        {
                            // count the disconnect reports
                            mCounterExtensionDisconnect++;

                            // tolerate a few false statements
                            // sometimes the Wiimote gives the wrong state
                            if (mCounterExtensionDisconnect == 10)
                            {
                                // generate status message for disconnected extension plugged into MotionPlus
                                // we have to do it manually - because no status message will notify us
                                Debug.WriteLine("Extension disconnected from MotionPlus");

                                // GetStatus is not possible - because only the activated MotionPlus will be detected
                                // MotionPlusID is not possible - because after activation no status message will be send
                                //mWiimoteState.ExtensionType = ExtensionType.MotionPlusPassThroughDisable;
                                InitializeExtension(ExtensionType.MotionPlusPassThroughDisable);
                            }
                        }
                    }

                    if (mWiimoteState.MotionPlusState.PassThrough == false)
                    {
                        // Motion Plus data
                        mWiimoteState.MotionPlusState.GyroState.YawFast = ((buff[offset + 3] & 0x02) >> 1) == 0;
                        mWiimoteState.MotionPlusState.GyroState.RollFast = ((buff[offset + 4] & 0x02) >> 1) == 0;
                        mWiimoteState.MotionPlusState.GyroState.PitchFast = ((buff[offset + 3] & 0x01) >> 0) == 0;

                        mWiimoteState.MotionPlusState.GyroState.RawValues.Yaw = (buff[offset + 0] | (buff[offset + 3] & 0xfc) << 6);
                        mWiimoteState.MotionPlusState.GyroState.RawValues.Roll = (buff[offset + 1] | (buff[offset + 4] & 0xfc) << 6);
                        mWiimoteState.MotionPlusState.GyroState.RawValues.Pitch = (buff[offset + 2] | (buff[offset + 5] & 0xfc) << 6);

                        if (mCalibMotionPlus == true)
                        {
                            if (mCalibMotionPlusNumValues > 0)
                            {
                                // save gyration values for further calibration
                                mCalibMotionPlusList.Add(mWiimoteState.MotionPlusState.GyroState);
                                mCalibMotionPlusNumValues--;
                            }
                            else
                            {
                                // calculate calibration data
                                mWiimoteState.MotionPlusState.CalibrationInfo.GyroCalibration.Yaw0 = 0;
                                mWiimoteState.MotionPlusState.CalibrationInfo.GyroCalibration.Roll0 = 0;
                                mWiimoteState.MotionPlusState.CalibrationInfo.GyroCalibration.Pitch0 = 0;
                                foreach (GyroState gyrostate in mCalibMotionPlusList)
                                {
                                    mWiimoteState.MotionPlusState.CalibrationInfo.GyroCalibration.Yaw0 += (uint)(gyrostate.RawValues.Yaw);
                                    mWiimoteState.MotionPlusState.CalibrationInfo.GyroCalibration.Roll0 += (uint)(gyrostate.RawValues.Roll);
                                    mWiimoteState.MotionPlusState.CalibrationInfo.GyroCalibration.Pitch0 += (uint)(gyrostate.RawValues.Pitch);
                                }
                                mWiimoteState.MotionPlusState.CalibrationInfo.GyroCalibration.Yaw0 /= (uint)mCalibMotionPlusList.Count;
                                mWiimoteState.MotionPlusState.CalibrationInfo.GyroCalibration.Roll0 /= (uint)mCalibMotionPlusList.Count;
                                mWiimoteState.MotionPlusState.CalibrationInfo.GyroCalibration.Pitch0 /= (uint)mCalibMotionPlusList.Count;

                                // stop calibration
                                mCalibMotionPlus = false;
                            }
                        }

                        if (mWiimoteState.MotionPlusState.GyroState.YawFast)
                        {
                            mWiimoteState.MotionPlusState.GyroState.Values.Yaw = (float)((((float)mWiimoteState.MotionPlusState.GyroState.RawValues.Yaw) - ((float)mWiimoteState.MotionPlusState.CalibrationInfo.GyroCalibration.Yaw0)) / MOTIONPLUS_HIGHSPEED_SCALING);
                        }
                        else
                        {
                            mWiimoteState.MotionPlusState.GyroState.Values.Yaw = (float)((((float)mWiimoteState.MotionPlusState.GyroState.RawValues.Yaw) - ((float)mWiimoteState.MotionPlusState.CalibrationInfo.GyroCalibration.Yaw0)) / MOTIONPLUS_LOWSPEED_SCALING);
                        }

                        if (mWiimoteState.MotionPlusState.GyroState.RollFast)
                        {
                            mWiimoteState.MotionPlusState.GyroState.Values.Roll = (float)((((float)mWiimoteState.MotionPlusState.GyroState.RawValues.Roll) - ((float)mWiimoteState.MotionPlusState.CalibrationInfo.GyroCalibration.Roll0)) / MOTIONPLUS_HIGHSPEED_SCALING);
                        }
                        else
                        {
                            mWiimoteState.MotionPlusState.GyroState.Values.Roll = (float)((((float)mWiimoteState.MotionPlusState.GyroState.RawValues.Roll) - ((float)mWiimoteState.MotionPlusState.CalibrationInfo.GyroCalibration.Roll0)) / MOTIONPLUS_LOWSPEED_SCALING);
                        }

                        if (mWiimoteState.MotionPlusState.GyroState.PitchFast)
                        {
                            mWiimoteState.MotionPlusState.GyroState.Values.Pitch = (float)((((float)mWiimoteState.MotionPlusState.GyroState.RawValues.Pitch) - ((float)mWiimoteState.MotionPlusState.CalibrationInfo.GyroCalibration.Pitch0)) / MOTIONPLUS_HIGHSPEED_SCALING);
                        }
                        else
                        {
                            mWiimoteState.MotionPlusState.GyroState.Values.Pitch = (float)((((float)mWiimoteState.MotionPlusState.GyroState.RawValues.Pitch) - ((float)mWiimoteState.MotionPlusState.CalibrationInfo.GyroCalibration.Pitch0)) / MOTIONPLUS_LOWSPEED_SCALING);
                        }
                    }
                    else
                    {
                        // Nunchuk pass-through mode 
                        if (mWiimoteState.ExtensionType == ExtensionType.MotionPlusNunchuk)
                        {
                            mWiimoteState.NunchukState.RawJoystick.X = buff[offset];
                            mWiimoteState.NunchukState.RawJoystick.Y = buff[offset + 1];

                            mWiimoteState.NunchukState.AccelState.RawValues.X = (buff[offset + 2] << 2) | (buff[offset + 5] >> 3 & 0x02);
                            mWiimoteState.NunchukState.AccelState.RawValues.Y = (buff[offset + 3] << 2) | (buff[offset + 5] >> 4 & 0x02);
                            mWiimoteState.NunchukState.AccelState.RawValues.Z = ((buff[offset + 4] & 0xFE) << 2) | (buff[offset + 5] >> 5 & 0x06);

                            mWiimoteState.NunchukState.C = (buff[offset + 5] & 0x08) == 0;
                            mWiimoteState.NunchukState.Z = (buff[offset + 5] & 0x04) == 0;

                            mWiimoteState.NunchukState.AccelState.Values.X = (float)((float)mWiimoteState.NunchukState.AccelState.RawValues.X - mWiimoteState.NunchukState.CalibrationInfo.AccelCalibration.X0) /
                                                            ((float)mWiimoteState.NunchukState.CalibrationInfo.AccelCalibration.XG - mWiimoteState.NunchukState.CalibrationInfo.AccelCalibration.X0);
                            mWiimoteState.NunchukState.AccelState.Values.Y = (float)((float)mWiimoteState.NunchukState.AccelState.RawValues.Y - mWiimoteState.NunchukState.CalibrationInfo.AccelCalibration.Y0) /
                                                            ((float)mWiimoteState.NunchukState.CalibrationInfo.AccelCalibration.YG - mWiimoteState.NunchukState.CalibrationInfo.AccelCalibration.Y0);
                            mWiimoteState.NunchukState.AccelState.Values.Z = (float)((float)mWiimoteState.NunchukState.AccelState.RawValues.Z - mWiimoteState.NunchukState.CalibrationInfo.AccelCalibration.Z0) /
                                                            ((float)mWiimoteState.NunchukState.CalibrationInfo.AccelCalibration.ZG - mWiimoteState.NunchukState.CalibrationInfo.AccelCalibration.Z0);

                            if (mWiimoteState.NunchukState.CalibrationInfo.MaxX != 0x00)
                                mWiimoteState.NunchukState.Joystick.X = (float)((float)mWiimoteState.NunchukState.RawJoystick.X - mWiimoteState.NunchukState.CalibrationInfo.MidX) /
                                                        ((float)mWiimoteState.NunchukState.CalibrationInfo.MaxX - mWiimoteState.NunchukState.CalibrationInfo.MinX);

                            if (mWiimoteState.NunchukState.CalibrationInfo.MaxY != 0x00)
                                mWiimoteState.NunchukState.Joystick.Y = (float)((float)mWiimoteState.NunchukState.RawJoystick.Y - mWiimoteState.NunchukState.CalibrationInfo.MidY) /
                                                        ((float)mWiimoteState.NunchukState.CalibrationInfo.MaxY - mWiimoteState.NunchukState.CalibrationInfo.MinY);
                        }

                        // ClassicController pass-through mode 
                        else if (mWiimoteState.ExtensionType == ExtensionType.MotionPlusClassicController)
                        {
                            mWiimoteState.ClassicControllerState.RawJoystickL.X = (byte)(buff[offset] & 0x3e);      //
                            mWiimoteState.ClassicControllerState.RawJoystickL.Y = (byte)(buff[offset + 1] & 0x3e);  //
                            mWiimoteState.ClassicControllerState.RawJoystickR.X = (byte)((buff[offset + 2] >> 7) | (buff[offset + 1] & 0xc0) >> 5 | (buff[offset] & 0xc0) >> 3);
                            mWiimoteState.ClassicControllerState.RawJoystickR.Y = (byte)(buff[offset + 2] & 0x1f);

                            mWiimoteState.ClassicControllerState.RawTriggerL = (byte)(((buff[offset + 2] & 0x60) >> 2) | (buff[offset + 3] >> 5));
                            mWiimoteState.ClassicControllerState.RawTriggerR = (byte)(buff[offset + 3] & 0x1f);

                            mWiimoteState.ClassicControllerState.ButtonState.TriggerR = (buff[offset + 4] & 0x02) == 0;
                            mWiimoteState.ClassicControllerState.ButtonState.Plus = (buff[offset + 4] & 0x04) == 0;
                            mWiimoteState.ClassicControllerState.ButtonState.Home = (buff[offset + 4] & 0x08) == 0;
                            mWiimoteState.ClassicControllerState.ButtonState.Minus = (buff[offset + 4] & 0x10) == 0;
                            mWiimoteState.ClassicControllerState.ButtonState.TriggerL = (buff[offset + 4] & 0x20) == 0;
                            mWiimoteState.ClassicControllerState.ButtonState.Down = (buff[offset + 4] & 0x40) == 0;
                            mWiimoteState.ClassicControllerState.ButtonState.Right = (buff[offset + 4] & 0x80) == 0;

                            mWiimoteState.ClassicControllerState.ButtonState.Up = (buff[offset] & 0x01) == 0;   //
                            mWiimoteState.ClassicControllerState.ButtonState.Left = (buff[offset + 1] & 0x01) == 0; //
                            mWiimoteState.ClassicControllerState.ButtonState.ZR = (buff[offset + 5] & 0x04) == 0;
                            mWiimoteState.ClassicControllerState.ButtonState.X = (buff[offset + 5] & 0x08) == 0;
                            mWiimoteState.ClassicControllerState.ButtonState.A = (buff[offset + 5] & 0x10) == 0;
                            mWiimoteState.ClassicControllerState.ButtonState.Y = (buff[offset + 5] & 0x20) == 0;
                            mWiimoteState.ClassicControllerState.ButtonState.B = (buff[offset + 5] & 0x40) == 0;
                            mWiimoteState.ClassicControllerState.ButtonState.ZL = (buff[offset + 5] & 0x80) == 0;

                            if (mWiimoteState.ClassicControllerState.CalibrationInfo.MaxXL != 0x00)
                                mWiimoteState.ClassicControllerState.JoystickL.X = (float)((float)mWiimoteState.ClassicControllerState.RawJoystickL.X - mWiimoteState.ClassicControllerState.CalibrationInfo.MidXL) /
                                (float)(mWiimoteState.ClassicControllerState.CalibrationInfo.MaxXL - mWiimoteState.ClassicControllerState.CalibrationInfo.MinXL);

                            if (mWiimoteState.ClassicControllerState.CalibrationInfo.MaxYL != 0x00)
                                mWiimoteState.ClassicControllerState.JoystickL.Y = (float)((float)mWiimoteState.ClassicControllerState.RawJoystickL.Y - mWiimoteState.ClassicControllerState.CalibrationInfo.MidYL) /
                                (float)(mWiimoteState.ClassicControllerState.CalibrationInfo.MaxYL - mWiimoteState.ClassicControllerState.CalibrationInfo.MinYL);

                            if (mWiimoteState.ClassicControllerState.CalibrationInfo.MaxXR != 0x00)
                                mWiimoteState.ClassicControllerState.JoystickR.X = (float)((float)mWiimoteState.ClassicControllerState.RawJoystickR.X - mWiimoteState.ClassicControllerState.CalibrationInfo.MidXR) /
                                (float)(mWiimoteState.ClassicControllerState.CalibrationInfo.MaxXR - mWiimoteState.ClassicControllerState.CalibrationInfo.MinXR);

                            if (mWiimoteState.ClassicControllerState.CalibrationInfo.MaxYR != 0x00)
                                mWiimoteState.ClassicControllerState.JoystickR.Y = (float)((float)mWiimoteState.ClassicControllerState.RawJoystickR.Y - mWiimoteState.ClassicControllerState.CalibrationInfo.MidYR) /
                                (float)(mWiimoteState.ClassicControllerState.CalibrationInfo.MaxYR - mWiimoteState.ClassicControllerState.CalibrationInfo.MinYR);

                            if (mWiimoteState.ClassicControllerState.CalibrationInfo.MaxTriggerL != 0x00)
                                mWiimoteState.ClassicControllerState.TriggerL = (mWiimoteState.ClassicControllerState.RawTriggerL) /
                                (float)(mWiimoteState.ClassicControllerState.CalibrationInfo.MaxTriggerL - mWiimoteState.ClassicControllerState.CalibrationInfo.MinTriggerL);

                            if (mWiimoteState.ClassicControllerState.CalibrationInfo.MaxTriggerR != 0x00)
                                mWiimoteState.ClassicControllerState.TriggerR = (mWiimoteState.ClassicControllerState.RawTriggerR) /
                                (float)(mWiimoteState.ClassicControllerState.CalibrationInfo.MaxTriggerR - mWiimoteState.ClassicControllerState.CalibrationInfo.MinTriggerR);
                        }
                    }
                    
                    break;
			}
		}

		private float GetBalanceBoardSensorValue(short sensor, short min, short mid, short max)
		{
			if(max == mid || mid == min)
				return 0;

			if(sensor < mid)
				return 68.0f * ((float)(sensor - min) / (mid - min));
			else
				return 68.0f * ((float)(sensor - mid) / (max - mid)) + 68.0f;
		}


		/// <summary>
		/// Parse data returned from a read report
		/// </summary>
		/// <param name="buff">Data buffer</param>
		private void ParseReadData(byte[] buff)
		{
			if((buff[3] & 0x08) != 0)
				throw new WiimoteException("Error reading data from Wiimote: Bytes do not exist.");

			if((buff[3] & 0x07) != 0)
			{
				Debug.WriteLine("*** read from write-only");
				LastReadStatus = LastReadStatus.ReadFromWriteOnlyMemory;
				mReadDone.Set();
				return;
			}

			// get our size and offset from the report
			int size = (buff[3] >> 4) + 1;
			int offset = (buff[4] << 8 | buff[5]);

			// add it to the buffer
            Array.Copy(buff, 6, mReadBuff, offset - mAddress, size); 
            
			// if we've read it all, set the event
			if(mAddress + mSize == offset + size)
				mReadDone.Set();

			LastReadStatus = LastReadStatus.Success;
		}

        /// <summary>
		/// Parse output report acknowledge
		/// </summary>
		/// <param name="buff">Data buffer</param>
		private void ParseOutputReportAck(byte[] buff)
		{
//			Debug.WriteLine("ack: " + buff[0] + " " +  buff[1] + " " +buff[2] + " " +buff[3] + " " +buff[4]);
            mWriteDone.Set();
        }

		/// <summary>
		/// Returns whether rumble is currently enabled.
		/// </summary>
		/// <returns>Byte indicating true (0x01) or false (0x00)</returns>
		private byte GetRumbleBit()
		{
			return (byte)(mWiimoteState.Rumble ? 0x01 : 0x00);
		}

		/// <summary>
		/// Read calibration information stored on Wiimote
		/// </summary>
		private void ReadWiimoteCalibration()
		{
			// this appears to change the report type to 0x31
            byte[] buff = ReadData(REGISTER_WIIMOTE_INIT, 7);

			mWiimoteState.AccelCalibrationInfo.X0 = (uint)((buff[0] << 2) | (buff[3]      & 0x03));
            mWiimoteState.AccelCalibrationInfo.Y0 = (uint)((buff[1] << 2) | (buff[3] >> 2 & 0x03));
            mWiimoteState.AccelCalibrationInfo.Z0 = (uint)((buff[2] << 2) | (buff[3] >> 4 & 0x03));

            mWiimoteState.AccelCalibrationInfo.XG = (uint)((buff[4] << 2) | (buff[3]      & 0x03));
            mWiimoteState.AccelCalibrationInfo.YG = (uint)((buff[5] << 2) | (buff[3] >> 2 & 0x03));
            mWiimoteState.AccelCalibrationInfo.ZG = (uint)((buff[6] << 2) | (buff[3] >> 4 & 0x03));
		}

		/// <summary>
		/// Set Wiimote reporting mode (if using an IR report type, IR sensitivity is set to WiiLevel3)
		/// </summary>
		/// <param name="type">Report type</param>
		/// <param name="continuous">Continuous data</param>
		public void SetReportType(InputReport type, bool continuous)
		{
			Debug.WriteLine("SetReportType: " + type);
			SetReportType(type, IRSensitivity.Maximum, continuous);
		}

		/// <summary>
		/// Set Wiimote reporting mode
		/// </summary>
		/// <param name="type">Report type</param>
		/// <param name="irSensitivity">IR sensitivity</param>
		/// <param name="continuous">Continuous data</param>
		public void SetReportType(InputReport type, IRSensitivity irSensitivity, bool continuous)
        {
            // only 1 report type allowed for the BB
            if (mWiimoteState.ExtensionType == ExtensionType.BalanceBoard)
                type = InputReport.ButtonsExtension;

            mWiimoteState.InputReport = type;

            switch (type)
            {
                case InputReport.IRAccel:
                    EnableIR(IRMode.Extended, irSensitivity);
                    break;
                case InputReport.IRExtensionAccel:
                    EnableIR(IRMode.Basic, irSensitivity);
                    break;
                default:
                    DisableIR();
                    break;
            }

            byte[] buff = CreateReport();
            buff[0] = (byte)OutputReport.DataReportType;
            buff[1] = (byte)((continuous ? 0x04 : 0x00) | (byte)(mWiimoteState.Rumble ? 0x01 : 0x00));
            buff[2] = (byte)type;

            WriteReport(buff);   
		}

		/// <summary>
		/// Set the LEDs on the Wiimote
		/// </summary>
		/// <param name="led1">LED 1</param>
		/// <param name="led2">LED 2</param>
		/// <param name="led3">LED 3</param>
		/// <param name="led4">LED 4</param>
		public void SetLEDs(bool led1, bool led2, bool led3, bool led4)
		{
			mWiimoteState.LEDState.LED1 = led1;
			mWiimoteState.LEDState.LED2 = led2;
			mWiimoteState.LEDState.LED3 = led3;
			mWiimoteState.LEDState.LED4 = led4;

			byte[] buff = CreateReport();

			buff[0] = (byte)OutputReport.LEDs;
			buff[1] =	(byte)(
						(led1 ? 0x10 : 0x00) |
						(led2 ? 0x20 : 0x00) |
						(led3 ? 0x40 : 0x00) |
						(led4 ? 0x80 : 0x00) |
						GetRumbleBit());

			WriteReport(buff);
		}

		/// <summary>
		/// Set the LEDs on the Wiimote
		/// </summary>
		/// <param name="leds">The value to be lit up in base2 on the Wiimote</param>
		public void SetLEDs(int leds)
		{
			mWiimoteState.LEDState.LED1 = (leds & 0x01) > 0;
			mWiimoteState.LEDState.LED2 = (leds & 0x02) > 0;
			mWiimoteState.LEDState.LED3 = (leds & 0x04) > 0;
			mWiimoteState.LEDState.LED4 = (leds & 0x08) > 0;

			byte[] buff = CreateReport();

			buff[0] = (byte)OutputReport.LEDs;
			buff[1] =	(byte)(
						((leds & 0x01) > 0 ? 0x10 : 0x00) |
						((leds & 0x02) > 0 ? 0x20 : 0x00) |
						((leds & 0x04) > 0 ? 0x40 : 0x00) |
						((leds & 0x08) > 0 ? 0x80 : 0x00) |
						GetRumbleBit());

			WriteReport(buff);
		}

		/// <summary>
		/// Toggle rumble
		/// </summary>
		/// <param name="on">On or off</param>
		public void SetRumble(bool on)
		{
			mWiimoteState.Rumble = on;

			// the LED report also handles rumble
			SetLEDs(mWiimoteState.LEDState.LED1, 
					mWiimoteState.LEDState.LED2,
					mWiimoteState.LEDState.LED3,
					mWiimoteState.LEDState.LED4);
		}

		/// <summary>
		/// Retrieve the current status of the Wiimote and extensions. Replaces GetBatteryLevel() since it was poorly named.
		/// </summary>
		public void GetStatus()
		{
            mGetStatusMutex.WaitOne();

            Debug.WriteLine("GetStatus");

			byte[] buff = CreateReport();

			buff[0] = (byte)OutputReport.Status;
			buff[1] = GetRumbleBit();

            mStatusRequest.Set();
			WriteReport(buff);

            // wait for the status report finished signal
            if (!mStatusDone.WaitOne(WIIMOTE_TIMEOUT, false))
            {
                mGetStatusMutex.ReleaseMutex();
                throw new WiimoteException("Timed out waiting for status report");
            }

            mGetStatusMutex.ReleaseMutex();
		}

		/// <summary>
		/// Turn on the IR sensor
		/// </summary>
		/// <param name="mode">The data report mode</param>
		/// <param name="irSensitivity">IR sensitivity</param>
		private void EnableIR(IRMode mode, IRSensitivity irSensitivity)
		{
			mWiimoteState.IRState.Mode = mode;

			byte[] buff = CreateReport();
			buff[0] = (byte)OutputReport.IR;
			buff[1] = (byte)(0x04 | GetRumbleBit());
			WriteReport(buff);

			Array.Clear(buff, 0, buff.Length);
			buff[0] = (byte)OutputReport.IR2;
			buff[1] = (byte)(0x04 | GetRumbleBit());
			WriteReport(buff);

			WriteData(REGISTER_IR, 0x08);
			switch(irSensitivity)
			{
				case IRSensitivity.WiiLevel1:
					WriteData(REGISTER_IR_SENSITIVITY_1, 9, new byte[] {0x02, 0x00, 0x00, 0x71, 0x01, 0x00, 0x64, 0x00, 0xfe});
					WriteData(REGISTER_IR_SENSITIVITY_2, 2, new byte[] {0xfd, 0x05});
					break;
				case IRSensitivity.WiiLevel2:
					WriteData(REGISTER_IR_SENSITIVITY_1, 9, new byte[] {0x02, 0x00, 0x00, 0x71, 0x01, 0x00, 0x96, 0x00, 0xb4});
					WriteData(REGISTER_IR_SENSITIVITY_2, 2, new byte[] {0xb3, 0x04});
					break;
				case IRSensitivity.WiiLevel3:
					WriteData(REGISTER_IR_SENSITIVITY_1, 9, new byte[] {0x02, 0x00, 0x00, 0x71, 0x01, 0x00, 0xaa, 0x00, 0x64});
					WriteData(REGISTER_IR_SENSITIVITY_2, 2, new byte[] {0x63, 0x03});
					break;
				case IRSensitivity.WiiLevel4:
					WriteData(REGISTER_IR_SENSITIVITY_1, 9, new byte[] {0x02, 0x00, 0x00, 0x71, 0x01, 0x00, 0xc8, 0x00, 0x36});
					WriteData(REGISTER_IR_SENSITIVITY_2, 2, new byte[] {0x35, 0x03});
					break;
				case IRSensitivity.WiiLevel5:
					WriteData(REGISTER_IR_SENSITIVITY_1, 9, new byte[] {0x07, 0x00, 0x00, 0x71, 0x01, 0x00, 0x72, 0x00, 0x20});
					WriteData(REGISTER_IR_SENSITIVITY_2, 2, new byte[] {0x1, 0x03});
					break;
				case IRSensitivity.Maximum:
					WriteData(REGISTER_IR_SENSITIVITY_1, 9, new byte[] {0x02, 0x00, 0x00, 0x71, 0x01, 0x00, 0x90, 0x00, 0x41});
					WriteData(REGISTER_IR_SENSITIVITY_2, 2, new byte[] {0x40, 0x00});
					break;
				default:
					throw new ArgumentOutOfRangeException("irSensitivity");
			}
			WriteData(REGISTER_IR_MODE, (byte)mode);
			WriteData(REGISTER_IR, 0x08);
		}

		/// <summary>
		/// Disable the IR sensor
		/// </summary>
		private void DisableIR()
		{
			mWiimoteState.IRState.Mode = IRMode.Off;

			byte[] buff = CreateReport();
			buff[0] = (byte)OutputReport.IR;
			buff[1] = GetRumbleBit();
			WriteReport(buff);

			Array.Clear(buff, 0, buff.Length);
			buff[0] = (byte)OutputReport.IR2;
			buff[1] = GetRumbleBit();
			WriteReport(buff);
		}

		/// <summary>
		/// Initialize the report data buffer
		/// </summary>
		private byte[] CreateReport()
		{
			return new byte[REPORT_LENGTH];
		}

		/// <summary>
		/// Write a report to the Wiimote
		/// </summary>
		private void WriteReport(byte[] buff)
		{
            // make sure we call only one read at the same time
            mWriteMutex.WaitOne();
            
			Debug.WriteLine("WriteReport: " + Enum.Parse(typeof(OutputReport), buff[0].ToString()));

			if(mAltWriteMethod)
				HIDImports.HidD_SetOutputReport(this.mHandle.DangerousGetHandle(), buff, (uint)buff.Length);
			else if(mStream != null)
				mStream.Write(buff, 0, REPORT_LENGTH);

			if(buff[0] == (byte)OutputReport.WriteMemory)
			{
                if (!mWriteDone.WaitOne(WIIMOTE_TIMEOUT, false))
                {
                    //TEST
                    mWriteMutex.ReleaseMutex();
                    Debug.WriteLine("Wait failed");
                    throw new WiimoteException("Error writing data to Wiimote...is it connected?");
                }
			}
            mWriteMutex.ReleaseMutex();
		}

		/// <summary>
		/// Read data or register from Wiimote
		/// </summary>
		/// <param name="address">Address to read</param>
		/// <param name="size">Length to read</param>
		/// <returns>Data buffer</returns>
		public byte[] ReadData(int address, short size)
		{
            // we use global variables 
            // make sure we call only one read at the same time
            mReadMutex.WaitOne();

            //Debug.WriteLine("ReadData: address: " + string.Format("{0:x}", address) + " - size: " + string.Format("{0:x}", size));
            
			byte[] buff = CreateReport();

			mReadBuff = new byte[size];
			mAddress = address & 0xffff;
			mSize = size;

			buff[0] = (byte)OutputReport.ReadMemory;
			buff[1] = (byte)(((address & 0xff000000) >> 24) | GetRumbleBit());
			buff[2] = (byte)((address & 0x00ff0000)  >> 16);
			buff[3] = (byte)((address & 0x0000ff00)  >>  8);
			buff[4] = (byte)(address & 0x000000ff);

			buff[5] = (byte)((size & 0xff00) >> 8);
			buff[6] = (byte)(size & 0xff);

			WriteReport(buff);

            // wait for the read data finished signal
            if (!mReadDone.WaitOne(WIIMOTE_TIMEOUT, false))
            {
                mReadMutex.ReleaseMutex();
                Debug.WriteLine("Error reading data from Wiimote...is it connected?");
                throw new WiimoteException("Error reading data from Wiimote...is it connected?");
            }

            mReadMutex.ReleaseMutex();

			return mReadBuff;
		}

		/// <summary>
		/// Write a single byte to the Wiimote
		/// </summary>
		/// <param name="address">Address to write</param>
		/// <param name="data">Byte to write</param>
		public void WriteData(int address, byte data)
		{
			WriteData(address, 1, new byte[] { data });
		}

		/// <summary>
		/// Write a byte array to a specified address
		/// </summary>
		/// <param name="address">Address to write</param>
		/// <param name="size">Length of buffer</param>
		/// <param name="data">Data buffer</param>
		public void WriteData(int address, byte size, byte[] data)
		{
            //Debug.WriteLine("WriteData: address: " + string.Format("{0:x}", address) + " - size: " + string.Format("{0:x}", size));

			byte[] buff = CreateReport();

			buff[0] = (byte)OutputReport.WriteMemory;
			buff[1] = (byte)(((address & 0xff000000) >> 24) | GetRumbleBit());
			buff[2] = (byte)((address & 0x00ff0000)  >> 16);
			buff[3] = (byte)((address & 0x0000ff00)  >>  8);
			buff[4] = (byte)(address & 0x000000ff);
			buff[5] = size;
			Array.Copy(data, 0, buff, 6, size);

			WriteReport(buff);
		}

		/// <summary>
		/// Current Wiimote state
		/// </summary>
		public WiimoteState WiimoteState
		{
			get { return mWiimoteState; }
		}

		///<summary>
		/// Unique identifier for this Wiimote (not persisted across application instances)
		///</summary>
		public Guid ID
		{
			get { return mID; }
		}

		/// <summary>
		/// HID device path for this Wiimote (valid until Wiimote is disconnected)
		/// </summary>
		public string HIDDevicePath
		{
			get { return mDevicePath; }
		}

		/// <summary>
		/// Status of last ReadMemory operation
		/// </summary>
		public LastReadStatus LastReadStatus { get; private set; }

		#region IDisposable Members

		/// <summary>
		/// Dispose Wiimote
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Dispose wiimote
		/// </summary>
		/// <param name="disposing">Disposing?</param>
		protected virtual void Dispose(bool disposing)
		{
			// close up our handles
			if(disposing)
				Disconnect();
		}
		#endregion


    }

	/// <summary>
	/// Thrown when no Wiimotes are found in the HID device list
	/// </summary>
	[Serializable]
	public class WiimoteNotFoundException : ApplicationException
	{
		/// <summary>
		/// Default constructor
		/// </summary>
		public WiimoteNotFoundException()
		{
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Error message</param>
		public WiimoteNotFoundException(string message) : base(message)
		{
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Error message</param>
		/// <param name="innerException">Inner exception</param>
		public WiimoteNotFoundException(string message, Exception innerException) : base(message, innerException)
		{
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="info">Serialization info</param>
		/// <param name="context">Streaming context</param>
		protected WiimoteNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}

	/// <summary>
	/// Represents errors that occur during the execution of the Wiimote library
	/// </summary>
	[Serializable]
	public class WiimoteException : ApplicationException
	{
		/// <summary>
		/// Default constructor
		/// </summary>
		public WiimoteException()
		{
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Error message</param>
		public WiimoteException(string message) : base(message)
		{
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Error message</param>
		/// <param name="innerException">Inner exception</param>
		public WiimoteException(string message, Exception innerException) : base(message, innerException)
		{
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="info">Serialization info</param>
		/// <param name="context">Streaming context</param>
		protected WiimoteException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}
