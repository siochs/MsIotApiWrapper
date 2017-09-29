// Copyright (c) 2017 Simon Graf, see License in the repository root.
using CommandLine;
using System.Text;

namespace WinIotCtl
{
    /// <summary>
    /// Represents a store of command line options.
    /// </summary>
    class CliOptions
    {
        //[Option('4', "ip4", Required = true, HelpText = "Target Ipv4")] // 4 is not working due to a bug
        [Option("ip4", Required = true, HelpText = "Target Ipv4")] // 4 is not working due to a bug
        public string Ipv4 { get; set; }

        [Option('u', "username", Required = true, HelpText = "API Username")]
        public string Username { get; set; }

        [Option('p', "password", Required = true, HelpText = "API Password")]
        public string Password { get; set; }

        [Option('l', "sideload", Required = false, HelpText = "Sideload appx files")]
        public bool Sideload { get; set; }

        [Option('s', "startup", Required = false, HelpText = "Set default app")]
        public string Startup { get; set; }

        [Option('r', "reboot", Required = false, HelpText = "Reboot device")]
        public bool Reboot { get; set; }

        [Option('n', "ls", Required = false, HelpText = "List installed apps")]
        public bool ListsApps { get; set; }

        [Option('t', "timeout", Required = false, HelpText = "The timeout to test for successfull deployment of the app")]
        public byte SuccessTestTimeout { get; set; } = 5; // 5 minutes default

        [HelpOption]
        public string GetUsage()
        {
            StringBuilder Usage = new StringBuilder();
            Usage.AppendLine("Usage:");
            Usage.AppendLine(System.AppDomain.CurrentDomain.FriendlyName + " [Options]");
            Usage.AppendLine("   | --ip4 \t\t <IPv4 Address> \t Required. Target IPv4 Address.");
            Usage.AppendLine("-u | --username \t <Username> \t\t Required. API Username.");
            Usage.AppendLine("-p | --password \t <Password> \t\t Required. API Password.");
            Usage.AppendLine("-n | --ls \t\t\t\t\t Optional. Lists installed apps.");
            Usage.AppendLine("-l | --sideload \t\t\t\t Optional. Scans for appx dependencies/apps and sideloads them.");
            Usage.AppendLine("-t | --timeout \t\t <Minutes> \t\t Optional. The timeout to test for successfull deployment of the app (default 5').");
            Usage.AppendLine("-s | --startup \t\t <Appname> \t\t Optional. Sets the startup app.");
            Usage.AppendLine("-r | --reboot \t\t\t\t\t Optional. Reboots the device.");
            Usage.AppendLine("");
            Usage.AppendLine("Example: " + System.AppDomain.CurrentDomain.FriendlyName + " --ip4 169.254.223.252 --username Administrator --password p@ssw0rd --sideload --startup MySideloadedApp --reboot");
            return Usage.ToString();   
        }
    }
}
