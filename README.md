# MsIotApiWrapper
MsIotApiWrapper is a library that binds to the (still unofficial?) ms-iot REST API which is serviced on small IoT devices like the Raspberry Pi running Windows 10 IoT.
The library provides several methods to interact with the device via web. For a detailed description see the [Wiki-Pages](https://github.com/siochs/MsIotApiWrapper/wiki). A typical use case could be an application that consumes the library in order to install a packaged UWP app on your Raspberry Pi running Windows 10 IoT Core...
* without Visual Studio deploy,
* without Azure (if your device cannot be connected to the internet),
* without WinAppDeployCmd.exe and the Windows 10 SDK
* without Windows 10 Store (if your device cannot be connected to the internet)

In the Samples you find a small command line application demonstrating the capabilities of the library. See also ::::
However, this is AFAIK **not the supported way**.

## Licensing
See https://github.com/siochs/MsIotApiWrapper/blob/master/LICENSE
## Dependencies & Build
Donwload the solution and build with Visual Studio 2015 CE or higher.
### MsIotApiWrapper library
The library requires the [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json) nuget package.
### WinIotCtl sample
The sample cli application requires the [CommandLineParser](https://github.com/gsscoder/commandline) **stable** nuget package.
## How to use the library
You have two options:
1) Build the library, place the MsIotApiWrapper.dll into your project and add a reference to is.
2) Clone the project's source into your project tree and use it as a part of your solution.

You can consume the library as following:
```csharp
var msIotApiWrapper = new MsIotApiWrapper("169.254.223.252", "Administrator", "p@ssw0rd");
msIotApiWrapper.SideloadAppxPackage("AppName_Version_Architecture.appx");
msIotApiWrapper.RebootTarget();
...
```
If you wish to use your own HttpMessageHandler you can do so by specifying that in the constructor. Otherwise the standard System.Net.Http.HttpClient will be used for the transport over web.
```csharp
var msIotApiWrapper = new MsIotApiWrapper("169.254.223.252", "Administrator", "p@ssw0rd", myHttpMessageHandler);
```
For more details visit the code documentation pages in the [Wiki](https://github.com/siochs/MsIotApiWrapper/wiki)
## How to test the library
The library is subjected to [NUnit3](http://nunit.org/) unit tests. You need to install according extensions to your IDE.
A mock HttpMessageHandler which emulates the behaviour of the ms-iot REST API is provided for unit testing.
## How to use the sample application
Call WinIotCtl.exe from the command line to see the usage. Commands available so far:
* list packages installed on the device
* set the default startup app
* reboot the device
* sideload and install an uwp package

For sideloading place all (ARM) .appx **apps** files into the exe's root. Place all (ARM) .appx **dependency** files into a subfolder called "Dependencies". The following example will scan the apps and dependencies, tries to remove old versions from the devie, tries to sideload the found packages, sets the MySideloadedApp as startup and reboots the device.
```sh
$ WinIotAppxUpdater.exe --ip4 169.254.223.252 --username Administrator --password p@ssw0rd --sideload --startup MySideloadedApp --reboot
```
