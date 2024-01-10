
# GoPro_WiFi_Enabler

A minimal C# Windows Console app that connects to a GoPro camera via Bluetooth to enable the WiFi access point so you can connect to it from another computer witout needing a USB cable or using a SD Card reader.

## Description

GoPro cameras have inbuilt WiFi but in order to turn on the access point so other WiFi devices can connect to it you must enable it first via a Bluetooth LE command.

This app when run on a, Windows 11 machine, will look for Bluetooth devices and connect to the first GoPro camera it finds.

It will then attempt to pair with the device via Bluetooth if not already paired.

Once paired it will list the Services and Characteristics available (described in the link below for the OpenGoPro BLE 2.0 Specs).

It will print to the console the Username and Password to use to connect to the WiFi access point then it will enable the access point so you can connect to it using them.

This was built so I could avoid using the Quik phone app to connect to a GoPro camera in order to enable the WiFi access point on the camera via a Windows 11 machine.

I want to be able to connect wirelesly and transfer files from the GoPro cameras internal server straight to my PC using WinSCP. 

This is the most minimal code needed, have no real error handling and has only been tested minimally with a GoPro Hero 12 Black and Windows 11.

## Getting Started

### Dependencies

I had to add the Microsoft.Windows.SDK.Contracts Nuget package as per the link given below.

### Installing

Download the code and build in Visual Studio.

### Executing program

Either run via the debugger or build and run the built GoProWiFiEnabler.exe file.

Once it has paired and enabled the access point you can connect to the WiFi using the username and password.

In Windows you must connect to the locked 'Unknown' WiFi network then enter the name as the SSID.

Once connected to the GoPro's WiFi, the media server is at: http://10.5.5.9/videos/DCIM/100GOPRO/

In WinSCP you can connect to it using the WebDAV protocol and with that URL as the HostName on Port 80.
No username or password is required.

## Help

When run, this app will wake up the GoPro camera even when it is powered off.

Bluetooth pairing seems a little flakey.

Once the application run and pairs it should remain a paired device in Windows.

If you have issues pairing/connecting you may need to remove the device from the Windows Bluetooth devices list (under Settings|Bluetooth & devices).

Failing that, try turning it off and on again!

## Authors

Simon Jansen
asciimation@gmail.com

[github.com/Asciimation]https://github.com/Asciimation

## Version History

1.0.0.0 Initial version

## License

This project is licensed under the MIT License - see the LICENSE.md file for details

## Acknowledgments

* [Transfer Files from your GoPro to a PC or Mac using Wi-Fi](https://www.youtube.com/watch?v=5iLR-acPPvA)
* [Quick C# application to connect with a Bluetooth LE device](https://www.youtube.com/watch?v=CozmqN_iwNs)
* [OpenGoPro BLE 2.0 Specs](https://gopro.github.io/OpenGoPro/ble_2_0)
* [OpenGoPro C# Demo code repository](https://github.com/gopro/OpenGoPro/tree/main/demos/csharp/GoProCSharpSample)
* [Microsoft.Windows.SDK.Contracts](https://github.com/dotnet/windows-desktop/blob/main/docs/win10apis/README.md)
* [Microsoft GATT Client documentation](https://learn.microsoft.com/en-us/windows/uwp/devices-sensors/gatt-client)
