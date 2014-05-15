// Note: I have never seen the Toshiba Bluetooth SDK, nor signed an NDA with them.
// This file was constructed through much trial and error, originally in Delphi, then ported to VC++.
// It is very much incomplete, and some parts will not be correct.

#pragma once

// This can be whatever custom windows message you want, I picked it at random:
#define WM_TOSHIBA_BLUETOOTH WM_APP + 1042

#define TOSHIBA_BLUETOOTH_SEARCH_ERROR    0x600000
#define TOSHIBA_BLUETOOTH_SEARCH_STARTING 0x600001
#define TOSHIBA_BLUETOOTH_SEARCH_FOUND    0x600080
#define TOSHIBA_BLUETOOTH_SEARCH_FINISHED 0x6000FF
#define TOSHIBA_BLUETOOTH_CONNECT_HID_ERROR     0xB00300
#define TOSHIBA_BLUETOOTH_CONNECT_HID_STARTING  0xB00301
#define TOSHIBA_BLUETOOTH_CONNECT_HID_CONNECTED 0xB00380
#define TOSHIBA_BLUETOOTH_CONNECT_HID_FINISHED  0xB003FF
#define TOSHIBA_BLUETOOTH_CONNECTION_CHANGED 0x10100000
#define TOSHIBA_BLUETOOTH_UNKNOWN 0x11000019 // don't know what this means, but I got it after NM_CONNECTION_CHANGED 2, after starting discovery

#pragma pack(push)
#pragma pack(1)

typedef BYTE TToshibaBluetoothAddress[6];

typedef struct {
  DWORD status;
  TToshibaBluetoothAddress BluetoothAddress;
  DWORD ClassOfDevice;
  char name[248];
  WORD unknown2;
} TToshibaBluetoothDevice, *PToshibaBluetoothDevice;

typedef struct {
	DWORD deviceCount;
	TToshibaBluetoothDevice device[7];
} TToshibaBluetoothDeviceList, *PToshibaBluetoothDeviceList;

typedef struct {
	TToshibaBluetoothAddress BluetoothAddress;
	BYTE LMPVersion;
	WORD LMPSubVersion;
	BYTE HCIVersion;
	WORD Manufacturer; // http://www.bluetooth.org/apps/content/?doc_id=49708
	WORD HCIRevision;
	BYTE unknown[16];
	BYTE junk[1024]; // I don't know how big this struct is!
} TBluetoothAdapter;

// API
typedef BOOL(__cdecl *TToshibaBluetoothInitAPI)(HWND WindowHandle, char *ApplicationName, int &error);
typedef BOOL (__cdecl *TToshibaBluetoothShutdownAPI)(int &error);
typedef BOOL (__cdecl *TToshibaBluetoothAdapterGetInfo)(void *pBluetoothAdapterInfo, int &error);
typedef BOOL(__cdecl *TToshibaBluetoothNotify)(DWORD EventMask /* 0 to stop */, int &error, HWND WindowHandle, DWORD MessageNumber);
typedef BOOL(__cdecl *TToshibaBluetoothFreeMemory)(void *p);
typedef BOOL(__cdecl *TToshibaBluetoothDisconnect)(WORD ChannelID, int &error);
// Searching for devices
typedef BOOL(__cdecl *TToshibaBluetoothStartSearching)(PToshibaBluetoothDeviceList &pDeviceList, DWORD flags, int &error, HWND WindowHandle, DWORD MessageNumber, LPARAM lParam);
typedef BOOL(__cdecl *TToshibaBluetoothCancelSearching)(int &error);
typedef BOOL(__cdecl *TBtGetRemoteName)(TToshibaBluetoothAddress BluetoothAddress, char *RemoteName, int &error);
typedef BOOL(__cdecl *TBtAddRemoteDevice)(PToshibaBluetoothDeviceList pDeviceList, int &error);
// other
typedef BOOL(__cdecl *TToshibaBluetoothConnectHID)(TToshibaBluetoothAddress BluetoothAddress, int &error, HWND WindowHandle, DWORD MessageNumber, LPARAM lParam);
typedef BOOL(__cdecl *TToshibaBluetoothClearPIN)(TToshibaBluetoothAddress BluetoothAddress, int &error);
typedef BOOL(__cdecl *TToshibaBluetoothSetPIN)(TToshibaBluetoothAddress BluetoothAddress, char *PinCode, int PinLength, int &error);


BOOL LoadToshibaBluetoothStack(HWND handle);

extern bool hasToshibaAdapter;
extern TToshibaBluetoothAddress ToshibaBluetoothAdapterAddr;
extern PToshibaBluetoothDeviceList pDeviceList;

extern TToshibaBluetoothInitAPI ToshibaBluetoothInitAPI;
extern TToshibaBluetoothShutdownAPI ToshibaBluetoothShutdownAPI, LaunchToshibaBluetoothManagerInSystemTray;
extern TToshibaBluetoothAdapterGetInfo ToshibaBluetoothAdapterGetInfo;
extern TToshibaBluetoothNotify ToshibaBluetoothNotify;
extern TToshibaBluetoothFreeMemory ToshibaBluetoothFreeMemory;
extern TToshibaBluetoothDisconnect ToshibaBluetoothDisconnect;
extern TToshibaBluetoothStartSearching ToshibaBluetoothStartSearching;
extern TToshibaBluetoothConnectHID ToshibaBluetoothConnectHID;
extern TToshibaBluetoothClearPIN ToshibaBluetoothClearPIN;
extern TToshibaBluetoothSetPIN ToshibaBluetoothSetPIN;

#pragma pack(pop)
