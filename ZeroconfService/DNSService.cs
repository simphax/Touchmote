using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading;
using System.Net.Sockets;
using System.Collections;
using System.Diagnostics;

namespace ZeroconfService
{
    /// <summary>
	/// The base class used by the <see cref="NetServiceBrowser">NetServiceBrowser</see>
	/// and <see cref="NetService">NetService</see> classes. This class primarily
	/// abstracts the asynchronous functionality of its derived classes.
	/// </summary>
	/// <remarks>
	/// It should not be necessary to derive from this class directly.
	/// </remarks>
	public abstract class DNSService
	{
		private const String DNSServiceProperty_DaemonVersion = "DaemonVersion";
		
		// Provides a mapping from sdRef's to their associated WatchSocket's
		private Hashtable sdRefToSocketMapping = Hashtable.Synchronized(new Hashtable());

        /// <summary>
        /// 
        /// </summary>
        public static Version DaemonVersion
		{
            get
            {
                int majorVersion = 0;
                int minorVersion = 0;
                int buildVersion = 0;
                IntPtr result = IntPtr.Zero;
                try
                {
                    UInt32 size = (UInt32)Marshal.SizeOf(typeof(UInt32));
                    result = Marshal.AllocCoTaskMem((Int32)size);
                    try
                    {
                        DNSServiceErrorType error = mDNSImports.DNSServiceGetProperty(
                            DNSServiceProperty_DaemonVersion, result, ref size);
                        if (error != DNSServiceErrorType.NoError)
                        {
                            throw new DNSServiceException("DNSServiceGetProperty", error);
                        }
                    }
                    catch (DllNotFoundException e)
                    {
                        throw new DNSServiceException("Unable to connect to system daemon service", e);
                    }
                    int version = Marshal.ReadInt32(result);
                    // Apple documentation states that the version number value is as follows.
                    // Major part of the build number * 10000 + minor part of the build number * 100 
                    // 
                    // If this is true then every version must be a multiple of 100. Just in case this doesn't hold
                    // up we will capture the remainder into the build version value.
                    majorVersion = version / 10000;
                    minorVersion = (version % 10000) / 100;
                    buildVersion = version % 100;
                }
                finally
                {
                    if (result != IntPtr.Zero) Marshal.FreeCoTaskMem(result);
                }
                return new Version(majorVersion, minorVersion, buildVersion);
            }
		}

		private void PollInvokeable(IntPtr sdRef)
		{
			try
			{
				mDNSImports.DNSServiceProcessResult(sdRef);
			}
			catch (Exception e)
			{
				Debug.WriteLine(String.Format("Got an exception on DNSServiceProcessResult (Unamanaged, so via user callback?)\n{0}{1}", e, e.StackTrace));
			}
		}
		private delegate void PollInvokeableDelegate(IntPtr sdRef);

		private bool mAllowApplicationForms = true;
		/// <summary>
		/// Allows the application to attempt to post async replies over the
		/// application "main loop" by using the message queue of the first available
		/// open form (window). This is retrieved through
		/// <see cref="System.Windows.Forms.Application.OpenForms">Application.OpenForms</see>.
		/// </summary>
		public bool AllowApplicationForms
		{
			get { return mAllowApplicationForms; }
			set { mAllowApplicationForms = value; }
		}

		System.ComponentModel.ISynchronizeInvoke mInvokeableObject = null;
		/// <summary>
		/// Set the <see cref="System.ComponentModel.ISynchronizeInvoke">ISynchronizeInvoke</see>
		/// object to use as the invoke object. When returning results from asynchronous calls,
		/// the Invoke method on this object will be called to pass the results back
		/// in a thread safe manner.
		/// </summary>
		/// <remarks>
		/// This is the recommended way of using the DNSService class. It is recommended
		/// that you pass your main <see cref="System.Windows.Forms.Form">form</see> (window) in.
		/// </remarks>
		public System.ComponentModel.ISynchronizeInvoke InvokeableObject
		{
			get { return mInvokeableObject; }
			set { mInvokeableObject = value; }
		}

		private bool mAllowMultithreadedCallbacks = false;
		/// <summary>
		/// If this is set to true, <see cref="AllowApplicationForms">AllowApplicationForms</see>
		/// is set to false and <see cref="InvokeableObject">InvokeableObject</see> is set
		/// to null. Any time an asynchronous method needs to invoke a method in the
		/// main loop, it will instead run the method in its own thread.
		/// </summary>
		/// <remarks>
		/// <para>The thread safety of this property depends on the thread safety of
		/// the underlying dnssd.dll functions. Although it is not recommended, there
		/// are no known problems with this library using this method.
		/// </para>
		/// <para>
		/// If your application uses Windows.Forms or any other non-thread safe
		/// library, you will have to do your own invoking.
		/// </para>
		/// </remarks>
		public bool AllowMultithreadedCallbacks
		{
			get { return mAllowMultithreadedCallbacks; }
			set
			{
				mAllowMultithreadedCallbacks = value;
				if (mAllowMultithreadedCallbacks)
				{
					mAllowApplicationForms = false;
					mInvokeableObject = null;
				}
			}
		}

		internal void InheritInvokeOptions(DNSService fromService)
		{
			// We set the MultiThreadedCallback property first,
			// as it has the potential to affect the other properties.
			AllowMultithreadedCallbacks = fromService.AllowMultithreadedCallbacks;

			AllowApplicationForms = fromService.AllowApplicationForms;
			InvokeableObject = fromService.InvokeableObject;
		}

		private System.ComponentModel.ISynchronizeInvoke GetInvokeObject()
		{
			if (mInvokeableObject != null) return mInvokeableObject;

			if (mAllowApplicationForms)
			{
				// Need to post it to self over control thread
				FormCollection forms = System.Windows.Forms.Application.OpenForms;

				if (forms != null && forms.Count > 0)
				{
					Control control = forms[0];
					return control;
				}
			}
			return null;
		}

		/// <summary>
		/// Calls a method using the objects invokable object.
		/// </summary>
		/// <param name="method">The method to call.</param>
		/// <param name="args">The arguments to call the object with.</param>
		/// <returns>The result returned from method, or null if the method
		/// could not be invoked.</returns>
		protected object Invoke(Delegate method, params object[] args)
		{
			System.ComponentModel.ISynchronizeInvoke invokeable = GetInvokeObject();

			try
			{
				if (invokeable != null)
				{
					return invokeable.Invoke(method, args);
				}

				if (mAllowMultithreadedCallbacks)
				{
					return method.DynamicInvoke(args);
				}
			}
			catch { }

			return null;
		}

		private void AsyncPollCallback(IAsyncResult result)
		{
			WatchSocket socket = (WatchSocket)result.AsyncState;

			bool ret = socket.EndPoll(result);

			if (socket.Stopping)
			{
				// If we're stopping, don't process any results, and don't begin a new poll.
				return;
			}

			if (ret)
			{
				PollInvokeableDelegate cb = new PollInvokeableDelegate(PollInvokeable);
				Invoke(cb, socket.SDRef);
			}

			// The user may have stopped the socket during the Invoke above
			if (!socket.Stopping)
			{
				AsyncCallback callback = new AsyncCallback(AsyncPollCallback);
				socket.BeginPoll(-1, SelectMode.SelectRead, callback, socket);
			}
		}

		/// <summary>
		/// Starts polling the DNSService socket, and delegates
		/// data back to the primary DNSService API when data arrives
		/// on the socket.
		/// </summary>
		protected void SetupWatchSocket(IntPtr sdRef)
		{
			Int32 socketId = mDNSImports.DNSServiceRefSockFD(sdRef);
			WatchSocket socket = new WatchSocket(socketId, sdRef);

			sdRefToSocketMapping.Add(sdRef, socket);

			AsyncCallback callback = new AsyncCallback(AsyncPollCallback);
			IAsyncResult ar = socket.BeginPoll(-1, SelectMode.SelectRead, callback, socket);
		}

		/// <summary>
		/// This method tears down a previously setup watch socket.
		/// </summary>
		protected void TeardownWatchSocket(IntPtr sdRef)
		{
			WatchSocket socket = (WatchSocket)sdRefToSocketMapping[sdRef];

			if (socket != null)
			{
				socket.Stopping = true;

				// Note that we did not actually stop the poll.
				// This is because there is no way to actually stop the poll.
				// Our only option is to wait for the poll to finish.
				// And since we set the stopping variable, then no further action should be taken by the socket.
				// 
				// This should be fine, since when the DNSServiceRefDeallocate(sdRef) method is invoked,
				// the socket will be shutdown, and the poll will complete.

				sdRefToSocketMapping.Remove(sdRef);
			}
		}
	}
}
