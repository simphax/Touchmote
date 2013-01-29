Touchmote
==============
Introducing Touchmote, a Windows app to control the Windows 8 Metro interface from your couch.
Swipe, scroll and tap by pointing your Wiimote on your screen or HDTV.

Go to touchmote.net for user info and a ready to use installer.

Touchmote is based on the WiiTUIO project which allows data from a Wii Remote to be translated as genuine Windows touch events.
Touch position is calculated using the Wii Sensor Bar.
The application is developed in primarily WPF .NET 4.0 C# and some C++.

Prerequisites
==============
1x Wireless Wii Sensor Bar
1x Wii Remote
1x Bluetooth enabled computer with Windows 7 or 8

How to build
==============
Get the source and open Touchmote.sln with Microsoft Visual Studio. 
Go to Build->Configuration manager...
Choose solution platform for either x86 or x64 depending on your system. Close it and Build.

Bug reports
==============
Please use the GitHub Issue tracker to report bugs. Always include the following information:
1. System configuration, including Bluetooth device vendor and model
2. Steps to reproduce the error
3. Expected output
4. Actual output

Tips
==============
Increase the size of Windows 8 Metro Interface (A must for TVs)
--------------
Follow Microsoft's guide at http://support.microsoft.com/kb/2737167 to override the display size setting. For a 42 inch HDTV I find it good to set it at 10.5 inches. Do not change DPI settings, it will make Touchmote unusable.

Credits
==============
WiiTUIO project:	http://code.google.com/p/wiituio/
MultiTouchVista:	http://multitouchvista.codeplex.com/
WiimoteLib 1.7:		http://wiimotelib.codeplex.com/
HIDLibrary:		http://hidlibrary.codeplex.com/
WiiPair:			http://www.richlynch.com/code/wiipair
WPFNotifyIcon:		http://www.hardcodet.net/projects/wpf-notifyicon

Release History
==============
- v1.0 beta 1
First release.