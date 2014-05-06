using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroconfService
{
    /// <summary>
    /// An exception that is thrown when a <see cref="NetService">NetService</see>
    /// or <see cref="NetServiceBrowser">NetServiceBrowser</see> dll error occurs.
    /// </summary>
    public class DNSServiceException : ApplicationException
    {
        string s = null;
        string f = null;
        Exception innerException;
        DNSServiceErrorType e = DNSServiceErrorType.NoError;

        internal DNSServiceException(string s)
        {
            this.s = s;
        }

        internal DNSServiceException(string s, Exception inner)
        {
            this.s = s;
            this.innerException = inner;
        }

        internal DNSServiceException(string function, DNSServiceErrorType error)
        {
            e = error;
            f = function;
            s = String.Format("An error occured in the function '{0}': {1}",
                function, error);
        }

        /// <summary>
        /// Creates a returns a string representation of the current exception
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return s;
        }

        /// <summary>
        /// Gets a message that describes the current exception.
        /// </summary>
        public override string Message
        {
            get { return s; }
        }

        /// <summary>
        /// Returns the Exception that is the root cause of the exception.
        /// </summary>
        /// <returns>The first exception thrown in a chain of exceptions.
        /// If the InnerException property of the current exception is a null reference, this property returns the current exception.
        /// </returns>
        public override Exception GetBaseException()
        {
            return innerException ?? this;
        }

        /// <summary>
        /// Gets the function name (if possible) that returned the underlying error
        /// </summary>
        public string Function { get { return f; } }
        /// <summary>
        /// Gets the <see cref="DNSServiceErrorType">DNSServiceErrorType</see> error
        /// that was returned by the underlying function.
        /// </summary>
        public DNSServiceErrorType ErrorType { get { return e; } }
    }
}
