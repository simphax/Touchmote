// WiiCPP.h

#pragma once

#include "stdafx.h"

#include <tchar.h>
#include <windows.h>
#include <bthsdpdef.h>
#include <bthdef.h>
#include <BluetoothAPIs.h>
#include <strsafe.h>

#pragma comment(lib, "Bthprops.lib")

using namespace std;
using namespace System;

namespace WiiCPP {

	public ref class WiiPairReport
	{
	public:

		enum class Status
		{
			RUNNING,
			CANCELLED,
			EXCEPTION,
			DONE
		};

		Status status;
		int numberPaired;
		bool removeMode;
		array<String^>^ deviceNames;
	};

	public interface class WiiPairListener
	{
	public:

		enum class MessageType 
		{
			INFO,
			SUCCESS,
			ERR
		};


		void pairingConsole(System::String ^message);
		void pairingMessage(System::String ^message, MessageType type);
		void onPairingStarted();
		void onPairingProgress(WiiPairReport ^report);
	};

	public ref class WiiPair
	{
	private:

		bool killme;
		bool cancelled;

		WiiPairListener ^listener;

		DWORD ShowErrorCode(LPTSTR msg, DWORD dw) 
		{ 
			// Retrieve the system error message for the last-error code

			LPVOID lpMsgBuf;

			FormatMessage(
				FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
				NULL,
				dw,
				MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
				(LPTSTR) &lpMsgBuf,
				0, 
				NULL 
				);

			String ^msgstr = gcnew String(msg);
			String ^lpMsgBufstr = gcnew String((LPTSTR)lpMsgBuf);
			System::String ^str = msgstr+": "+lpMsgBufstr;
			listener->pairingConsole(str);

			//_tprintf(_T("%s: %s"), msg, lpMsgBuf);

			LocalFree(lpMsgBuf);

			return dw;
		}


		_TCHAR * FormatBTAddress(BLUETOOTH_ADDRESS address)
		{
			static _TCHAR ret[20];
			_stprintf(ret, _T("%02x:%02x:%02x:%02x:%02x:%02x"),
				address.rgBytes[5],
				address.rgBytes[4],
				address.rgBytes[3],
				address.rgBytes[2],
				address.rgBytes[1],
				address.rgBytes[0]
			);
			return ret;
		}

		

	public:

		void addListener(WiiPairListener ^listener) {
			this->listener = listener;
		}

		void stop() {
			killme = true;
			cancelled = true;
		}

		void start(bool removeMode, int stopat)
		{
			cancelled = false;
			killme = false;
			WiiPairReport ^report = gcnew WiiPairReport();
			HANDLE hRadios[256];
			int nRadios;
			int nPaired = 0;

			listener->onPairingStarted();

			report->removeMode = removeMode;

			report->deviceNames = gcnew array<String^>(10);

			///////////////////////////////////////////////////////////////////////
			// Enumerate BT radios
			///////////////////////////////////////////////////////////////////////
			{
				HBLUETOOTH_RADIO_FIND hFindRadio;
				BLUETOOTH_FIND_RADIO_PARAMS radioParam;

				listener->pairingConsole("Enumerating radios...\n");

				radioParam.dwSize = sizeof(BLUETOOTH_FIND_RADIO_PARAMS);

				nRadios = 0;
				hFindRadio = BluetoothFindFirstRadio(&radioParam, &hRadios[nRadios++]);
				if (hFindRadio)
				{
					while (BluetoothFindNextRadio(&radioParam, &hRadios[nRadios++]));
					BluetoothFindRadioClose(hFindRadio);
				}
				else
				{
					ShowErrorCode(_T("Error enumerating radios"), GetLastError());
					listener->pairingMessage("Could not find any bluetooth devices",WiiPairListener::MessageType::ERR);
					report->status = WiiPairReport::Status::EXCEPTION;
					listener->onPairingProgress(report);
					return;
				}
				nRadios--;

				System::String^ str = "Found " + nRadios + " radios\n"; 
				listener->pairingConsole(str);
			}

			///////////////////////////////////////////////////////////////////////
			// Keep looping until we pair with a Wii device
			///////////////////////////////////////////////////////////////////////

			while (!killme)
			{
				 //If this run is just to remove all previous connections, just loop once.
				
				/*
				if(killme) {
					listener->onPairingCancelled();
					return;
				}
				*/

				int radio;

				for (radio = 0; radio < nRadios; radio++)
				{
					BLUETOOTH_RADIO_INFO radioInfo;
					HBLUETOOTH_DEVICE_FIND hFind;
					BLUETOOTH_DEVICE_INFO btdi;
					BLUETOOTH_DEVICE_SEARCH_PARAMS srch;

					radioInfo.dwSize = sizeof(radioInfo);
					btdi.dwSize = sizeof(btdi);
					srch.dwSize = sizeof(BLUETOOTH_DEVICE_SEARCH_PARAMS);

					ShowErrorCode(_T("BluetoothGetRadioInfo"), BluetoothGetRadioInfo(hRadios[radio], &radioInfo));

					System::String^ szNamestr = gcnew System::String(radioInfo.szName);
					System::String^ addressstr = gcnew System::String(FormatBTAddress(radioInfo.address));
					System::String^ str = "Radio " + radio + ": " + szNamestr + " " + addressstr + "\n";
					listener->pairingConsole(str);

					srch.fReturnAuthenticated = TRUE;
					srch.fReturnRemembered = TRUE;
					srch.fReturnConnected = TRUE;
					srch.fReturnUnknown = TRUE;
					srch.fIssueInquiry = TRUE;
					srch.cTimeoutMultiplier = 2;
					srch.hRadio = hRadios[radio];

					listener->pairingConsole("Scanning...\n");
					if(removeMode)
					{
						listener->pairingMessage("Removing old connections...",WiiPairListener::MessageType::INFO);
					}
					else
					{
						listener->pairingMessage("Scanning...",WiiPairListener::MessageType::INFO);
					}

					hFind = BluetoothFindFirstDevice(&srch, &btdi);

					if (hFind == NULL)
					{
						if (GetLastError() == ERROR_NO_MORE_ITEMS)
						{
							listener->pairingConsole("No bluetooth devices found.\n");
							if(removeMode)
							{
								killme = true;
							}
						}
						else
						{
							listener->pairingMessage("Could not find any bluetooth devices",WiiPairListener::MessageType::ERR);
							ShowErrorCode(_T("Error enumerating devices"), GetLastError());
							report->status = WiiPairReport::Status::EXCEPTION;
							listener->onPairingProgress(report);
							return;
						}
					}
					else
					{
						do
						{
							String ^szName = gcnew String(btdi.szName);
							System::String ^str = "Found:"+szName+"\n";
							listener->pairingConsole(str);

							if (!wcscmp(btdi.szName, L"Nintendo RVL-CNT-01-TR") || !wcscmp(btdi.szName, L"Nintendo RVL-CNT-01"))
							{
								WCHAR pass[6];
								DWORD pcServices = 16;
								GUID guids[16];
								BOOL error = FALSE;

								if (!error)
								{
									if (btdi.fRemembered && removeMode)
									{
										listener->pairingMessage("Removing old Wiimote",WiiPairListener::MessageType::SUCCESS);
										// Make Windows forget pairing
										if (ShowErrorCode(_T("BluetoothRemoveDevice"), BluetoothRemoveDevice(&btdi.Address)) != ERROR_SUCCESS)
										{
											listener->pairingMessage("Could not remove device",WiiPairListener::MessageType::ERR);
										}
										else if(removeMode)
										{
											error = TRUE;
										}
									} else if(btdi.fRemembered || removeMode) {
										error = TRUE;
									} else {
										listener->pairingMessage("Found a new Wiimote",WiiPairListener::MessageType::SUCCESS);
									}
								}

								// MAC address is passphrase
								pass[0] = radioInfo.address.rgBytes[0];
								pass[1] = radioInfo.address.rgBytes[1];
								pass[2] = radioInfo.address.rgBytes[2];
								pass[3] = radioInfo.address.rgBytes[3];
								pass[4] = radioInfo.address.rgBytes[4];
								pass[5] = radioInfo.address.rgBytes[5];

								if (!error && !removeMode)
								{
									//BluetoothRegisterForAuthenticationEx(&btdi,NULL,(PFN_AUTHENTICATION_CALLBACK_EX)OnAuthenticate,NULL);
									// Pair with Wii device
									DWORD error = BluetoothAuthenticateDevice(NULL, hRadios[radio], &btdi, pass, 6);
									if (ShowErrorCode(_T("BluetoothAuthenticateDevice"), error) != ERROR_SUCCESS) {
									//if (ShowErrorCode(_T("BluetoothAuthenticateDevice"), BluetoothAuthenticateDeviceEx(NULL, hRadios[radio], &btdi, NULL,MITMProtectionNotDefined)) != ERROR_SUCCESS) {
										//error = TRUE;
										if(error != ERROR_NO_MORE_ITEMS) {
											//listener->pairingMessage("Could not authenticate",WiiPairListener::MessageType::ERR);
										}
									} else {
										listener->pairingMessage("Authenticated",WiiPairListener::MessageType::SUCCESS);
									}
								}

								if (!error && !removeMode)
								{
									Sleep(100);
									// If this is not done, the Wii device will not remember the pairing
									if (ShowErrorCode(_T("BluetoothEnumerateInstalledServices"), BluetoothEnumerateInstalledServices(hRadios[radio], &btdi, &pcServices, guids)) != ERROR_SUCCESS) {
										error = TRUE;
										listener->pairingMessage("Could not permanently pair the Wiimote",WiiPairListener::MessageType::ERR);
									} else {
										listener->pairingMessage("Paired",WiiPairListener::MessageType::SUCCESS);
									}
								}
								
								if (!error && !removeMode)
								{
									Sleep(100);
									// Activate service
									if (ShowErrorCode(_T("BluetoothSetServiceState"), BluetoothSetServiceState (hRadios[radio], &btdi, &HumanInterfaceDeviceServiceClass_UUID, BLUETOOTH_SERVICE_ENABLE )) != ERROR_SUCCESS) {
										error = TRUE;
										listener->pairingMessage("Could not activate",WiiPairListener::MessageType::ERR);
									} else {
										listener->pairingMessage("Activated",WiiPairListener::MessageType::SUCCESS);
									}
								}

								if (!error)
								{
									report->deviceNames[nPaired] = (gcnew System::String(btdi.szName));
									nPaired++;
									report->numberPaired = nPaired;
									report->status = WiiPairReport::Status::RUNNING;
									listener->onPairingProgress(report);
									if(nPaired >= stopat) {
										killme = true;
									}
								}
							} // if (!wcscmp(btdi.szName, L"Nintendo RVL-WBC-01") || !wcscmp(btdi.szName, L"Nintendo RVL-CNT-01"))
						}
						while (BluetoothFindNextDevice(hFind, &btdi));
						
						if(removeMode)
						{
							killme = true;
						}
					} // if (hFind == NULL)
				} // for (radio = 0; radio < nRadios; radio++)

			}

			///////////////////////////////////////////////////////////////////////
			// Clean up
			///////////////////////////////////////////////////////////////////////

			{
				int radio;

				for (radio = 0; radio < nRadios; radio++)
				{
					CloseHandle(hRadios[radio]);
				}
			}

			listener->pairingConsole("=============================================\n");
			System::String^ str =  nPaired + " Wii devices paired\n";
			listener->pairingConsole(str);

			if(cancelled)
			{
				report->status = WiiPairReport::Status::CANCELLED;
			}
			else
			{
				report->status = WiiPairReport::Status::DONE;
			}
			listener->onPairingProgress(report);

			return;
		}
	};


	//
	//
	// Following is based on
	// https://github.com/Ciantic/monitortoggler/blob/master/src/restoremonitors7.c
	//
	public ref class MonitorInfo
	{
	public:
		String^ DevicePath;
		String^ FriendlyName;
	};

	public ref class Monitors
	{
	public:
		/*
		Gets GDI Device name from Source (e.g. \\.\DISPLAY4).
		*/
		static String^ getGDIDeviceNameFromSource(LUID adapterId, UINT32 sourceId) {
			DISPLAYCONFIG_SOURCE_DEVICE_NAME deviceName;
			DISPLAYCONFIG_DEVICE_INFO_HEADER header;
			header.size = sizeof(DISPLAYCONFIG_SOURCE_DEVICE_NAME);
			header.adapterId = adapterId;
			header.id = sourceId;
			header.type = DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME;
			deviceName.header = header;
			DisplayConfigGetDeviceInfo((DISPLAYCONFIG_DEVICE_INFO_HEADER*)&deviceName);
			return gcnew System::String(deviceName.viewGdiDeviceName);
		}

		/*
		Gets Device Path from Target
		e.g. \\?\DISPLAY#SAM0304#5&9a89472&0&UID33554704#{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}
		*/
		static String^ getMonitorDevicePathFromTarget(LUID adapterId, UINT32 targetId) {
			DISPLAYCONFIG_TARGET_DEVICE_NAME deviceName;
			DISPLAYCONFIG_DEVICE_INFO_HEADER header;
			header.size = sizeof(DISPLAYCONFIG_TARGET_DEVICE_NAME);
			header.adapterId = adapterId;
			header.id = targetId;
			header.type = DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME;
			deviceName.header = header;
			DisplayConfigGetDeviceInfo((DISPLAYCONFIG_DEVICE_INFO_HEADER*)&deviceName);
			return gcnew System::String(deviceName.monitorDevicePath);
		}


		/*
		Gets Friendly name from Target (e.g. "SyncMaster")
		*/
		static String^ getFriendlyNameFromTarget(LUID adapterId, UINT32 targetId) {
			DISPLAYCONFIG_TARGET_DEVICE_NAME deviceName;
			DISPLAYCONFIG_DEVICE_INFO_HEADER header;
			header.size = sizeof(DISPLAYCONFIG_TARGET_DEVICE_NAME);
			header.adapterId = adapterId;
			header.id = targetId;
			header.type = DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME;
			deviceName.header = header;
			DisplayConfigGetDeviceInfo((DISPLAYCONFIG_DEVICE_INFO_HEADER*)&deviceName);
			return gcnew System::String(deviceName.monitorFriendlyDeviceName);
		}

		static array<MonitorInfo^>^ enumerateMonitors(){

			int numMonitors = 0;
			int curMonitor = 0;

			UINT32 num_of_paths = 0;
			UINT32 num_of_modes = 0;
			DISPLAYCONFIG_PATH_INFO* displayPaths = NULL;
			DISPLAYCONFIG_MODE_INFO* displayModes = NULL;

			UINT32 num_of_paths2 = 0;
			UINT32 num_of_modes2 = 0;
			DISPLAYCONFIG_PATH_INFO* displayPaths2 = NULL;
			DISPLAYCONFIG_MODE_INFO* displayModes2 = NULL;

			GetDisplayConfigBufferSizes(QDC_ALL_PATHS, &num_of_paths, &num_of_modes);


			// Allocate paths and modes dynamically
			displayPaths = (DISPLAYCONFIG_PATH_INFO*)calloc((int)num_of_paths, sizeof(DISPLAYCONFIG_PATH_INFO));
			displayModes = (DISPLAYCONFIG_MODE_INFO*)calloc((int)num_of_modes, sizeof(DISPLAYCONFIG_MODE_INFO));

			// Query for the information 
			QueryDisplayConfig(QDC_ALL_PATHS, &num_of_paths, displayPaths, &num_of_modes, displayModes, NULL);

			//Count how many monitors...
			for (int i = 0; i < num_of_modes; i++) {

				switch (displayModes[i].infoType) {
				case DISPLAYCONFIG_MODE_INFO_TYPE_SOURCE:
					break;

				case DISPLAYCONFIG_MODE_INFO_TYPE_TARGET:
					numMonitors++;
					break;

				default:
					fputs("error", stderr);
					break;
				}
			}

			array<MonitorInfo^>^ monitors = gcnew array<MonitorInfo^>(numMonitors);

			for (int i = 0; i < num_of_modes; i++) {

				MonitorInfo^ newMonitorInfo = gcnew MonitorInfo();

				switch (displayModes[i].infoType) {

				case DISPLAYCONFIG_MODE_INFO_TYPE_SOURCE:
					break;

				case DISPLAYCONFIG_MODE_INFO_TYPE_TARGET:
					newMonitorInfo->DevicePath = getMonitorDevicePathFromTarget(displayModes[i].adapterId, displayModes[i].id);
					newMonitorInfo->FriendlyName = getFriendlyNameFromTarget(displayModes[i].adapterId, displayModes[i].id);
					monitors[curMonitor++] = newMonitorInfo;
					break;

				default:
					fputs("error", stderr);
					break;
				}
			}

			free(displayPaths);
			free(displayModes);
			return monitors;
		}
	};

}
