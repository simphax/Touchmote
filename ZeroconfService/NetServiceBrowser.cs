using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

using System.Windows.Forms;

namespace ZeroconfService
{
	/// <summary>
	/// <para>
	/// The NetServiceBrowser class enables the user of the class to find
	/// published services on a network using multicast DNS. The user uses
	/// an instance of the NetServiceBrowser, called a <B>network service browser</B>,
	/// to find devices such as Printers, HTTP and FTP servers.
	/// </para>
	/// <para>
	/// A <B>network service browser</B> can be used to obtain a list of possible
	/// domains or services. A <see cref="NetService">NetService</see> is then obtained for each discovered
	/// service. You can perform multiple searches at a time by using multiple
	/// <B>network service browsers</B>.
	/// </para>
	/// </summary>
	/// <remarks>
	/// <para>
	/// Network searches are performed asynchronously and are returned to your application
	/// via events fired from within this class. Events are typically fired in
	/// your application's main run loop, see <see cref="DNSService">DNSService</see> for information
	/// about controlling asynchronous events.
	/// </para>
	/// </remarks>
	public sealed class NetServiceBrowser : DNSService, IDisposable
	{
		/// <summary>
		/// Represents the method that will handle <see cref="DidFindService">DidFindService</see>
		/// events from a <see cref="NetServiceBrowser">NetServiceBrowser</see> instance.
		/// </summary>
		/// <param name="browser">Sender of this event.</param>
		/// <param name="service"><see cref="NetService">NetService</see> found by the the browser. This object
		/// can be used to obtain more information about the service.</param>
		/// <param name="moreComing">True when more services will be arriving shortly.</param>
		/// <remarks>
		/// <para>The target uses this delegate to compile a list of services. It should wait
		/// until moreComing is false before updating the user interface, so as to avoid
		/// flickering.
		/// </para>
		/// <para>
		/// The <c>service</c> object inherits its <see cref="DNSService">DNSService</see>
		/// invokable options from <b>browser</b>.
		/// </para>
		/// </remarks>
		public delegate void ServiceFound(NetServiceBrowser browser, NetService service, bool moreComing);

		/// <summary>
		/// Occurs when a <see cref="NetService">NetService</see> was found.
		/// </summary>
		public event ServiceFound DidFindService;

		/// <summary>
		/// Represents the method that will handle <see cref="DidRemoveService">DidRemoveService</see>
		/// events from a <see cref="NetServiceBrowser">NetServiceBrowser</see> instance.
		/// </summary>
		/// <param name="browser">Sender of this event.</param>
		/// <param name="service"><see cref="NetService">NetService</see> to be removed from the browser.
		/// This object is the same instance as was reported by the <see cref="ServiceFound">ServiceFound</see>
		/// event.</param>
		/// <param name="moreComing">True when more services will be made unavailable shortly.</param>
		/// <remarks>
		/// The target uses this delegate to compile a list of services. It should wait
		/// until moreComing is false before updating the user interface, so as to avoid
		/// flickering.
		/// </remarks>
		public delegate void ServiceRemoved(NetServiceBrowser browser, NetService service, bool moreComing);

		/// <summary>
		/// Occurs when a <see cref="NetService">NetService</see> is no longer available.
		/// </summary>
		public event ServiceRemoved DidRemoveService;


		/// <summary>
		/// Represents the method that will handle <see cref="DidFindDomain">DidFindDomain</see>
		/// events from a <see cref="NetServiceBrowser">NetServiceBrowser</see> instance.
		/// </summary>
		/// <param name="browser">Sender of this event.</param>
		/// <param name="domainName">Name of the domain found.</param>
		/// <param name="moreComing">True when more domains will be arriving shortly.</param>
		/// <remarks>
		/// The target uses this delegate to compile a list of domains. It should wait
		/// until moreComing is false before updating the user interface, so as to
		/// avoid flickering.
		/// </remarks>
		public delegate void DomainFound(NetServiceBrowser browser, string domainName, bool moreComing);

		/// <summary>
		/// Occurs when a domain has been found.
		/// </summary>
		public event DomainFound DidFindDomain;

		/// <summary>
		/// Represents the method that will handle <see cref="DidRemoveDomain">DidRemoveDomain</see>
		/// events from a <see cref="NetServiceBrowser">NetServiceBrowser</see> instance.
		/// </summary>
		/// <param name="browser">Sender of this event.</param>
		/// <param name="domainName">Name of the domain that is no longer available.</param>
		/// <param name="moreComing">True when more domains will be made unavailble shortly.</param>
		/// <remarks>
		/// The target uses this delegate to compile a list of domains. It should wait
		/// until moreComing is false before updating the user interface, so as to
		/// avoid flickering.
		/// </remarks>
		public delegate void DomainRemoved(NetServiceBrowser browser, string domainName, bool moreComing);

		/// <summary>
		/// Occurs when a domain is no longer available.
		/// </summary>
		public event DomainRemoved DidRemoveDomain;

		private IntPtr serviceHandle;
        private bool disposed = false;

		private List<NetService> foundServices;
		private mDNSImports.DNSServiceBrowseReply browseReplyCb;
		private mDNSImports.DNSServiceDomainEnumReply domainSearchReplyCb;

		/// <summary>
		/// Initialize a new instance of the <see cref="NetServiceBrowser">NetServiceBrowser</see> class.
		/// </summary>
		public NetServiceBrowser()
		{
			foundServices = new List<NetService>();
		}

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources here
                }
                // Clean up unmanaged resources here
                Stop();
            }
            disposed = true;
        }

        /// <summary>
        /// Releases all resources used by the <see cref="NetServiceBrowser">NetServiceBrowser</see> 
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

		/// <summary>
		/// Starts a search for services of a given type within a given domain.
		/// </summary>
		/// <param name="type">Type of service to search for.</param>
		/// <param name="domain">Domain name in which to search.</param>
		/// <remarks>
		/// The <I>domain</I> argument can be an explicity domain name, the
		/// generic "local." (including the trailing period) domain name or
		/// an empty string ("") which represents the default registration domain.
		/// </remarks>
		public void SearchForService(String type, String domain)
		{
			Stop();
			browseReplyCb = new mDNSImports.DNSServiceBrowseReply(BrowseReply);
			DNSServiceErrorType err = mDNSImports.DNSServiceBrowse(out serviceHandle, 0, 0, type, domain, browseReplyCb, IntPtr.Zero);
			if (err != DNSServiceErrorType.NoError)
			{
				throw new DNSServiceException("DNSServiceBrowse", err);
			}
			SetupWatchSocket(serviceHandle);
		}

		private void SearchForDomains(DNSServiceFlags flags)
		{
			Stop();
			domainSearchReplyCb = new mDNSImports.DNSServiceDomainEnumReply(DomainSearchReply);
			DNSServiceErrorType err = mDNSImports.DNSServiceEnumerateDomains(out serviceHandle, flags, 0, domainSearchReplyCb, IntPtr.Zero);
			if (err != DNSServiceErrorType.NoError)
			{
				throw new DNSServiceException("DNSServiceEnumerateDomains", err);
			}
			SetupWatchSocket(serviceHandle);
		}

		/// <summary>
		/// Starts a search for domains visible to the host.
		/// </summary>
		public void SearchForBrowseableDomains()
		{
            SearchForDomains(DNSServiceFlags.BrowseDomains);
		}

		/// <summary>
		/// Starts a search for domains in which the host may register services.
		/// </summary>
		/// <remarks>
		/// Most clients do not need to use this method. It is normally sufficient
		/// to use the empty string ("") to registers a service in any available domain.
		/// </remarks>
		public void SearchForRegistrationDomains()
		{
			SearchForDomains(DNSServiceFlags.RegistrationDomains);
		}

		/// <summary>
		/// Stops the currently running search or resolution.
		/// </summary>
		public void Stop()
		{
			TeardownWatchSocket(serviceHandle);
			if (serviceHandle != IntPtr.Zero)
			{
				mDNSImports.DNSServiceRefDeallocate(serviceHandle);
				serviceHandle = IntPtr.Zero;
			}
			browseReplyCb = null;
			domainSearchReplyCb = null;
		}

		private  void BrowseReply(IntPtr sdRef,
		                       DNSServiceFlags flags,
		                                UInt32 interfaceIndex,
		                   DNSServiceErrorType errorCode,
		                                String serviceName,
		                                String regtype,
		                                String replyDomain,
		                                IntPtr context)
		{
			bool moreComing = ((flags & DNSServiceFlags.MoreComing) != 0);
			if ((flags & DNSServiceFlags.Add) != 0)
			{
				// Add
				NetService newService = new NetService(replyDomain, regtype, serviceName);
				newService.InheritInvokeOptions(this);

				foundServices.Add(newService);

				if (DidFindService != null)
					DidFindService(this, newService, moreComing);
			}
			else
			{
				// Remove
				foreach (NetService service in foundServices)
				{
					if (service.Name == serviceName && service.Type == regtype && service.Domain == replyDomain)
					{
						foundServices.Remove(service);
						if (DidRemoveService != null)
							DidRemoveService(this, service, moreComing);
						break;
					}
				}
			}
		}

		private void DomainSearchReply(IntPtr sdRef,
		                             DNSServiceFlags flags,
		                                      UInt32 interfaceIndex,
		                         DNSServiceErrorType errorCode,
		                                      String replyDomain,
		                                      IntPtr context)
		{
			bool moreComing = ((flags & DNSServiceFlags.MoreComing) != 0);
			if ((flags & DNSServiceFlags.Add) != 0)
			{ /* add */
				if (DidFindDomain != null)
					DidFindDomain(this, replyDomain, moreComing);
			}
			else
			{ /* remove */
				if (DidRemoveDomain != null)
					DidRemoveDomain(this, replyDomain, moreComing);
			}
		}
    } /* end class */
}
