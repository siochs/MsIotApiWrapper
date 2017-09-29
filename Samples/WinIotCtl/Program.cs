using MsIotApiWrapper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WinIotCtl
{
    class Program
    {
        static List<string> dependencyFiles = new List<string>();
        static List<string> appFiles = new List<string>();
        static List<AppxPackage> installedAppsOnTarget = new List<AppxPackage>();
        static MsIotApiWrapper.MsIotApiWrapper msIotApiWrapper;
        static Timer fakeProgressIndicatorTimer;

        /// <summary>
        /// Iterates recursively to the exception source and prints the accrding messages.
        /// </summary>
        /// <param name="exception">The exception to follow.</param>
        static void PrintInnerException(Exception exception)
        {
            if (exception.InnerException != null) PrintInnerException(exception.InnerException);
            Console.WriteLine("-->" + exception.Message);
        }

        /// <summary>
        /// Extracts the app's name from the .appx filename.
        /// </summary>
        /// <remarks>The .appx filename follows the syntax AppName_Version_Architecture.appx. '_' will be used as delimiter to obtain the app's name.</remarks>
        /// <param name="fileName">.appx filename having the syntax .\\AppName_Version_Architecture.appx</param>
        /// <returns>The app's name.</returns>
        static string GetAppNameFromFile(string fileName)
        {
            if (!fileName.Contains("_")) throw new Exception("GetAppNameFromFile: Cannot extract app name from file name. Does it follow the syntax \"AppName_Version_Architecture.appx\"?");
            string[] fileNameParts = fileName.Split('_');
            return fileNameParts[0].Remove(0, 2); // removes '.\\'
        }

        /// <summary>
        /// Removes an app from the device
        /// </summary>
        /// <remarks>An according package will be searched on the device. With the package identified the uninstall process will be triggered.</remarks>
        /// <param name="appName">The app's name which shall be removed.</param>
        static void RemoveAppIfExists(string appName)
        {
            // first, get the package via the app's name
            AppxPackage installedPackage = installedAppsOnTarget
                .Where(app => app.PackageName.Contains(appName))
                .FirstOrDefault();
            if (installedPackage != null)
            {
                // invoke uninstall
                Console.Write("A previous version of " + appName + " was found on the target. Removing " + installedPackage.PackageFullName + "...");
                msIotApiWrapper.RemoveAppxPackageFromTarget(installedPackage);
                Console.WriteLine(" Done.");
            }
        }

        /// <summary>
        /// Sideloads an .appx file and displays a fake progress indicator.
        /// </summary>
        /// <param name="fileName">The .appx file to sideload.</param>
        static void SideloadAppxFile(string fileName)
        {
            fakeProgressIndicatorTimer = new Timer((state) => Console.Write("."), null, 0, 2500); // todo: magic number. It's just a fake progress-indicator
            Console.Write("Sideloading file " + fileName + "...");
            msIotApiWrapper.SideloadAppxPackage(fileName);
            Console.WriteLine(" Done.");
            fakeProgressIndicatorTimer.Dispose();
        }

        /// <summary>
        /// Sets the app specified by its name as startup app.
        /// </summary>
        /// <remarks>An according package will be searched on the device. With the package identified the startup setup will be triggered.</remarks>
        /// <param name="appName">The app's name which shall be set as startup app.</param>
        static void SetDefaultStartupApp(string appName)
        {
            Console.Write("Default startup app specified: " + appName);
            installedAppsOnTarget = msIotApiWrapper.GetInstalledApps();
            AppxPackage StartupAppPackage = installedAppsOnTarget.Where(app => app.PackageName.Contains(appName)).FirstOrDefault();
            if (StartupAppPackage != null)
            {
                Console.Write(" having the ID " + StartupAppPackage.PackageRelativeId + ". Trying to register as default startup app... ");
                msIotApiWrapper.SetDefaultStartupApp(StartupAppPackage);
                Console.WriteLine(" Done.");
            }
            else
            {
                Console.WriteLine(". This app could not be found on the target.");
            }
        }

        /// <summary>
        /// Sideloads packages found in the current directory structure.
        /// </summary>
        /// <remarks>Dependencies will be searched in the Dependencies subfolder. Apps will be searched in the current folder. The apps-files follow the syntax AppName_Version_Architecture.appx</remarks>
        static void SideloadAppxPackages()
        {
            // get the dependencies and the app filenames
            dependencyFiles = Directory.GetFiles(".\\Dependencies", "*.appx", SearchOption.AllDirectories).ToList();
            appFiles = Directory.GetFiles(".", "*.appx", SearchOption.TopDirectoryOnly).ToList();
            installedAppsOnTarget = msIotApiWrapper.GetInstalledApps();

            // uninstall existing versions
            appFiles.ForEach((fileName) => RemoveAppIfExists(GetAppNameFromFile(fileName)));

            // sideloading files
            Console.WriteLine("Sideloading dependencies (times out after " + Convert.ToDecimal(msIotApiWrapper.SideloadSuccessTimeoutMillisecs / 60000).ToString("0.00") + " minutes each):");
            dependencyFiles.ForEach((fileName) => SideloadAppxFile(fileName.Remove(0, 2)));
            Console.WriteLine("Sideloading apps (times out after " + Convert.ToDecimal(msIotApiWrapper.SideloadSuccessTimeoutMillisecs / 60000).ToString("0.00") + " minutes each):");
            appFiles.ForEach((fileName) => SideloadAppxFile(fileName.Remove(0, 2)));
        }

        /// <summary>
        /// Shows a list of the installed packages on the device.
        /// </summary>
        static void ListPrintInstalledApps()
        {
            installedAppsOnTarget = msIotApiWrapper.GetInstalledApps();
            Console.WriteLine("The following packages are installed on the target:");
            installedAppsOnTarget.ForEach(app => Console.WriteLine("Uninstallabe: " + app.CanUninstall + "\t Package Name: \"" + app.PackageName + "\" => " + app.PackageFullName));
        }

        /// <summary>
        /// Requests the target to reboot.
        /// </summary>
        static void RebootTarget()
        {
            Console.Write("Try rebooting the target... ");
            msIotApiWrapper.RebootTarget();
            Console.WriteLine(" Done.");
        }

        static void Main(string[] args)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Reset();
            stopWatch.Start();
            Console.WriteLine(AppDomain.CurrentDomain.FriendlyName + " https://github.com/siochs");

            // try parse the cli options
            CliOptions cliOptions = new CliOptions();
            if (CommandLine.Parser.Default.ParseArguments(args, cliOptions))
            {
                // required arguments provided in the correct format. Setup a new connection and perform specified actions
                try
                {
                    msIotApiWrapper = new MsIotApiWrapper.MsIotApiWrapper(cliOptions.Ipv4, cliOptions.Username, cliOptions.Password, cliOptions.SuccessTestTimeout);
                    if (cliOptions.ListsApps) ListPrintInstalledApps();
                    if (cliOptions.Sideload) SideloadAppxPackages();
                    if (!String.IsNullOrEmpty(cliOptions.Startup)) SetDefaultStartupApp(cliOptions.Startup);
                    if (cliOptions.Reboot) RebootTarget();
                }
                catch (Exception ex)
                {
                    fakeProgressIndicatorTimer?.Dispose();
                    Console.WriteLine("");
                    Console.WriteLine("An error occured: ");
                    PrintInnerException(ex);
                }
            }
            else
            {
                // usage will be printed automatically if the command line args are not satisfactory. nothing to do here...
            }
            Console.WriteLine("Finished. The operation took " + Convert.ToDecimal(stopWatch.ElapsedMilliseconds / 60000M).ToString("0.000") + " Minutes.");
#if DEBUG
            Console.Read();
#endif
        }
    }
}
