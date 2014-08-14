// WiiCPP.h

#pragma once

#include "stdafx.h"

#include <tchar.h>
#include <windows.h>
#include <bthsdpdef.h>
#include <bthdef.h>
#include <BluetoothAPIs.h>
#include <strsafe.h>
#include <vcclr.h>
#include <map>

#include "ToshibaHack.h"

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
		int ToshibaState;
		int ToshibaConnectedCount;

		WiiPairListener ^listener;

		HWND windowHandle;

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

		_TCHAR * FormatToshibaBTAddress(TToshibaBluetoothAddress address)
		{
			static _TCHAR ret[20];
			_stprintf(ret, _T("%02x:%02x:%02x:%02x:%02x:%02x"),
				address[0],
				address[1],
				address[2],
				address[3],
				address[4],
				address[5]
				);
			return ret;
		}


	public:
		WiiPair(IntPtr handle){
			this->windowHandle = (HWND)handle.ToPointer();
			LoadToshibaBluetoothStack(windowHandle);
		}
		
		bool ToshibaBluetoothIsWiiController(TToshibaBluetoothDevice &Device) {
			// This matches the following:
			// Nintendo RVL-CNT-01    = Wii Remote / Wii Remote Plus / Interworks Pro Controller U
			// Nintendo RVL-CNT-01-TR = Wii Remote Plus TR
			// Nintendo RVL-WBC-01    = Wii Fit Balance Board
			// Nintendo RVL-CNT-01-UC = Wii U Pro Controller
			String^ name = gcnew String(Device.name);
			if (name->Length > 12)
			{
				return (name->ToUpper()->Substring(0, 13)->Equals("NINTENDO RVL-"));
			}
			return false;
		}

		void StartPairingWiimote(int i) {
			int error;
			ToshibaState = 3;
			listener->pairingMessage("Found a new Wiimote (Toshiba)", WiiPairListener::MessageType::SUCCESS);
			listener->pairingConsole("Pairing " + i + ": '" + gcnew String(pDeviceList->device[i].name) + "' " + gcnew String(FormatToshibaBTAddress(pDeviceList->device[i].BluetoothAddress)));
			//if (ToshibaBluetoothClearPIN)
			//	ToshibaBluetoothClearPIN(pDeviceList->device[i].BluetoothAddress, error);
			//ToshibaBluetoothNotify(0, error, windowHandle, WM_TOSHIBA_BLUETOOTH);
			if (ToshibaBluetoothConnectHID)
			  ToshibaBluetoothConnectHID(&(pDeviceList->device[i].BluetoothAddress[0]), error, windowHandle, WM_TOSHIBA_BLUETOOTH, i);
			//ToshibaBluetoothConnectHID(NULL, error, 0, WM_TOSHIBA_BLUETOOTH, 7);
			System::Threading::Thread::Sleep(2000);
			
		}

		void ConnectToshibaWiimotes() {
			if (!pDeviceList) return;
			if (pDeviceList->deviceCount == 0) {
				listener->pairingConsole("No bluetooth devices found.\n");
			}
			for (int i = 0; i < pDeviceList->deviceCount; i++) {
				String ^szName = gcnew String(pDeviceList->device[i].name);
				System::String ^str = "Found:" + szName + "\n";
				listener->pairingConsole(str);
				if (ToshibaBluetoothIsWiiController(pDeviceList->device[i]))
					StartPairingWiimote(i);
			}
			if (ToshibaState == 2)
				ToshibaState = 0;
		}

		void ToshibaBluetoothMessage(int wParam, int lParam) {
			//if (!listener) {
				switch (wParam) {
				case TOSHIBA_BLUETOOTH_SEARCH_FINISHED:
				case TOSHIBA_BLUETOOTH_SEARCH_ERROR:
					ToshibaState = 2;
					break;
				case TOSHIBA_BLUETOOTH_CONNECT_HID_CONNECTED:
					if (ToshibaState == 3) ToshibaConnectedCount++;
					break;
				case TOSHIBA_BLUETOOTH_CONNECT_HID_FINISHED:
					ToshibaState = 4;
					break;
				}
			//}
			switch (wParam) {
			case TOSHIBA_BLUETOOTH_SEARCH_STARTING:
					listener->pairingConsole("Starting Toshiba search.\n");
					break;
			case TOSHIBA_BLUETOOTH_SEARCH_FINISHED:
					listener->pairingConsole("Finnished Toshiba search.\n");
					break;
			case TOSHIBA_BLUETOOTH_SEARCH_ERROR:
					listener->pairingConsole("ERROR in Toshiba search.\n");
					break;
			case TOSHIBA_BLUETOOTH_SEARCH_FOUND:
					listener->pairingConsole("FOUND in Toshiba search.\n");
					break;

			case TOSHIBA_BLUETOOTH_CONNECT_HID_ERROR:
					listener->pairingConsole("ERROR in Toshiba HID connection.\n");
					break;
			case TOSHIBA_BLUETOOTH_CONNECT_HID_STARTING:
					listener->pairingConsole("Started Toshiba HID connection.\n");
					break;
			case TOSHIBA_BLUETOOTH_CONNECT_HID_CONNECTED:
					listener->pairingConsole("Connected Toshiba HID connection.\n");
					break;
			case TOSHIBA_BLUETOOTH_CONNECT_HID_FINISHED:
					listener->pairingConsole("Finished Toshiba HID connection.\n");
					break;

			case TOSHIBA_BLUETOOTH_CONNECTION_CHANGED:
					if (lParam == 2) listener->pairingConsole("[Bluetooth] Toshiba connected?\n");
					else if (lParam == 1) listener->pairingConsole("[Bluetooth] Toshiba disconnected?\n");
					else listener->pairingConsole("Bluetooth connection changed = " + lParam);
					break;

			case TOSHIBA_BLUETOOTH_UNKNOWN:
					listener->pairingConsole("Unknown Toshiba Bluetooth message" + lParam + "\n");
					break;

				default:
					listener->pairingConsole("Unknown message " + wParam);
					break;
			}
		}

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
			int nMicrosoftRadios, nToshibaRadios;
			int nPaired = 0;

			ToshibaState = 0;
			ToshibaConnectedCount = 0;

			listener->onPairingStarted();

			report->removeMode = removeMode;

			report->deviceNames = gcnew array<String^>(10);

			do
			{
				///////////////////////////////////////////////////////////////////////
				// Enumerate BT radios
				///////////////////////////////////////////////////////////////////////
				{
					HBLUETOOTH_RADIO_FIND hFindRadio;
					BLUETOOTH_FIND_RADIO_PARAMS radioParam;

					listener->pairingConsole("Enumerating radios...\n");
					if (hasToshibaAdapter) {
						nToshibaRadios = 1;
						System::String^ str = "Found 1 bluetooth adapter using Toshiba stack\n";
						listener->pairingConsole(str);
						System::String^ addressstr = gcnew System::String(FormatToshibaBTAddress(ToshibaBluetoothAdapterAddr));
						str = "Toshiba Bluetooth adapter 0:  " + addressstr + "\n";
						listener->pairingConsole(str);
					}

					radioParam.dwSize = sizeof(BLUETOOTH_FIND_RADIO_PARAMS);

					nMicrosoftRadios = 0;
					hFindRadio = BluetoothFindFirstRadio(&radioParam, &hRadios[nMicrosoftRadios++]);
					if (hFindRadio)
					{
						while (BluetoothFindNextRadio(&radioParam, &hRadios[nMicrosoftRadios++]));
						BluetoothFindRadioClose(hFindRadio);
					}
					else
					{
						DWORD lastError = GetLastError();
						ShowErrorCode(_T("Error enumerating radios"), lastError);
						listener->pairingMessage("Couldn't find any bluetooth adapters\nusing Microsoft stack",WiiPairListener::MessageType::INFO);
						
						// It's not a problem if there are no Microsoft adapters (because we can use Toshiba ones) unless we are in removeMode.
						if (GetLastError() == ERROR_NO_MORE_ITEMS && !removeMode)
						{
							report->status = WiiPairReport::Status::RUNNING;
							listener->onPairingProgress(report);
						}
						else
						{
							report->status = WiiPairReport::Status::EXCEPTION;
							listener->onPairingProgress(report);
						}						
					}
					nMicrosoftRadios--;

					System::String^ str = "Found " + nMicrosoftRadios + " bluetooth adapters using Microsoft stack\n"; 
					listener->pairingConsole(str);
					if (nMicrosoftRadios + nToshibaRadios == 0) {
						listener->pairingMessage("Couldn't find any bluetooth adapters using Microsoft or Toshiba stacks", WiiPairListener::MessageType::ERR);
						report->status = WiiPairReport::Status::EXCEPTION;
						listener->onPairingProgress(report);
						return;
					}
				}

				///////////////////////////////////////////////////////////////////////
				// Keep looping until we pair with a Wii device
				///////////////////////////////////////////////////////////////////////

				do
				{
					 //If this run is just to remove all previous connections, just loop once.
				
					/*
					if(killme) {
						listener->onPairingCancelled();
						return;
					}
					*/

					int radio;

					if (nToshibaRadios > 0) {
						if (ToshibaState == 2) { // Connecting
							listener->pairingConsole("State=" + ToshibaState + "\n");
							ConnectToshibaWiimotes();
							System::Threading::Thread::Sleep(500);
						}

						if (ToshibaState == 0) { // Ready
							listener->pairingConsole("State=0\n");
							int error;
							BOOL result;
							error = 0;
							listener->pairingConsole("Scanning (Toshiba)...\n");
							// notify us about everything (this is probably unneccessary)
							//result = ToshibaBluetoothNotify(0xFFFFFFFF, error, windowHandle, WM_TOSHIBA_BLUETOOTH);
							ToshibaState = 1; // Scanning
							result = ToshibaBluetoothStartSearching(pDeviceList, 0, error, windowHandle, WM_TOSHIBA_BLUETOOTH, 57);
							System::Threading::Thread::Sleep(500);
							for (radio = 0; radio < nMicrosoftRadios; radio++) {

							}
							listener->pairingMessage("Scanning (Toshiba)...", WiiPairListener::MessageType::INFO);
						} else if (ToshibaState == 4) { // finished connecting
							listener->pairingConsole("State=4\n");

							//report->deviceNames[nPaired] = (gcnew System::String("test"));
							nPaired += ToshibaConnectedCount;
							report->numberPaired = nPaired;
							report->status = WiiPairReport::Status::RUNNING;
							listener->onPairingProgress(report);
							if (nPaired >= stopat) {
								killme = true;
							}
							ToshibaState = 0;
							ToshibaConnectedCount = 0;
						} else {
							listener->pairingConsole("State="+ToshibaState+"\n");
							System::Threading::Thread::Sleep(500);
						}
					}

					for (radio = 0; radio < nMicrosoftRadios; radio++)
					{
						BLUETOOTH_RADIO_INFO radioInfo;
						HBLUETOOTH_DEVICE_FIND hFind;
						BLUETOOTH_DEVICE_INFO btdi;
						BLUETOOTH_DEVICE_SEARCH_PARAMS srch;

						radioInfo.dwSize = sizeof(radioInfo);
						btdi.dwSize = sizeof(btdi);
						srch.dwSize = sizeof(BLUETOOTH_DEVICE_SEARCH_PARAMS);

						Sleep(100);

						ShowErrorCode(_T("BluetoothGetRadioInfo"), BluetoothGetRadioInfo(hRadios[radio], &radioInfo));

						System::String^ szNamestr = gcnew System::String(radioInfo.szName);
						System::String^ addressstr = gcnew System::String(FormatBTAddress(radioInfo.address));
						System::String^ str = "Microsoft Bluetooth adapter " + radio + ": " + szNamestr + " " + addressstr + "\n";
						listener->pairingConsole(str);

						srch.fReturnAuthenticated = TRUE;
						srch.fReturnRemembered = TRUE;
						srch.fReturnConnected = TRUE;
						srch.fReturnUnknown = TRUE;
						srch.fIssueInquiry = TRUE;
						srch.cTimeoutMultiplier = 1;
						srch.hRadio = hRadios[radio];

						listener->pairingConsole("Scanning (Microsoft)...\n");
						if(removeMode)
						{
							listener->pairingMessage("Removing old connections...",WiiPairListener::MessageType::INFO);
						}
						else
						{
							listener->pairingMessage("Scanning (Microsoft)...",WiiPairListener::MessageType::INFO);
						}

						hFind = BluetoothFindFirstDevice(&srch, &btdi);

						if (hFind == NULL)
						{
							if (GetLastError() == ERROR_NO_MORE_ITEMS)
							{
								listener->pairingConsole("No bluetooth devices found.\n");
								if(removeMode)
								{
									killme = true; // I don't use removeMode with Toshiba.
								}
							}
							else
							{

								//listener->pairingMessage("The bluetooth device is acting funky",WiiPairListener::MessageType::ERR);
								ShowErrorCode(_T("Error enumerating devices"), GetLastError());
								//report->status = WiiPairReport::Status::RUNNING;
								//listener->onPairingProgress(report);
								//break;
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
										DWORD autherror = BluetoothAuthenticateDevice(NULL, hRadios[radio], &btdi, pass, 6);
										if (ShowErrorCode(_T("BluetoothAuthenticateDevice"), autherror) != ERROR_SUCCESS) {
										//if (ShowErrorCode(_T("BluetoothAuthenticateDevice"), BluetoothAuthenticateDeviceEx(NULL, hRadios[radio], &btdi, NULL,MITMProtectionNotDefined)) != ERROR_SUCCESS) {
											//error = TRUE;
											if (autherror != ERROR_NO_MORE_ITEMS) {
												//listener->pairingMessage("Could not authenticate",WiiPairListener::MessageType::ERR);
											}
											Sleep(400);
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

				} while (!killme);

			} while (!killme);
			///////////////////////////////////////////////////////////////////////
			// Clean up
			///////////////////////////////////////////////////////////////////////

			{
				int radio;

				for (radio = 0; radio < nMicrosoftRadios; radio++)
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

	//To be able to use LUID in map
	class luidComp
	{
	public:

		bool operator()(const LUID& l,
			const LUID& r)
			const
		{
			return (l.LowPart != r.LowPart) || (l.HighPart != r.HighPart);
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
		String^ DeviceName;
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

			GetDisplayConfigBufferSizes(QDC_ALL_PATHS, &num_of_paths, &num_of_modes);
			
			// Allocate paths and modes dynamically
			displayPaths = (DISPLAYCONFIG_PATH_INFO*)calloc((int)num_of_paths, sizeof(DISPLAYCONFIG_PATH_INFO));
			displayModes = (DISPLAYCONFIG_MODE_INFO*)calloc((int)num_of_modes, sizeof(DISPLAYCONFIG_MODE_INFO));

			// Query for the information 
			LONG result = QueryDisplayConfig(QDC_ALL_PATHS, &num_of_paths, displayPaths, &num_of_modes, displayModes, NULL);
			
			map<int, gcroot<MonitorInfo^>> monitormap;

			//display paths lists relationships between virtual desktop (source) -> physical monitors (target)
			//we will base the list on physical monitor ids
			//since there can only be either several monitors to one virtual desktop but never 
			//several virtual desktops to one monitor (as far as I can tell...)
			for (int i = 0; i < num_of_paths; i++)
			{
				if (displayPaths[i].flags & DISPLAYCONFIG_PATH_ACTIVE) //If the monitor is active
				{
					monitormap[displayPaths[i].targetInfo.id] = gcnew MonitorInfo();
					monitormap[displayPaths[i].targetInfo.id]->DeviceName = getGDIDeviceNameFromSource(displayPaths[i].sourceInfo.adapterId, displayPaths[i].sourceInfo.id);
					monitormap[displayPaths[i].targetInfo.id]->DevicePath = getMonitorDevicePathFromTarget(displayPaths[i].targetInfo.adapterId, displayPaths[i].targetInfo.id);
					monitormap[displayPaths[i].targetInfo.id]->FriendlyName = getFriendlyNameFromTarget(displayPaths[i].targetInfo.adapterId, displayPaths[i].targetInfo.id);
				}
			}

			array<MonitorInfo^>^ monitors = gcnew array<MonitorInfo^>(monitormap.size());
			
			for (map<int, gcroot<MonitorInfo^>>::iterator iter = monitormap.begin();
				iter != monitormap.end();
				++iter)
			{
				/*
				printf("\n id: %d", iter->first);
				printf("\n Devname: %s", iter->second->DeviceName);
				printf("\n friendly: %s", iter->second->FriendlyName);
				*/
				monitors[curMonitor++] = iter->second;
			}
			
			free(displayPaths);
			free(displayModes);
			return monitors;
		}
	};

}
