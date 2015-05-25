Touchmote
==============
Introducing Touchmote, a Windows app to control the Windows 8 Metro interface from your couch.<br />
Swipe, scroll and tap by pointing your Wiimote on your screen or HDTV.

Visit http://touchmote.net/ for a compiled installer.

Touchmote is based on the WiiTUIO project which allows data from a Wii Remote to be translated as genuine Windows touch events.<br />
Touch position is calculated using the Wii Sensor Bar.<br />
The application is developed in primarily C# .NET 4.5 and some C++.

Prerequisites
==============
1x Nintendo Wii Remote<br />
1x Wireless Wii Sensor Bar<br />
1x Bluetooth enabled computer with Windows 8

Bug reports
==============
Please use the GitHub Issue tracker to report bugs. Always include the following information:<br />
1. System configuration, including Bluetooth device vendor and model<br />
2. Steps to reproduce the error<br />
3. Expected output<br />
4. Actual output<br />

How to build
==============
*First install:*<br />
Microsoft Visual Studio 2013<br />
Direct X SDK

1. Install the Touchmote drivers and test certificate by running the installer from touchmote.net<br />
2. Run Visual Studio "as Administrator". Open the project file Touchmote.sln. <br />
3. If you want to use the debugger, edit the file called app.manifest and change uiAccess to false. Otherwise the app has to be run under Program Files. This is for the cursor to be able to show on top of the Modern UI.<br />
4. Go to Build->Configuration manager...<br />
5. Choose solution platform for either x86 or x64 depending on your system. Close it and Build.<br />

Credits
==============
WiimoteLib 1.7:  	http://wiimotelib.codeplex.com/<br />
WiiTUIO project:	http://code.google.com/p/wiituio/<br />
TouchInjector:	  http://touchinjector.codeplex.com/<br />
Scarlet.Crush Xinput wrapper:   http://forums.pcsx2.net/Thread-XInput-Wrapper-for-DS3-and-Play-com-USB-Dual-DS2-Controller<br />
WiiPair:  				http://www.richlynch.com/code/wiipair<br />
EcoTUIOdriver:    https://github.com/ecologylab/EcoTUIODriver<br />
MultiTouchVista:  http://multitouchvista.codeplex.com<br />

Release History
==============

**v1.0 beta 14**<br />
- FPS cursor mapping and cursor to stick mapping, thanks rVinor
- Updated OSD GUI

**v1.0 beta 13**<br />
- Less GPU usage
- Works together with other Xbox 360 controls
- Bug fixes

**v1.0 beta 12**<br />
- Classic Controller Pro support
- Raw input support
- Automatic check for new versions

**v1.0 beta 11**<br />
- Support for multiple monitors
- More possibilities with analog sticks
- Better pairing
- Bug fixes

**v1.0 beta 10**<br />
- Added visual keymap editor.
- Experimental Windows 7 support.

**v1.0 beta 9**<br />
- Nunchuk and Classic Controller support.
- XBox 360 controller emulation.
- Change keymaps on the fly. Hold the Home button for 5 seconds to open the layout chooser.
- Pointer will consider Wiimote rotation.
- Better more responsive cursor.
- Enabled "Minimize to tray" option.

**v1.0 beta 8**<br />
- Implemented custom cursors
- New windowed UI
- Added Sleepmode to save battery when Wiimote is not in use.
- Added option to pair a Wiimote at startup.
- Increased CPU utilization, for smoother cursor movement.

**v1.0 beta 7**<br />
- Added ability to connect several Wiimotes.
- Enabled individual keymap settings for each Wiimote.
- Added GameMouse pointer mode through keymap setting.
- Moved settings file into the application folder.
- Fixed 64 bit installer default install folder.
- Fixed support for MultiTouchVista drivers (for Windows 7 or lower)

**v1.0 beta 6**<br />
- Added support for new Wiimotes (RVL-CNT-01-TR)
- Added option to specify Sensor Bar position
- Bugfix, using two touch points would sometimes disable edge gestures

**v1.0 beta 5**<br />
- Multi touch! Use the B button to add a second touch point and zoom or rotate with the A button.
- Added application specific keymaps. Edit or add new keymaps in the Keymaps folder.
- Now using native Windows 8 touch cursor.
- Added helpers to perform edge guestures and taps.

**v1.0 beta 4**<br />
- Much better performance and stability on Windows 8
- Driver is now optional
- Only works on Windows 8, use beta3 for Windows 7/Vista
- Completely disconnects the Wiimote so it doesn't drain battery when not used

**v1.0 beta 3**<br />
- Forgot to enable driver detection
- Added error messaging

**v1.0 beta 2**<br />
- Press minus or plus to zoom in or out
- Press 2 to reset connection to touch driver
- No crash on restart
- Pointer settings saves correctly
- Improved pairing
- Bug fixes

**v1.0 beta 1**<br />
- First release.
