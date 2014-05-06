using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace ZeroconfService
{
    [Flags]
    enum DNSServiceFlags : uint
	{
		/// <summary>
		/// MoreComing indicates to a callback that at least one more result is
		/// queued and will be delivered following immediately after this one.
		/// Applications should not update their UI to display browse
		/// results when the MoreComing flag is set, because this would
		/// result in a great deal of ugly flickering on the screen.
		/// Applications should instead wait until until MoreComing is not set,
		/// and then update their UI.
		/// When MoreComing is not set, that doesn't mean there will be no more
		/// answers EVER, just that there are no more answers immediately
		/// available right now at this instant. If more answers become available
		/// in the future they will be delivered as usual.
		/// </summary>
		MoreComing          = 0x1,
		
		/// <summary>
		/// Flags for domain enumeration and browse/query reply callbacks.
		/// "Default" applies only to enumeration and is only valid in
		/// conjuction with "Add".  An enumeration callback with the "Add"
		/// flag NOT set indicates a "Remove", i.e. the domain is no longer
		/// valid.
		/// </summary>
		Add                 = 0x2,
		Default             = 0x4,
		
		/// <summary>
		/// Flag for specifying renaming behavior on name conflict when registering
		/// non-shared records. By default, name conflicts are automatically handled
		/// by renaming the service.  NoAutoRename overrides this behavior - with this
		/// flag set, name conflicts will result in a callback.  The NoAutorename flag
		/// is only valid if a name is explicitly specified when registering a service
		/// (i.e. the default name is not used.)
		/// </summary>
		NoAutoRename        = 0x8,
		
		/// <summary>
		/// Flags for specifying domain enumeration type in DNSServiceEnumerateDomains.
		/// BrowseDomains enumerates domains recommended for browsing,
		/// RegistrationDomains enumerates domains recommended for registration.
		/// </summary>
		BrowseDomains       = 0x40,
		RegistrationDomains = 0x80,
		
		/// <summary>
		/// Flag for creating a long-lived unicast query for the DNSServiceQueryRecord call.
		/// </summary>
		LongLivedQuery      = 0x100
	}
	
	enum DNSServiceClass : ushort
	{
		IN = 1 /* Internet */
	}
	
	enum DNSServiceType : ushort
	{
		A     = 1,   /* Host address. */
		PTR   = 12,  /* Domain name pointer. */
		TXT   = 16,  /* One or more text strings. */
		SRV   = 33,  /* Server Selection. */
		ANY   = 255  /* Wildcard match. */
	}
	
	/// <summary>
	/// The error type used by the underlying dnssd.dll. These errors can
	/// be wrapped in <see cref="DNSServiceException">DNSServiceException</see> exceptions.
	/// </summary>
	public enum DNSServiceErrorType : int
	{
		NoError           = 0,
		Unknown           = -65537,  /* 0xFFFE FFFF */
		NoSuchName        = -65538,
		NoMemory          = -65539,
		BadParam          = -65540,
		BadReference      = -65541,
		BadState          = -65542,
		BadFlags          = -65543,
		Unsupported       = -65544,
		NotInitialized    = -65545,
		AlreadyRegistered = -65547,
		NameConflict      = -65548,
		Invalid           = -65549,
		Firewall          = -65550,
		Incompatible      = -65551,  /* client library incompatible with daemon */
		BadInterfaceIndex = -65552,
		Refused           = -65553,
		NoSuchRecord      = -65554,
		NoAuth            = -65555,
		NoSuchKey         = -65556,
		NATTraversal      = -65557,
		DoubleNAT         = -65558,
		BadTime           = -65559,
		Timeout           = -72007  /* NSNetServiceError */
	}


    sealed class mDNSImports
	{
    		
        /// <summary>
        /// Gets the specified property.
        /// </summary>
        /// <param name="name">
        /// 	The requested attribute.
        /// 	Currently the only attribute defined is DNSServiceProperty_DaemonVersion.
        /// </param>
        /// <param name="result">
        /// 	Place to store result.
        /// 	For retrieving DaemonVersion, this should be the address of a UInt32.
        /// </param>
        /// <param name="size">
        /// 	On return, contains size of the result.
        /// 	For DaemonVersion, the returned size is always sizeof(UInt32, but
        /// 	future attributes could be defined which return variable-sized results.
        /// </param>
        /// <returns>
        /// Returns kDNSServiceErr_NoError on success,
        /// 	otherwise returns an error code indicating the specific failure that occurred.
        /// </returns>
		[DllImport("dnssd.dll")]
		public static extern DNSServiceErrorType DNSServiceGetProperty(
		   [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]String name,
		    IntPtr result,
		    ref UInt32 size);

		/// <summary>
		/// Access underlying Unix domain socket for an initialized DNSServiceRef.
		/// The DNS Service Discovery implmementation uses this socket to communicate between
		/// the client and the mDNSResponder daemon.  The application MUST NOT directly read from
		/// or write to this socket.  Access to the socket is provided so that it can be used as a
		/// run loop source, or in a select() loop: when data is available for reading on the socket,
		/// DNSServiceProcessResult() should be called, which will extract the daemon's reply from
		/// the socket, and pass it to the appropriate application callback.  By using a run loop or
		/// select(), results from the daemon can be processed asynchronously.  Without using these
		/// constructs, DNSServiceProcessResult() will block until the response from the daemon arrives.
		/// The client is responsible for ensuring that the data on the socket is processed in a timely
		/// fashion - the daemon may terminate its connection with a client that does not clear its
		/// socket buffer.
		/// </summary>
		/// <param name="sdRef">
		/// 	A DNSServiceRef initialized by any of the DNSService calls.
		/// </param>
		/// <returns>
		/// 	The DNSServiceRef's underlying socket descriptor, or -1 on error.
		/// </returns>
		[DllImport("dnssd.dll")]
		public static extern Int32 DNSServiceRefSockFD(IntPtr sdRef);

        /// <summary>
        /// Read a reply from the daemon, calling the appropriate application callback.  This call will
        /// block until the daemon's response is received.  Use DNSServiceRefSockFD() in
        /// conjunction with a run loop or select() to determine the presence of a response from the
        /// server before calling this function to process the reply without blocking.  Call this function
        /// at any point if it is acceptable to block until the daemon's response arrives.  Note that the
        /// client is responsible for ensuring that DNSServiceProcessResult() is called whenever there is
        /// a reply from the daemon - the daemon may terminate its connection with a client that does not
        /// process the daemon's responses.
        /// </summary>
        /// <param name="sdRef">
        /// 	A DNSServiceRef initialized by any of the DNSService calls that take a callback parameter.
        /// </param>
        /// <returns>
        /// Returns kDNSServiceErr_NoError on success,
        /// 	otherwise returns an error code indicating the specific failure that occurred.
        /// </returns>
		[DllImport("dnssd.dll")]
		public static extern DNSServiceErrorType DNSServiceProcessResult(IntPtr sdRef);
		
		/// <summary>
		/// Terminate a connection with the daemon and free memory associated with the DNSServiceRef.
		/// Any services or records registered with this DNSServiceRef will be deregistered. Any
		/// Browse, Resolve, or Query operations called with this reference will be terminated.
		/// 
		/// Note: If the reference's underlying socket is used in a run loop or select() call, it should
		/// be removed BEFORE DNSServiceRefDeallocate() is called, as this function closes the reference's
		/// socket.
		/// 
		/// Note: If the reference was initialized with DNSServiceCreateConnection(), any DNSRecordRefs
		/// created via this reference will be invalidated by this call - the resource records are
		/// deregistered, and their DNSRecordRefs may not be used in subsequent functions.  Similarly,
		/// if the reference was initialized with DNSServiceRegister, and an extra resource record was
		/// added to the service via DNSServiceAddRecord(), the DNSRecordRef created by the Add() call
		/// is invalidated when this function is called - the DNSRecordRef may not be used in subsequent
		/// functions.
		/// 
		/// Note: This call is to be used only with the DNSServiceRef defined by this API.  It is
		/// not compatible with dns_service_discovery_ref objects defined in the legacy Mach-based
		/// DNSServiceDiscovery.h API.
		/// </summary>
		/// <param name="sdRef">
		/// 	A DNSServiceRef initialized by any of the DNSService calls.
		/// </param>
		[DllImport("dnssd.dll")]
		public static extern void DNSServiceRefDeallocate(IntPtr sdRef);
		
		/// <summary>
		/// Callback method for DNSServiceBrowse()
		/// </summary>
		/// <param name="sdRef">
		/// 	The DNSServiceRef initialized by DNSServiceBrowse().
		/// </param>
		/// <param name="flags">
		/// 	Possible values are kDNSServiceFlagsMoreComing and kDNSServiceFlagsAdd.
		/// 	See flag definitions for details.
		/// </param>
		/// <param name="interfaceIndex">
		/// 	The interface on which the service is advertised.  This index should
		/// 	be passed to DNSServiceResolve() when resolving the service.
		/// </param>
		/// <param name="errorCode">
		/// 	Will be NoError (0) on success, otherwise will
		/// 	indicate the failure that occurred.  Other parameters are undefined if
		/// 	the errorCode is nonzero.
		/// </param>
		/// <param name="serviceName">
		/// 	The discovered service name. This name should be displayed to the user,
		/// 	and stored for subsequent use in the DNSServiceResolve() call.
		/// </param>
		/// <param name="regtype">
		/// 	The service type, which is usually (but not always) the same as was passed
		/// 	to DNSServiceBrowse(). One case where the discovered service type may
		/// 	not be the same as the requested service type is when using subtypes:
		/// 	The client may want to browse for only those ftp servers that allow
		/// 	anonymous connections. The client will pass the string "_ftp._tcp,_anon"
		/// 	to DNSServiceBrowse(), but the type of the service that's discovered
		/// 	is simply "_ftp._tcp". The regtype for each discovered service instance
		/// 	should be stored along with the name, so that it can be passed to
		/// 	DNSServiceResolve() when the service is later resolved.
		/// </param>
		/// <param name="replyDomain">
		/// 	The domain of the discovered service instance. This may or may not be the
		/// 	same as the domain that was passed to DNSServiceBrowse(). The domain for each
		/// 	discovered service instance should be stored along with the name, so that
		/// 	it can be passed to DNSServiceResolve() when the service is later resolved.
		/// </param>
		/// <param name="context">
		/// 	The context pointer that was passed to the callout.
		/// </param>
		public delegate void DNSServiceBrowseReply(
		    IntPtr sdRef,
		    DNSServiceFlags flags,
		    UInt32 interfaceIndex,
		    DNSServiceErrorType errorCode,
		   [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]String serviceName,
		   [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]String regtype,
		   [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]String replyDomain,
		    IntPtr context);

        /// <summary>
        /// Browse for instances of a service.
        /// </summary>
        /// <param name="sdRef">
        /// 	A pointer to an uninitialized DNSServiceRef. If the call succeeds
        /// 	then it initializes the DNSServiceRef, returns NoError,
        /// 	and the browse operation will run indefinitely until the client
        /// 	terminates it by passing this DNSServiceRef to DNSServiceRefDeallocate().
        /// </param>
        /// <param name="flags">
        /// 	Currently ignored, reserved for future use.
        /// </param>
        /// <param name="interfaceIndex">
        /// 	If non-zero, specifies the interface on which to browse for services
        /// 	(the index for a given interface is determined via the if_nametoindex()
        /// 	family of calls.)  Most applications will pass 0 to browse on all available
        /// 	interfaces. See "Constants for specifying an interface index" for more details.
        /// </param>
        /// <param name="regtype">
        /// 	The service type being browsed for followed by the protocol, separated by a
        /// 	dot (e.g. "_ftp._tcp").  The transport protocol must be "_tcp" or "_udp".
        /// </param>
        /// <param name="domain">
        /// 	If non-NULL, specifies the domain on which to browse for services.
        /// 	Most applications will not specify a domain, instead browsing on the
        /// 	default domain(s).
        /// </param>
        /// <param name="callBack">
        /// 	The function to be called when an instance of the service being browsed for
        /// 	is found, or if the call asynchronously fails.
        /// </param>
        /// <param name="context">
        /// 	An application context pointer which is passed to the callback function (may be NULL).
        /// </param>
        /// <returns>
        /// Returns kDNSServiceErr_NoError on succeses (any subsequent, asynchronous
        /// 	errors are delivered to the callback), otherwise returns an error code indicating
        /// 	the error that occurred (the callback is not invoked and the DNSServiceRef
        /// 	is not initialized.)
        /// </returns>
		[DllImport("dnssd.dll")]
		public static extern DNSServiceErrorType DNSServiceBrowse(out IntPtr sdRef,
		    DNSServiceFlags flags,
		    UInt32 interfaceIndex,
		   [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]String regtype,
		   [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]String domain,
		    DNSServiceBrowseReply callBack,
		    IntPtr context);

		
		/// <summary>
		/// Callback method for DNSServiceResolve()
		/// </summary>
		/// <param name="sdRef">
		/// 	The DNSServiceRef initialized by DNSServiceResolve().
		/// </param>
		/// <param name="flags">
		/// 	Currently unused, reserved for future use.
		/// </param>
		/// <param name="interfaceIndex">
		/// 	The interface on which the service was resolved.
		/// </param>
		/// <param name="errorCode">
		/// 	Will be NoError (0) on success, otherwise will
		/// 	indicate the failure that occurred.  Other parameters are undefined if
		/// 	the errorCode is nonzero.
		/// </param>
		/// <param name="fullname">
		/// 	The full service domain name, in the form [servicename].[protocol].[domain].
		/// 	(This name is escaped following standard DNS rules, making it suitable for
		/// 	passing to standard system DNS APIs such as res_query(), or to the
		/// 	special-purpose functions included in this API that take fullname parameters.
		/// </param>
		/// <param name="hosttarget">
		/// 	The target hostname of the machine providing the service.  This name can
		/// 	be passed to functions like gethostbyname() to identify the host's IP address.
		/// </param>
		/// <param name="port">
		/// 	The port, in network byte order, on which connections are accepted for this service.
		/// </param>
		/// <param name="txtLen">
		/// 	The length of the txt record, in bytes.
		/// </param>
		/// <param name="txtRecord">
		/// 	The service's primary txt record, in standard txt record format.
		/// </param>
		/// <param name="context">
		/// 	The context pointer that was passed to the callout.
		/// </param>
		public delegate void DNSServiceResolveReply(IntPtr sdRef,
		    DNSServiceFlags flags,
		    UInt32 interfaceIndex,
		    DNSServiceErrorType errorCode,
		   [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] String fullname,
		   [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] String hosttarget,
		    UInt16 port,
		    UInt16 txtLen,
		   [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 7)] byte[] txtRecord,
		    IntPtr context);

        /// <summary>
        /// Resolve a service name discovered via DNSServiceBrowse()
        /// to a target host name, port number, and txt record.
        ///  Note: Applications should NOT use DNSServiceResolve() solely for 
        ///  txt record monitoring - use DNSServiceQueryRecord() instead,
        ///  as it is more efficient for this task.
        ///  
        /// Note: When the desired results have been returned,
        /// the client MUST terminate the resolve by calling DNSServiceRefDeallocate().
        /// Note: DNSServiceResolve() behaves correctly for typical services that have
        /// a single SRV record and a single TXT record. To resolve non-standard
        /// services with multiple SRV or TXT records, DNSServiceQueryRecord() should be used.
        /// </summary>
        /// <param name="sdRef">
        /// 	A pointer to an uninitialized DNSServiceRef.
        /// 	If the call succeeds then it initializes the DNSServiceRef, returns
        /// 	NoError, and the resolve operation will run indefinitely until
        /// 	the client terminates it by passing this DNSServiceRef to DNSServiceRefDeallocate().
        /// </param>
        /// <param name="flags">
        /// 	Currently ignored, reserved for future use.
        /// </param>
        /// <param name="interfaceIndex">
        /// 	The interface on which to resolve the service. If this resolve call is
        /// 	as a result of a currently active DNSServiceBrowse() operation, then the
        /// 	interfaceIndex should be the index reported in the DNSServiceBrowseReply
        /// 	callback. If this resolve call is using information previously saved
        /// 	(e.g. in a preference file) for later use, then use interfaceIndex 0, because
        /// 	the desired service may now be reachable via a different physical interface.
        /// 	See "Constants for specifying an interface index" for more details.
        /// </param>
        /// <param name="name">
        /// 	The name of the service instance to be resolved,
        /// 	as reported to the DNSServiceBrowseReply() callback.
        /// </param>
        /// <param name="regtype">
        /// 	The type of the service instance to be resolved,
        /// 	as reported to the DNSServiceBrowseReply() callback.
        /// </param>
        /// <param name="domain">
        /// 	The domain of the service instance to be resolved,
        /// 	as reported to the DNSServiceBrowseReply() callback.
        /// </param>
        /// <param name="callBack">
        /// 	The function to be called when a result is found, or if the call
        /// 	asynchronously fails.
        /// </param>
        /// <param name="context">
        /// 	An application context pointer which is passed to the callback function (may be NULL).
        /// </param>
        /// <returns>
        /// Returns kDNSServiceErr_NoError on succeses (any subsequent, asynchronous
        /// 	errors are delivered to the callback), otherwise returns an error code indicating
        /// 	the error that occurred (the callback is never invoked and the DNSServiceRef
        /// 	is not initialized.)
        /// </returns>
		[DllImport("dnssd.dll")]
		public static extern DNSServiceErrorType DNSServiceResolve(out IntPtr sdRef,
		    DNSServiceFlags flags,
		    UInt32 interfaceIndex,
		   [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]String name,
		   [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]String regtype,
		   [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]String domain,
		    DNSServiceResolveReply callBack,
		    IntPtr context);
		
		/// <summary>
		/// Callback method for DNSServiceQueryRecord()
		/// </summary>
		/// <param name="sdRef">
		/// 	The DNSServiceRef initialized by DNSServiceQueryRecord().
		/// </param>
		/// <param name="flags">
		/// 	Possible values are kDNSServiceFlagsMoreComing and
		/// 	kDNSServiceFlagsAdd.  The Add flag is NOT set for PTR records
		/// 	with a ttl of 0, i.e. "Remove" events.
		/// </param>
		/// <param name="interfaceIndex">
		/// 	The interface on which the query was resolved (the index for a given
		/// 	interface is determined via the if_nametoindex() family of calls).
		/// 	See "Constants for specifying an interface index" for more details.
		/// </param>
		/// <param name="errorCode">
		/// 	Will be NoError on success, otherwise will
		/// 	indicate the failure that occurred.  Other parameters are undefined if
		/// 	errorCode is nonzero.
		/// </param>
		/// <param name="fullname">
		/// 	The resource record's full domain name.
		/// </param>
		/// <param name="rrType">
		/// 	The resource record's type (e.g. kDNSServiceType_PTR, kDNSServiceType_SRV, etc)
		/// </param>
		/// <param name="rrClass">
		/// 	The class of the resource record (usually kDNSServiceClass_IN).
		/// </param>
		/// <param name="rdLength">
		/// 	The length, in bytes, of the resource record rdata.
		/// </param>
		/// <param name="rData">
		/// 	The raw rdata of the resource record.
		/// </param>
		/// <param name="ttl">
		/// 	The resource record's time to live, in seconds.
		/// </param>
		/// <param name="context">
		/// 	The context pointer that was passed to the callout.
		/// </param>
		public delegate void DNSServiceQueryReply(
		    IntPtr sdRef,
		    DNSServiceFlags flags,
		    UInt32 interfaceIndex,
		    DNSServiceErrorType errorCode,
		   [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]String fullname,
		    DNSServiceType rrType,
		    DNSServiceClass rrClass,
		    UInt16 rdLength,
		   [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 7)]byte[] rData,
		    UInt32 ttl,
		    IntPtr context);

        /// <summary>
        /// Query for an arbitrary DNS record.
        /// </summary>
        /// <param name="sdRef">
        /// 	A pointer to an uninitialized DNSServiceRef. If the call succeeds
        /// 	then it initializes the DNSServiceRef, returns NoError,
        /// 	and the query operation will run indefinitely until the client
        /// 	terminates it by passing this DNSServiceRef to DNSServiceRefDeallocate().
        /// </param>
        /// <param name="flags">
        /// 	Pass kDNSServiceFlagsLongLivedQuery to create a "long-lived" unicast
        /// 	query in a non-local domain.  Without setting this flag, unicast queries
        /// 	will be one-shot - that is, only answers available at the time of the call
        /// 	will be returned.  By setting this flag, answers (including Add and Remove
        /// 	events) that become available after the initial call is made will generate
        /// 	callbacks.  This flag has no effect on link-local multicast queries.
        /// </param>
        /// <param name="interfaceIndex">
        /// 	If non-zero, specifies the interface on which to issue the query
        /// 	(the index for a given interface is determined via the if_nametoindex()
        /// 	family of calls.)  Passing 0 causes the name to be queried for on all
        /// 	interfaces. See "Constants for specifying an interface index" for more details.
        /// </param>
        /// <param name="fullname">
        /// 	The full domain name of the resource record to be queried for.
        /// </param>
        /// <param name="rrType">
        /// 	The numerical type of the resource record to be queried for
        /// 	(e.g. kDNSServiceType_PTR, kDNSServiceType_SRV, etc)
        /// </param>
        /// <param name="rrClass">
        /// 	The class of the resource record (usually kDNSServiceClass_IN).
        /// </param>
        /// <param name="callBack">
        /// 	The function to be called when a result is found, or if the call
        /// 	asynchronously fails.
        /// </param>
        /// <param name="context">
        /// 	An application context pointer which is passed to the callback function (may be NULL).
        /// </param>
        /// <returns>
        /// Returns kDNSServiceErr_NoError on succeses (any subsequent, asynchronous
        /// 	errors are delivered to the callback), otherwise returns an error code indicating
        /// 	the error that occurred (the callback is never invoked and the DNSServiceRef
        /// 	is not initialized.)
        /// </returns>
		[DllImport("dnssd.dll")]
		public static extern DNSServiceErrorType DNSServiceQueryRecord(out IntPtr sdRef,
		    DNSServiceFlags flags,
		    UInt32 interfaceIndex,
		   [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]String fullname,
		    DNSServiceType rrType,
		    DNSServiceClass rrClass,
		    DNSServiceQueryReply callBack,
		    IntPtr context);
		
		/// <summary>
		/// Callback method for DNSServiceEnumerateDomains()
		/// </summary>
		/// <param name="sdRef">
		/// 	The DNSServiceRef initialized by DNSServiceEnumerateDomains().
		/// </param>
		/// <param name="flags">
		/// 	Possible values are:
		/// 	kDNSServiceFlagsMoreComing
		/// 	kDNSServiceFlagsAdd
		/// 	kDNSServiceFlagsDefault
		/// </param>
		/// <param name="interfaceIndex">
		/// 	Specifies the interface on which the domain exists.  (The index for a given
		/// 	interface is determined via the if_nametoindex() family of calls.)
		/// </param>
		/// <param name="errorCode">
		/// 	Will be NoError (0) on success, otherwise indicates
		/// 	the failure that occurred (other parameters are undefined if errorCode is nonzero).
		/// </param>
		/// <param name="replyDomain">
		/// 	The name of the domain.
		/// </param>
		/// <param name="context">
		/// 	The context pointer passed to DNSServiceEnumerateDomains.
		/// </param>
		public delegate void DNSServiceDomainEnumReply(
		    IntPtr sdRef,
		    DNSServiceFlags flags,
		    UInt32 interfaceIndex,
		    DNSServiceErrorType errorCode,
		   [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]String replyDomain,
		    IntPtr context);

        /// <summary>
        /// Asynchronously enumerate domains available for browsing and registration.
        /// The enumeration MUST be cancelled via DNSServiceRefDeallocate() when no more domains
        /// are to be found.
        /// Note that the names returned are (like all of DNS-SD) UTF-8 strings,
        /// and are escaped using standard DNS escaping rules.
        /// (See "Notes on DNS Name Escaping" earlier in this file for more details.)
        /// A graphical browser displaying a hierarchical tree-structured view should cut
        /// the names at the bare dots to yield individual labels, then de-escape each
        /// label according to the escaping rules, and then display the resulting UTF-8 text.
        /// </summary>
        /// <param name="sdRef">
        /// 	A pointer to an uninitialized DNSServiceRef. If the call succeeds
        /// 	then it initializes the DNSServiceRef, returns NoError,
        /// 	and the enumeration operation will run indefinitely until the client
        /// 	terminates it by passing this DNSServiceRef to DNSServiceRefDeallocate().
        /// </param>
        /// <param name="flags">
        /// 	Possible values are:
        /// 	kDNSServiceFlagsBrowseDomains to enumerate domains recommended for browsing.
        /// 	kDNSServiceFlagsRegistrationDomains to enumerate domains recommended for registration.
        /// </param>
        /// <param name="interfaceIndex">
        /// 	If non-zero, specifies the interface on which to look for domains.
        /// 	(the index for a given interface is determined via the if_nametoindex()
        /// 	family of calls.)  Most applications will pass 0 to enumerate domains on
        /// 	all interfaces. See "Constants for specifying an interface index" for more details.
        /// </param>
        /// <param name="callBack">
        /// 	The function to be called when a domain is found or the call asynchronously fails.
        /// </param>
        /// <param name="context">
        /// 	An application context pointer which is passed to the callback function (may be NULL).
        /// </param>
        /// <returns>
        /// Returns kDNSServiceErr_NoError on succeses (any subsequent, asynchronous
        /// 	errors are delivered to the callback), otherwise returns an error code indicating
        /// 	the error that occurred (the callback is not invoked and the DNSServiceRef
        /// 	is not initialized.)
        /// </returns>
		[DllImport("dnssd.dll")]
		public static extern DNSServiceErrorType DNSServiceEnumerateDomains(
		    out IntPtr sdRef,
		    DNSServiceFlags flags,
		    UInt32 interfaceIndex,
		    DNSServiceDomainEnumReply callBack,
		    IntPtr context);
		
		/// <summary>
		/// Callback method for DNSServiceRegister()
		/// </summary>
		/// <param name="sdRef">
		/// 	The DNSServiceRef initialized by DNSServiceRegister().
		/// </param>
		/// <param name="flags">
		/// 	Currently unused, reserved for future use.
		/// </param>
		/// <param name="errorCode">
		/// 	Will be NoError on success, otherwise will
		/// 	indicate the failure that occurred (including name conflicts,
		/// 	if the kDNSServiceFlagsNoAutoRename flag was used when registering.)
		/// 	Other parameters are undefined if errorCode is nonzero.
		/// </param>
		/// <param name="name">
		/// 	The service name registered (if the application did not specify a name in
		/// 	DNSServiceRegister(), this indicates what name was automatically chosen).
		/// </param>
		/// <param name="regtype">
		/// 	The type of service registered, as it was passed to the callout.
		/// </param>
		/// <param name="domain">
		/// 	The domain on which the service was registered (if the application did not
		/// 	specify a domain in DNSServiceRegister(), this indicates the default domain
		/// 	on which the service was registered).
		/// </param>
		/// <param name="context">
		/// 	The context pointer that was passed to the callout.
		/// </param>
		public delegate void DNSServiceRegisterReply(
		    IntPtr sdRef,
		    DNSServiceFlags flags,
		    DNSServiceErrorType errorCode,
		   [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]String name,
		   [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]String regtype,
		   [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]String domain,
		    IntPtr context);

        /// <summary>
        /// Register a service that is discovered via Browse() and Resolve() calls.
        /// </summary>
        /// <param name="sdRef">
        /// 	A pointer to an uninitialized DNSServiceRef. If the call succeeds
        /// 	then it initializes the DNSServiceRef, returns NoError,
        /// 	and the registration will remain active indefinitely until the client
        /// 	terminates it by passing this DNSServiceRef to DNSServiceRefDeallocate().
        /// </param>
        /// <param name="flags">
        /// 	Indicates the renaming behavior on name conflict (most applications will pass 0).
        /// 	See flag definitions above for details.
        /// </param>
        /// <param name="interfaceIndex">
        /// 	If non-zero, specifies the interface on which to register the service
        /// 	(the index for a given interface is determined via the if_nametoindex() family of calls.)
        /// 	Most applications will pass 0 to register on all available interfaces.
        /// 	See "Constants for specifying an interface index" for more details.
        /// </param>
        /// <param name="name">
        /// 	If non-NULL, specifies the service name to be registered.
        /// 	Most applications will not specify a name, in which case the computer
        /// 	name is used (this name is communicated to the client via the callback).
        /// 	If a name is specified, it must be 1-63 bytes of UTF-8 text.
        /// 	If the name is longer than 63 bytes it will be automatically truncated
        /// 	to a legal length, unless the NoAutoRename flag is set,
        /// 	in which case BadParam will be returned.
        /// </param>
        /// <param name="regtype">
        /// 	The service type followed by the protocol, separated by a dot
        /// 	(e.g. "_ftp._tcp"). The service type must be an underscore, followed
        /// 	by 1-14 characters, which may be letters, digits, or hyphens.
        /// 	The transport protocol must be "_tcp" or "_udp". New service types
        /// 	should be registered at http://www.dns-sd.org/ServiceTypes.html
        /// </param>
        /// <param name="domain">
        /// 	If non-NULL, specifies the domain on which to advertise the service.
        /// 	Most applications will not specify a domain, instead automatically
        /// 	registering in the default domain(s).
        /// </param>
        /// <param name="host">
        /// 	If non-NULL, specifies the SRV target host name.  Most applications
        /// 	will not specify a host, instead automatically using the machine's
        /// 	default host name(s).  Note that specifying a non-NULL host does NOT
        /// 	create an address record for that host - the application is responsible
        /// 	for ensuring that the appropriate address record exists, or creating it
        /// 	via DNSServiceRegisterRecord().
        /// </param>
        /// <param name="port">
        /// 	The port, in network byte order, on which the service accepts connections.
        /// 	Pass 0 for a "placeholder" service (i.e. a service that will not be discovered
        /// 	by browsing, but will cause a name conflict if another client tries to
        /// 	register that same name).  Most clients will not use placeholder services.
        /// </param>
        /// <param name="txtLen">
        /// 	The length of the txtRecord, in bytes.  Must be zero if the txtRecord is NULL.
        /// </param>
        /// <param name="txtRecord">
        /// 	The TXT record rdata. A non-NULL txtRecord MUST be a properly formatted DNS
        /// 	TXT record, i.e. [length byte] [data] [length byte] [data] ...
        /// 	Passing NULL for the txtRecord is allowed as a synonym for txtLen=1, txtRecord="",
        /// 	i.e. it creates a TXT record of length one containing a single empty string.
        /// 	RFC 1035 doesn't allow a TXT record to contain *zero* strings, so a single empty
        /// 	string is the smallest legal DNS TXT record.
        /// </param>
        /// <param name="callBack">
        /// 	The function to be called when the registration completes or asynchronously
        /// 	fails.  The client MAY pass NULL for the callback -  The client will NOT be notified
        /// 	of the default values picked on its behalf, and the client will NOT be notified of any
        /// 	asynchronous errors (e.g. out of memory errors, etc.) that may prevent the registration
        /// 	of the service.  The client may NOT pass the NoAutoRename flag if the callback is NULL.
        /// 	The client may still deregister the service at any time via DNSServiceRefDeallocate().
        /// </param>
        /// <param name="context">
        /// 	An application context pointer which is passed to the callback function (may be NULL).
        /// </param>
        /// <returns>
        /// Returns kDNSServiceErr_NoError on succeses (any subsequent, asynchronous
        /// 	errors are delivered to the callback), otherwise returns an error code indicating
        /// 	the error that occurred (the callback is never invoked and the DNSServiceRef
        /// 	is not initialized.)
        /// </returns>
		[DllImport("dnssd.dll")]
		public static extern DNSServiceErrorType DNSServiceRegister(
		    out IntPtr sdRef,
		    DNSServiceFlags flags,
		    UInt32 interfaceIndex,
		   [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]String name,
		   [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]String regtype,
		   [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]String domain,
		   [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]String host,
		    UInt16 port,
		    UInt16 txtLen,
		    byte[] txtRecord,
		    DNSServiceRegisterReply callBack,
		    IntPtr context);

        /// <summary>
        /// Update a registered resource record.  The record must either be:
        ///   - The primary txt record of a service registered via DNSServiceRegister()
        ///   - A record added to a registered service via DNSServiceAddRecord()
        ///   - An individual record registered by DNSServiceRegisterRecord()
        /// </summary>
        /// <param name="sdRef">
        /// 	A DNSServiceRef that was initialized by DNSServiceRegister() or DNSServiceCreateConnection().
        /// </param>
        /// <param name="recordRef">
        /// 	A DNSRecordRef initialized by DNSServiceAddRecord, or NULL to update the service's primary txt record.
        /// </param>
        /// <param name="flags">
        /// 	Currently ignored, reserved for future use.
        /// </param>
        /// <param name="rdLength">
        /// 	The length, in bytes, of the new rdata.
        /// </param>
        /// <param name="rData">
        /// 	The new rdata to be contained in the updated resource record.
        /// </param>
        /// <param name="ttl">
        /// 	The time to live of the updated resource record, in seconds.
        /// 	Specify a value of 0 for the TTL to allow mDNSResponder to automatically choose the default value.
        /// </param>
        /// <returns>Returns kDNSServiceErr_NoError on success, otherwise returns an error code indicating the error that occurred.</returns>
		[DllImport("dnssd.dll")]
		public static extern DNSServiceErrorType DNSServiceUpdateRecord(
			IntPtr sdRef,
			IntPtr recordRef,
			DNSServiceFlags flags,
			UInt16 rdLength,
			[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)]byte[] rData,
			UInt32 ttl);
		
	}
}
