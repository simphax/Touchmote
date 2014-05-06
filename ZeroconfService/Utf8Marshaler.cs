using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ZeroconfService
{
    public class Utf8Marshaler : ICustomMarshaler
    {
        private string cookie;
        private int nativeDataSize = 0;

        public Utf8Marshaler(string cookie)
        {
            this.cookie = cookie;
        }

        public Object MarshalNativeToManaged(IntPtr pNativeData)
        {
            if (pNativeData == IntPtr.Zero)
                return null;
            List<byte> bytes = new List<byte>();
            byte readbyte;
            int i = 0;
            while ((readbyte = Marshal.ReadByte(pNativeData, i)) != 0)
            {
                bytes.Add(readbyte);
                i++;
            }
            return Encoding.UTF8.GetString(bytes.ToArray());
        }

        public IntPtr MarshalManagedToNative(Object managedObject)
        {
            String inString = (String)managedObject;
            if (inString == null)
                return IntPtr.Zero;
            byte[] utf8bytes = Encoding.UTF8.GetBytes(inString);
            nativeDataSize = utf8bytes.Length + 1;
            IntPtr ptr = Marshal.AllocHGlobal(nativeDataSize);
            Marshal.Copy(utf8bytes, 0, ptr, utf8bytes.Length);
            Marshal.WriteByte(ptr, utf8bytes.Length, 0);
            return ptr;
        }

        public int GetNativeDataSize()
        {
            return nativeDataSize;
        }

        public void CleanUpManagedData(Object managedObject)
        {
        }

        public void CleanUpNativeData(IntPtr pNativeData)
        {
            if (pNativeData == IntPtr.Zero) return;
            Marshal.FreeHGlobal(pNativeData);
        }

        public static ICustomMarshaler GetInstance(String cookie)
        {
            return new Utf8Marshaler(cookie);
        }
    }
}
