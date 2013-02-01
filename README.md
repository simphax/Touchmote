Touchmote
==============
Introducing Touchmote, a Windows app to control the Windows 8 Metro interface from your couch.<br />
Swipe, scroll and tap by pointing your Wiimote on your screen or HDTV.

Go to touchmote.net for user info and a ready to use installer.

Touchmote is based on the WiiTUIO project which allows data from a Wii Remote to be translated as genuine Windows touch events.<br />
Touch position is calculated using the Wii Sensor Bar.<br />
The application is developed in primarily WPF .NET 4.0 C# and some C++.

Prerequisites
==============
1x Wireless Wii Sensor Bar<br />
1x Wii Remote<br />
1x Bluetooth enabled computer with Windows 7 or 8

Bug reports
==============
Please use the GitHub Issue tracker to report bugs. Always include the following information:<br />
1. System configuration, including Bluetooth device vendor and model<br />
2. Steps to reproduce the error<br />
3. Expected output<br />
4. Actual output<br />

How to build
==============
Get the source and open Touchmote.sln with Microsoft Visual Studio. <br />
Go to Build->Configuration manager...<br />
Choose solution platform for either x86 or x64 depending on your system. Close it and Build.<br />

Tips
==============
Increase the size of Windows 8 Metro Interface (A must for TVs)
--------------
Follow Microsoft's guide at http://support.microsoft.com/kb/2737167 to override the display size setting. For a 42 inch HDTV I find it good to set it at 10.5 inches. Do not change DPI settings, it will make Touchmote unusable.

Credits
==============
WiiTUIO project:	http://code.google.com/p/wiituio/<br />
MultiTouchVista:	http://multitouchvista.codeplex.com/<br />
WiimoteLib 1.7:		http://wiimotelib.codeplex.com/<br />
HIDLibrary:				http://hidlibrary.codeplex.com/<br />
WiiPair:					http://www.richlynch.com/code/wiipair<br />
WPFNotifyIcon:		http://www.hardcodet.net/projects/wpf-notifyicon<br />

Release History
==============
**v1.0 beta 2**<br />
- Press minus or plus to zoom in or out
- Press 2 to reset connection to touch driver
- No crash on restart
- Pointer settings saves correctly
- Improved pairing
- Bug fixes

**v1.0 beta 1**<br />
- First release.
