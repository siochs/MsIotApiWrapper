using NUnit.Framework;
using MsIotApiWrapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MsIotApiWrapperTests;
using System.IO;

namespace MsIotApiWrapper.Tests
{
    [TestFixture()]
    public class MsIotApiWrapperTests
    {
        [Test()]
        public void MsIotApiWrapperTest()
        {
            MsIotApiWrapper msIotApiWrapper = new MsIotApiWrapper("127.0.0.1", "user", "password");
            Assert.AreEqual(msIotApiWrapper.SideloadSuccessTimeoutMillisecs, 300000);

            msIotApiWrapper = new MsIotApiWrapper("127.0.0.1", "user", "password", 0);
            Assert.AreEqual(msIotApiWrapper.SideloadSuccessTimeoutMillisecs, 300000);

            msIotApiWrapper = new MsIotApiWrapper("127.0.0.1", "user", "password", 1);
            Assert.AreEqual(msIotApiWrapper.SideloadSuccessTimeoutMillisecs, 60000);
        }

        [Test()]
        public void GetInstalledAppsTest_WrongCredentials()
        {
            MsIotApiWrapper msIotApiWrapper = new MsIotApiWrapper("127.0.0.1", "user_", "password", new MockHttpMessageHandler());
            AggregateException exception = Assert.Throws<AggregateException>(() => msIotApiWrapper.GetInstalledApps());
            Assert.That(exception.InnerException.Message.Contains("401"));

            msIotApiWrapper = new MsIotApiWrapper("127.0.0.1", "user", "_password", new MockHttpMessageHandler());
            exception = Assert.Throws<AggregateException>(() => msIotApiWrapper.GetInstalledApps());
            Assert.That(exception.InnerException.Message.Contains("401"));
        }

        [Test()]
        public void GetInstalledAppsTest()
        {
            MsIotApiWrapper msIotApiWrapper = new MsIotApiWrapper("127.0.0.1", "user", "password", new MockHttpMessageHandler());
            List<AppxPackage> installedApps = msIotApiWrapper.GetInstalledApps();
            Assert.AreEqual(installedApps.Count, 4);
            Assert.AreEqual(installedApps[0].CanUninstall, true);
            Assert.AreEqual(installedApps[0].PackageFullName, "Microsoft.Windows.Cortana_1.8.12.15063_neutral_neutral_cw5n1h2txyewy");
            Assert.AreEqual(installedApps[0].PackageName, "Search");
            Assert.AreEqual(installedApps[0].PackageRelativeId, "Microsoft.Windows.Cortana_cw5n1h2txyewy!CortanaUI");
        }

        [Test()]
        public void RemoveAppxPackageFromTargetTest_NotRemovable()
        {
            MsIotApiWrapper msIotApiWrapper = new MsIotApiWrapper("127.0.0.1", "user", "password", new MockHttpMessageHandler());
            AppxPackage notRemovablePackage = new AppxPackage
            {
                CanUninstall = false,
                PackageFullName = "PackageFullName",
                PackageName = "AppName",
                PackageRelativeId = "PackageRelativeId"
            };

            AggregateException exception = Assert.Throws<AggregateException>(() => msIotApiWrapper.RemoveAppxPackageFromTarget(notRemovablePackage));
            Assert.That(exception.InnerException.Message.Contains("is marked as non-removable on the target"));

        }

        [Test()]
        public void RemoveAppxPackageFromTargetTest_NotExisting()
        {
            MsIotApiWrapper msIotApiWrapper = new MsIotApiWrapper("127.0.0.1", "user", "password", new MockHttpMessageHandler());
            AppxPackage notRemovablePackage = new AppxPackage
            {
                CanUninstall = true,
                PackageFullName = "DoesNotExist",
                PackageName = "AppName",
                PackageRelativeId = "PackageRelativeId"
            };

            AggregateException exception = Assert.Throws<AggregateException>(() => msIotApiWrapper.RemoveAppxPackageFromTarget(notRemovablePackage));
            Assert.That(exception.InnerException.Message.Contains("500"));
        }

        [Test()]
        public void RemoveAppxPackageFromTargetTest_Existing()
        {
            MsIotApiWrapper msIotApiWrapper = new MsIotApiWrapper("127.0.0.1", "user", "password", new MockHttpMessageHandler());
            AppxPackage removablePackage = new AppxPackage
            {
                CanUninstall = true,
                PackageFullName = "Exists",
                PackageName = "AppName",
                PackageRelativeId = "PackageRelativeId"
            };
            Assert.DoesNotThrow(() => msIotApiWrapper.RemoveAppxPackageFromTarget(removablePackage));
        }

        [Test()]
        public void SideloadAppxPackageTest_SideLoadWorkingFile()
        {
            // create a dummy test file
            string path = Path.GetTempPath() + "workingTestFile.appx";
            if (File.Exists(path)) File.Delete(path);
            using (FileStream fileStream = File.Create(path))
            {
                Byte[] content = new UTF8Encoding(true).GetBytes("Dummy content for test case.");
                fileStream.Write(content, 0, content.Length);
            }

            MsIotApiWrapper msIotApiWrapper = new MsIotApiWrapper("127.0.0.1", "user", "password", new MockHttpMessageHandler());
            msIotApiWrapper.SideloadSuccessPollIntervalMillisecs = 10;
            msIotApiWrapper.SideloadAppxPackage(path);
        }

        [Test()]
        public void SideloadAppxPackageTest_SideLoadNotWorkingFile()
        {
            // create a dummy test file
            string path = Path.GetTempPath() + "notWorkingTestFile.appx";
            if (File.Exists(path)) File.Delete(path);
            using (FileStream fileStream = File.Create(path))
            {
                Byte[] content = new UTF8Encoding(true).GetBytes("Dummy content for test case.");
                fileStream.Write(content, 0, content.Length);
            }

            MsIotApiWrapper msIotApiWrapper = new MsIotApiWrapper("127.0.0.1", "user", "password", new MockHttpMessageHandler());
            msIotApiWrapper.SideloadSuccessPollIntervalMillisecs = 10; // speed ups the poll a little
            AggregateException exception = Assert.Throws<AggregateException>(() => msIotApiWrapper.SideloadAppxPackage(path));
            Assert.That(exception.InnerException.Message.Contains("Uploaded file could not be processed"));
        }

        [Test()]
        public void SideloadAppxPackageTest_SideLoadTimesOut()
        {
            // create a dummy test file
            string path = Path.GetTempPath() + "timeoutTestFile.appx";
            if (File.Exists(path)) File.Delete(path);
            using (FileStream fileStream = File.Create(path))
            {
                Byte[] content = new UTF8Encoding(true).GetBytes("Dummy content for test case.");
                fileStream.Write(content, 0, content.Length);
            }

            MsIotApiWrapper msIotApiWrapper = new MsIotApiWrapper("127.0.0.1", "user", "password", new MockHttpMessageHandler());
            // make timeout occur earlier than normal
            msIotApiWrapper.SideloadSuccessPollIntervalMillisecs = 100; // polls every 100ms for success
            msIotApiWrapper.SideloadSuccessTimeoutMillisecs = 500; // timeout after 500ms => 5 requests
            AggregateException exception = Assert.Throws<AggregateException>(() => msIotApiWrapper.SideloadAppxPackage(path));
            Assert.That(exception.InnerException.Message.Contains("The operation timed out"));
        }

        [Test()]
        public void SetDefaultStartupAppTest_Existing()
        {
            MsIotApiWrapper msIotApiWrapper = new MsIotApiWrapper("127.0.0.1", "user", "password", new MockHttpMessageHandler());
            AppxPackage existingApp = new AppxPackage
            {
                CanUninstall = true,
                PackageFullName = "ExistingPackageFullName",
                PackageName = "ExistingAppName",
                PackageRelativeId = "ExistingPackageRelativeId"
            };
            Assert.DoesNotThrow(() => msIotApiWrapper.SetDefaultStartupApp(existingApp));
        }

        [Test()]
        public void SetDefaultStartupAppTest_NotExisting()
        {
            MsIotApiWrapper msIotApiWrapper = new MsIotApiWrapper("127.0.0.1", "user", "password", new MockHttpMessageHandler());
            AppxPackage existingApp = new AppxPackage
            {
                CanUninstall = true,
                PackageFullName = "NotExistingPackageFullName",
                PackageName = "NotExistingAppName",
                PackageRelativeId = "NotExistingPackageRelativeId"
            };
            AggregateException exception = Assert.Throws<AggregateException>(() => msIotApiWrapper.SetDefaultStartupApp(existingApp));
            Assert.That(exception.InnerException.Message.Contains("500"));
        }

        [Test()]
        public void RebootTargetTest()
        {
            MsIotApiWrapper msIotApiWrapper = new MsIotApiWrapper("127.0.0.1", "user", "password", new MockHttpMessageHandler());
            Assert.DoesNotThrow(() => msIotApiWrapper.RebootTarget());
        }
    }
}