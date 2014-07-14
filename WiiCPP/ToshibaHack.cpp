#include "stdafx.h"
#include <Windows.h>
#include <Shlobj.h>
#include "ToshibaHack.h"

TToshibaBluetoothInitAPI ToshibaBluetoothInitAPI;
TToshibaBluetoothShutdownAPI ToshibaBluetoothShutdownAPI, LaunchToshibaBluetoothManagerInSystemTray;
TToshibaBluetoothAdapterGetInfo ToshibaBluetoothAdapterGetInfo;
TToshibaBluetoothNotify ToshibaBluetoothNotify;
TToshibaBluetoothFreeMemory ToshibaBluetoothFreeMemory;
TToshibaBluetoothDisconnect ToshibaBluetoothDisconnect;
TToshibaBluetoothStartSearching ToshibaBluetoothStartSearching;
TToshibaBluetoothConnectHID ToshibaBluetoothConnectHID;
TToshibaBluetoothClearPIN ToshibaBluetoothClearPIN;
TToshibaBluetoothSetPIN ToshibaBluetoothSetPIN;

HWND ToshibaWindowHandle;
HMODULE dll;
bool loaded = false, hasToshibaAdapter = false;
TToshibaBluetoothAddress ToshibaBluetoothAdapterAddr;
PToshibaBluetoothDeviceList pDeviceList = NULL;

void DebugPrint(char *s) {
	//OutputDebugStringA(s);
	System::Console::WriteLine(gcnew System::String(s));
}

void GetProgramFiles32Dir(wchar_t *s, wchar_t *suffix) {
#ifdef WIN64
	if (SHGetSpecialFolderPath(0, s, CSIDL_PROGRAM_FILESX86, false)) {
		wcscat_s(s, MAX_PATH, suffix);
	}
#else
	if (SHGetSpecialFolderPath(0, s, CSIDL_PROGRAM_FILES, false)) {
		wcscat_s(s, MAX_PATH, suffix);
	}
#endif
	else s[0] = '\0';
}

BOOL LoadToshibaBluetoothStack(HWND handle) {
	int error = 0;
	char RemoteName[1024];
	BOOL result;

	if (loaded) return true;
	ZeroMemory(ToshibaBluetoothAdapterAddr, 6);
	ToshibaWindowHandle = handle;
	dll = LoadLibrary(L"TosBtAPI.dll");
	if (!dll) {
		DebugPrint("[Bluetooth] Toshiba Bluetooth API TosBtAPI.dll is not in path");
		wchar_t path[MAX_PATH];
#ifdef WIN32
		GetProgramFiles32Dir(path, L"\\Toshiba\\Bluetooth Toshiba Stack\\sys\\TosBtAPI.dll");
#else
		GetProgramFiles32Dir(path, L"\\Toshiba\\Bluetooth Toshiba Stack\sys\\x64\\TosBtAPI.dll");
#endif
		dll = LoadLibrary(path);
		if (!dll) {
			DebugPrint("[Bluetooth] Toshiba Bluetooth API TosBtAPI.dll does not exist.");
			return false;
		}
	}
	DebugPrint("[Bluetooth] Toshiba Bluetooth API TosBtAPI.dll loaded.");
	ToshibaBluetoothInitAPI = (TToshibaBluetoothInitAPI)GetProcAddress(dll, "BtOpenAPI");
	ToshibaBluetoothShutdownAPI = (TToshibaBluetoothShutdownAPI)GetProcAddress(dll, "BtCloseAPI");
	ToshibaBluetoothAdapterGetInfo = (TToshibaBluetoothAdapterGetInfo)GetProcAddress(dll, "BtGetLocalInfo2");
	ToshibaBluetoothNotify = (TToshibaBluetoothNotify)GetProcAddress(dll, "BtNotifyEvent");
	ToshibaBluetoothFreeMemory = (TToshibaBluetoothFreeMemory)GetProcAddress(dll, "BtMemFree");
	ToshibaBluetoothDisconnect = (TToshibaBluetoothDisconnect)GetProcAddress(dll, "BtDisconnect");
	LaunchToshibaBluetoothManagerInSystemTray = (TToshibaBluetoothShutdownAPI)GetProcAddress(dll, "BtExecBtMng");
	ToshibaBluetoothStartSearching = (TToshibaBluetoothStartSearching)GetProcAddress(dll, "BtDiscoverRemoteDevice2");
	ToshibaBluetoothConnectHID = (TToshibaBluetoothConnectHID)GetProcAddress(dll, "BtConnectHID");
	ToshibaBluetoothClearPIN = (TToshibaBluetoothClearPIN)GetProcAddress(dll, "BtClearAutoReplyPinCode");
	ToshibaBluetoothSetPIN = (TToshibaBluetoothSetPIN)GetProcAddress(dll, "BtSetAutoReplyPinCode");

	if (!ToshibaBluetoothInitAPI) {
		DebugPrint("[Bluetooth] Toshiba Bluetooth API is missing BtOpenAPI function.");
		return false;
	}
	result = ToshibaBluetoothInitAPI(handle, "Touchmote", error);
	if (result)
		DebugPrint("[Bluetooth] Toshiba BtOpenAPI returned false.");
	else
		DebugPrint("[Bluetooth] Toshiba BtOpenAPI returned true.");
	if (!LaunchToshibaBluetoothManagerInSystemTray) {
		DebugPrint("[Bluetooth] Toshiba BtExecBtMng function is missing.");
	} else {
		// this is a blocking call that may take a long time if the Bluetooth Manager is not running.
		// it will fail with -3005 if it times out.
		//LaunchToshibaBluetoothManagerInSystemTray(error);
	}
	if (!ToshibaBluetoothAdapterGetInfo) {
		DebugPrint("[Bluetooth] Toshiba BtGetLocalInfo2 function is missing.");
	} else {
		error = 0;
		//result = ToshibaBluetoothAdapterGetInfo(RemoteName, error);
		//memcpy(ToshibaBluetoothAdapterAddr, RemoteName, 6);
		if (error == 0)
			hasToshibaAdapter = true;
	}
	if (!ToshibaBluetoothNotify) {
		DebugPrint("[Bluetooth] Toshiba BtNotifyEvent function is missing.");
	}
	else {
		error = 0;
		//result = ToshibaBluetoothNotify(0xFFFFFFFF, error, handle, WM_TOSHIBA_BLUETOOTH);
	}
	if (!ToshibaBluetoothStartSearching) {
		DebugPrint("[Bluetooth] Toshiba BtDiscoverRemoteDevice2 function is missing.");
	}
	else {
	}

	loaded = true;
	return result;
}
