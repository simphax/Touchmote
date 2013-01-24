using System;
using System.IO;
using System.Windows;
using System.Collections;
using System.Collections.Generic;

using WiiTUIO.Provider;

/*
 * This code is based on code in the MulitTouch.Driver.Logic namespace provided with the MultiTouchVista project.
 * http://multitouchvista.codeplex.com/
 * License: http://multitouchvista.codeplex.com/license
 */

namespace WiiTUIO.WinTouch
{
    /// <summary>
    /// An enumeration which describes the different states that HID contacts can be in.
    /// </summary>
    public enum HidContactState
    {
        /// <summary>
        /// The contact is an add signal.
        /// </summary>
        Adding,

        /// <summary>
        /// The contact is an update signal.
        /// </summary>
        Updated,

        /// <summary>
        /// The contact is a remove signal.
        /// </summary>
        Removing,
        
        /// <summary>
        /// The contact is a remove signal.
        /// </summary>
        Removed
    }

    /// <summary>
    /// This class defines the information required to represent a HID contact.
    /// </summary>
    internal class HidContactInfo : IEquatable<HidContactInfo>
    {
        private const ushort MaxSize = 32767;
        private static readonly double XRatio = SystemParameters.VirtualScreenWidth / MaxSize;
        private static readonly double YRatio = SystemParameters.VirtualScreenHeight / MaxSize;

        /// <summary>
        /// The state of the contact. e.g. Adding, Updated, Removing, Removed.
        /// </summary>
        public HidContactState State { get; private set; }

        /// <summary>
        /// The X-coordinate of the contact.
        /// </summary>
        public ushort X { get; set; }

        /// <summary>
        /// The Y-coordinate of the contact.
        /// </summary>
        public ushort Y { get; set; }

        /// <summary>
        /// The pressure of the contact.
        /// </summary>
        public ushort Pressure { get; set; }

        /// <summary>
        /// The width of the contact.
        /// </summary>
        public ushort Width { get; set; }

        /// <summary>
        /// The height of the contact.
        /// </summary>
        public ushort Height { get; set; }

        // The unique ID of this contact.
        public ushort Id { get; set; }

        /// <summary>
        /// The timestamp for when this contact was generated.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Size of one contact in bytes
        /// </summary>
        public const byte HidContactInfoSize = 14;

        /// <summary>
        /// TipSwitch indicates the presence of a finger in single touch mode and is thus superfluous in multitouch mode.
        /// TipSwitch is True if the contact is an update and false if added, removed or removing.
        /// </summary>
        public bool TipSwitch
        {
            get
            {
                switch (State)
                {
                    case HidContactState.Adding:
                    case HidContactState.Removing:
                    case HidContactState.Removed:
                        return false;
                    case HidContactState.Updated:
                        return true;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// InRange is True for all events appart from when the touch has definately been removed.
        /// InRange is False if the contact is removed and true if added, removing or updated.
        /// </summary>
        public bool InRange
        {
            get
            {
                switch (State)
                {
                    case HidContactState.Adding:
                    case HidContactState.Removing:
                    case HidContactState.Updated:
                        return true;
                    case HidContactState.Removed:
                        return false;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// A reference to the WiiContact which we derrive this HID contact from.
        /// </summary>
        public WiiContact Contact { get; set; }

        /// <summary>
        /// A constructor that builds a HidContactInfo from a WiiContact and a HID state.
        /// </summary>
        /// <param name="eState"></param>
        /// <param name="contact"></param>
        internal HidContactInfo(HidContactState eState, WiiContact pContact)
        {
            // Save the state.
            State = eState;

            // If we have a contact then shoehorn its details into our class.
            if (pContact != null)
            {
                // Convert the ID.  Perhaps ensure we are going to work with larger values.
                Id = unchecked((UInt16)pContact.ID); 

                // Convert the point into HID space.
                Point tPoint = transformPoint(pContact.Position);
                X = Convert.ToUInt16(tPoint.X);
                Y = Convert.ToUInt16(tPoint.Y);

                // Save the width and height.
                Width  = Convert.ToUInt16(pContact.Size.X);
                Height = Convert.ToUInt16(pContact.Size.Y);

                // Compute the pressure from the area.
                Pressure = Convert.ToUInt16(Math.Max(0, Math.Min(MaxSize, pContact.Area)));

                // Save the timestamp and WiiContact.
                Timestamp = DateTime.Now;
                Contact = pContact;
            }
        }

        /// <summary>
        /// Test if two objects are equal (based on ID).
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if they are equal, false if not.</returns>
        public bool Equals(HidContactInfo obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.Id == Id;
        }

        /// <summary>
        /// Test if two objects are equal (based on ID).
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if they are equal, false if not.</returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(HidContactInfo)) return false;
            return Equals((HidContactInfo)obj);
        }

        /// <summary>
        /// Return a hash-code for this object.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        /// <summary>
        /// Compile a binary buffer which consists of all the data that describes this contact.
        /// 
        /// </summary>
        /// <returns></returns>
        internal byte[] ToBytes()
        {
            byte[] tBuffer = new byte[HidContactInfoSize];
            BitArray pBits = new BitArray(new[] { TipSwitch, InRange });
            pBits.CopyTo(tBuffer, 0);
            using (BinaryWriter pWriter = new BinaryWriter(new MemoryStream(tBuffer)))
            {
                pWriter.Seek(2, SeekOrigin.Begin);
                pWriter.Write(X);
                pWriter.Write(Y);
                pWriter.Write(Pressure);
                pWriter.Write(Width);
                pWriter.Write(Height);
                pWriter.Write(Id);
            }
            return tBuffer;
        }

        /// <summary>
        /// Return a string based representation of this contact.
        /// </summary>
        /// <returns>A string representation of this contact.</returns>
        public override string ToString()
        {
            return string.Format("Id: {0}, State: {1}, TipSwitch: {2}, InRange: {3}, X,Y: {4},{5}, W,H: {6},{7}, Pressure: {8}, TimeStamp: {9}",
                Id, State, TipSwitch, InRange, X, Y, Width, Height, Pressure, Timestamp.Ticks);
        }

        /// <summary>
        /// A helper method which transforms a point from wiiprovider space into hid space.
        /// </summary>
        /// <param name="tPosition">The point in WiiProvider space.</param>
        /// <returns>The transformed point.</returns>
        internal static Point transformPoint(Point tPosition)
        {
            return new Point(Math.Max(0, tPosition.X / XRatio), Math.Max(0, tPosition.Y / YRatio));
        }
    }

    /// <summary>
    /// A simple class which is used to compare HidContactInfo classes by
    /// defining methods to support the comparison of objects for equality.
    /// </summary>
    internal class HidContactInfoEqualityComparer : IEqualityComparer<HidContactInfo>
    {
        /// <summary>
        /// Determines whether the specified objects are equal.
        /// </summary>
        /// <param name="x">The first object to compare.</param>
        /// <param name="y">The second object to compare.</param>
        /// <returns>True if they are the same, false if not.</returns>
        public bool Equals(HidContactInfo x, HidContactInfo y)
        {
            // return x.Id.Equals(x.Id);
            return x.Id.Equals(y.Id);
        }

        /// <summary>
        /// Return the hashcode for a given object.
        /// </summary>
        /// <param name="obj">The Object for which a hash code is to be returned.</param>
        /// <returns>A hash code for the specified object.</returns>
        public int GetHashCode(HidContactInfo obj)
        {
            return obj.Id.GetHashCode();
        }
    }
}
