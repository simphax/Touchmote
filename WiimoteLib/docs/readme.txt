Managed Library for Nintendo's Wiimote
v1.7.0.0
by Brian Peek (http://www.brianpeek.com/)

For more information, please visit the associated article for this project at:

http://msdn.microsoft.com/coding4fun/hardware/article.aspx?articleid=1879033

There you will find documentation on how all of this works.

If all else fails, please contact me at the address above.  Enjoy!

Changes
=======

v1.7.0.0
--------
	o Writing registers is now properly waiting for the Wiimote to reply
	  before continuing...this removes all of the Thread.Sleep() calls and
	  should *greatly* improve performance when setting LEDs and rumble
	  (Serial Nightmare & wwibrew.org)
	o Guitar Hero: World Tour Guitar and Drums now properly recognized and
	  used (wiibrew.org, tested by Tyler Tolley and Mauro Milazzo)
	o Guitar whammy bar is now a 5-bit value instead of 4 (wiibrew.org)
	o Position of 4 IRs now properly reported in Basic reporting mode
	  (Dan Carter)
	o Found1/2 now properly reported in MSRS (reported by akka243)
	o MSRS project updated to Microsoft Robotics Developer Studio 2008

v1.6.0.0
--------
	o Added "center of gravity" calculation to the Wii Fit Balance Board
	  (thanks to Steven Battersby)
	o Structs are now marked [Serializable] (suggested by Caio)
	o Battery property is now a float containing the calculated percentage
	  of battery remaining
	o BatteryRaw is the byte value that used to be stored in the Battery
	  property
	o WiimoteTest app now reads extensions properly when inserted at startup
	o Exposed HID device path in new HIDDevicePath property on Wiimote object
	o Changed the time delay on writes to 50ms from 100ms...this should
	  improve responsiveness of setting LEDs and rumble

v1.5.2.0
--------
	o Ok, Balance Board support is *really* fixed this time
	  (thanks to Manuel Schroeder, Eduard Kujit and Alex Wilkinson for testing)
	o LED checkboxes are properly set on the WiimoteTest tabs

v1.5.1.0
--------
	o Oops...a last minute change broke the one thing I was adding:  Balance
	  Board support.  Should be working now...(identified by Manuel Schroeder)

v1.5.0.0
--------
	o Wii Fit Balance Board support
	o The GetStatus() method now waits for a response from the Wiimote before
	  continuing
	o Bug fix for ButtonsExtension report type (0x34)

v1.4.0.0
--------
	o Multiple Wiimotes supported!
	o Slight change to ExtensionType enum for better extension detection
	o Decided I didn't like the dependency on System.Drawing for the 2D point
      so am now using my own Point structs.  Sorry...
	o WiimoteTest app updated to show multiple Wiimotes working

v1.3.0.0
--------
	o SetReportType contains an overload taking a new IRSensitivity parameter
	  which will set the IR camera sensitivity when using an IR report type
	o Created new WiimoteException type which is now thrown by the library
	o Moved InputReport enum to namespace level
	o Events now using the generic EventHandler class instead of custom
	  delegates
	o Refactored the state structures to use Point/PointF and my own
	  Point3/Point3F
	o Refactored IR sensors to be an array
	o Added support for the Guitar Hero controller
	  (tested by Matthias Shapiro, Evan Jacovier)
	o Test app will run without Wiimote connected (Andrea Leganza)
	o ReadData now returns the proper amount of data for requests of more than
	  16 bytes (reported by David Hawley)
	o Test application updated with above changes
	o Lots of breaking changes, but the survey on my site said most didn't care
	  about backwards compatibility...  :)

v1.2.1.0
--------
	o Added support for IR 3 and 4 (Johnny Lee)

v1.2.0.0
--------
	o Moved to CodePlex! (http://www.codeplex.com/WiimoteLib)
	o New license!  Please read the included license.txt/copyright.txt for more
	  info.  This likely doesn't change anything for anyone, but at least now
	  it's official.
	o AltWriteMethod deprecated.  Connect will now determine which write method
	  to use at runtime.  It remains in case someone needs to override the
	  write method for some reason. (gl.tter)
	o WiimoteState.LEDState is now filled with proper values.
	  (identified by gl.tter/Leif902)
	o Extensions that are attached at startup are now recognized properly.
	  (identified by Will Pressly)
	o "Partially inserted" extensions now handled properly (Michael Dorman)
	o SetRumble method now does this via the SetLEDs method instead of using the
	  status report to avoid a needless response from the Wiimote. (Michael Dorman)
	o IRState now contains RawMidX/Y and MidX/Y containing the value of the
	  midpoint between the IR points.
	o Async reads now begin after the data parsing and event has been raised.
	  This should lead to non-overlapping events.
	o Updated the test application with the above changes and cleaned up the UI
	  updates by using delegates a bit more effeciently.

	Breaking Changes (may not be a complete list)
	----------------------------------------------
	o LEDs renamed to LEDState
	o GetBatteryLevel renamed to GetStatus
	o OnWiimoteChanged renamed to WiimoteChanged
	o OnWiimoteExtensionChanged renamed to WiimoteExtensionChanged
	o CalibrationInfo renamed to AccelCalibrationInfo
	o Event handlers renamed to WiimoteChangedEventHandler and
	  WiimoteExtensionChangedEventHandler

v1.1.0.0
--------
	o Support for XP and Vista x64 (Paul Miller)
	o VB fix in ParseExtension (Evan Merz)
	o New "AltWriteMethod" property which will try a secondary approach to writing
	  to the Wiimote.  If you get an error when connecting, set this property and
	  try again to see if it fixes the issue.
	o Microsoft Robotics Studio project
	  Open the WiimoteMSRS directory and start the Wiimote.sln solution to take a
	  look! (David Lee)

v1.0.1.0
--------
	o Calibration copy/paste error (James Darpinian)