Touchmote
==============
Introducing Touchmote, a Windows app to control the Windows 8 Metro interface from your couch.
Swipe, scroll and tap by pointing your Wiimote on your screen or HDTV.

Touchmote is based on the WiiTUIO project which allows data from a Wii Remote to be translated as genuine Windows touch events.
You can choose to use the Wiimote as a pointing device together with a Sensor Bar, or by pointing the Wiimote at your screen and use one or several IR pens to control Windows with multitouch.

Prerequisities
==============
1x Wireless Wii Sensor Bar
1x Wii Remote
Bluetooth enabled computer

How to install
==============
1. Download touchscreen drivers from http://multitouchvista.codeplex.com/releases/view/28979
2. Run Command Prompt as administrator
3. Browse to MultiTouchVista/Driver/x64 or x32 if you are using a 32 bit Windows.
4. Run Install driver.cmd
5. Go to Device Manager and disable and enable "Universal Software HID device"
6. Pair you Wiimote with your computer using your favourite method. You can permanently pair your Wiimote using a program called WiiPair, downloaded from http://www.richlynch.com/custom/dist/WiiPair-1.0-setup.exe
7. Download Touchmote and run it. 
8. Check the Windows Touch 
Start Touchmote and check the Windows Touch check button and click connect.
8. The Wiimote should connect and you are ready to touch away.

How to use
==============
Make sure the Sensor Bar is on.
Point the Wiimote at your screen and hold the A button to perform a touch gesture.
Use the Home button to get to the Metro interface.

Tips
==============
Increase the size of Windows 8 Metro Interface (A must for TVs)
--------------
Follow Microsoft's guide at http://support.microsoft.com/kb/2737167 to override the display size setting. For a 40 inch HDTV I find it good to set it at 10.5 inches. Do not change DPI settings, it will make Touchmote unusable.

Credits
==============
WiiTUIO project		http://code.google.com/p/wiituio/
Johnny Chung Lee: 	http://johnnylee.net/projects/wii/
Brian Peek: 		http://www.brianpeek.com/
Nesher:			http://www.codeplex.com/site/users/view/nesher
TUIO Project: 		http://www.tuio.org
MultiTouchVista:	http://multitouchvista.codeplex.com/
OSC.NET Library:	http://luvtechno.net/
WiimoteLib 1.7:		http://wiimotelib.codeplex.com/
HIDLibrary:		http://hidlibrary.codeplex.com/
WPFNotifyIcon:		http://www.hardcodet.net/projects/wpf-notifyicon
