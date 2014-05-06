using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

namespace ZeroconfService
{
	public class UnixSocket
	{
		private Int32 mSocket;

		// Delegate to allow asynchronous calling of the poll method
		private delegate bool AsyncPollCaller(int microSeconds, SelectMode mode);
		private AsyncPollCaller caller;

		public UnixSocket(Int32 socket)
		{
			mSocket = socket;

			caller = new AsyncPollCaller(Poll);
		}

		public virtual IAsyncResult BeginPoll(int microSeconds, SelectMode mode, AsyncCallback callback, Object state)
		{
			IAsyncResult result = caller.BeginInvoke(microSeconds, mode, callback, state);
			return result;
		}

		public virtual bool EndPoll(IAsyncResult asyncResult)
		{
			return caller.EndInvoke(asyncResult);
		}

		protected bool Poll(int microSeconds, SelectMode mode)
		{
			fd_set readFDs = null;
			fd_set writeFDs = null;

			if (mode == SelectMode.SelectRead)
			{
				readFDs = new fd_set();
				readFDs.FD_ZERO();
				readFDs.FD_SET(mSocket);
			}
			if (mode == SelectMode.SelectWrite)
			{
				writeFDs = new fd_set();
				writeFDs.FD_ZERO();
				writeFDs.FD_SET(mSocket);
			}

			Int32 ret = select(0, readFDs, null, null, null);

			//Console.WriteLine("select returned: {0}", ret);

			if (readFDs.FD_ISSET(mSocket))
			{
				return true;
			}
			return false;
		}

		/* unmanaged stuff */
		[StructLayout(LayoutKind.Sequential)]
		private unsafe class fd_set
		{
			public UInt32 fd_count;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
			public Int32[] fd_array;

			public fd_set()
			{
				fd_array = new Int32[64];
			}

			public void FD_ZERO()
			{
				fd_count = 0;
			}
			public void FD_SET(Int32 fd)
			{
				int i;
				for (i = 0; i < fd_count; i++)
				{
					if (fd_array[i] == fd) break;
				}
				if (i == fd_count)
				{
					fd_array[i] = fd;
					fd_count++;
				}
			}
			public void FD_CLR(Int32 fd)
			{
				int i;
				for (i = 0; i < fd_count; i++)
				{
					if (fd_array[i] == fd)
					{
						while (i < (fd_count - 1))
						{
							fd_array[i] = fd_array[i + 1];
							i++;
						}
						fd_count--;
						break;
					}
				}
			}
			public bool FD_ISSET(Int32 fd)
			{
				return (__WSAFDIsSet(fd, this) != 0);
			}
		}

		private unsafe class timeval
		{
		}

		[DllImport("Ws2_32.dll")]
		private static extern Int32 __WSAFDIsSet(Int32 fd, fd_set set);

		[DllImport("Ws2_32.dll")]
		private static extern Int32 select(Int32 nfds, fd_set readFDs, fd_set writeFDs, fd_set exceptFDs, timeval timeout);

		[DllImport("Ws2_32.dll")]
		private static extern Int32 WSAGetLastError();
	}

	class WatchSocket : UnixSocket
	{
		private IntPtr sdRef;
		private bool inPoll;
		private bool stopping;

		public WatchSocket(Int32 socket, IntPtr sdRef)
			: base(socket)
		{
			this.sdRef = sdRef;
			this.inPoll = false;
			this.stopping = false;
		}

		public IntPtr SDRef
		{
			get { return sdRef; }
		}

		public bool InPoll
		{
			get { return inPoll; }
		}

		public override IAsyncResult BeginPoll(int microSeconds, SelectMode mode, AsyncCallback callback, Object state)
		{
			if(inPoll)
				throw new ApplicationException("Attempting to begin a new poll while already polling.");

			if(stopping)
                throw new ApplicationException("Attempting to begin a new poll after stopping.");

			inPoll = true;
			return base.BeginPoll(microSeconds, mode, callback, state);
		}

		public override bool EndPoll(IAsyncResult asyncResult)
		{
			inPoll = false;
			return base.EndPoll(asyncResult);
		}

		public bool Stopping
		{
			get { return stopping; }
			set { stopping = value; }
		}
	}
}
