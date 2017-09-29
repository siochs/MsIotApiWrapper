// Copyright (c) 2017 Simon Graf, see License in the repository root.
namespace MsIotApiWrapper
{
    /// <summary>Represents a basic description of an installed package on the target.</summary>
    public class AppxPackage
    {
        /// <summary>
        /// The package full name like IoTCoreDefaultApp_1.0.1702.21000_arm__1w720vyc4ccym.
        /// </summary>
        public string PackageFullName { get; set; } = "";
        /// <summary>
        /// The package name like IoTCoreDefaultApp.
        /// </summary>
        public string PackageName { get; set; } = "";
        /// <summary>
        /// The package relative ID like IoTCoreDefaultApp_1w720vyc4ccym!App.
        /// </summary>
        public string PackageRelativeId { get; set; } = "";
        /// <summary>
        /// The package property if it is uninstallable.
        /// </summary>
        public bool CanUninstall { get; set; } = false;
    }
}
