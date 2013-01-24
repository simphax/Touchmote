using System;

namespace WiiTUIO.Provider
{
    /// <summary>
    /// This describes the various states that a contact can take.  For example, it has just begun, just ended or just moved.
    /// </summary>
    public enum ContactType
    {
        /// <summary>
        /// Describes the start of a new contact.
        /// </summary>
        Start,

        /// <summary>
        /// Describes the end of an existing contact.
        /// </summary>
        End,

        /// <summary>
        /// Describes a change to an existing contact.
        /// </summary>
        Move,
    }
}
