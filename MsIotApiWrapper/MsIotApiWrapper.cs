// Copyright (c) 2017 Simon Graf, see License in the repository root.
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace MsIotApiWrapper
{
    /// <summary>
    /// Represents an enumeration of the available HTTP-Request methods.
    /// </summary>
    public enum HttpMethod : byte
    {
        /// <summary>
        /// POST method.
        /// </summary>
        POST,
        /// <summary>
        /// GET method.
        /// </summary>
        GET,
        /// <summary>
        /// DELETE method
        /// </summary>
        DELETE
    }

    /// <summary>
    /// Provides procedures to remotely control an endpoint running a ms-iot REST service.
    /// </summary>
    public class MsIotApiWrapper
    {
        private long _sideloadSuccessTimeoutMillisecs = 5 * 60 * 1000; // defaults to 5 minutes
        /// <summary>
        /// Gets or sets the timespan in milliseconds which is allowed to pass until the REST API returns from 204 -> 200
        /// </summary>
        public long SideloadSuccessTimeoutMillisecs
        {
            get
            {
                return this._sideloadSuccessTimeoutMillisecs;
            }
            set
            {
                if (value > 0) this._sideloadSuccessTimeoutMillisecs = value;
            }
        }

        private long _sideloadSuccessPollIntervalMillisecs = 5000; // defaults to 5 seconds
        /// <summary>
        /// Gets or sets the interval between polls for 200 when a method is waiting for the REST API to return from 204 -> 200
        /// </summary>
        public long SideloadSuccessPollIntervalMillisecs
        {
            get
            {
                return this._sideloadSuccessPollIntervalMillisecs;
            }
            set
            {
                if (value > 0) this._sideloadSuccessPollIntervalMillisecs = value;
            }
        }

        private string targetIpAddress = "";
        private string targetUserName = "";
        private string targetPassword = "";
        private string targetBaseUri = "";
        private AuthenticationHeaderValue authenticationHeaderValue;
        private HttpMessageHandler httpMessageHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="MsIotApiWrapper"/> class.
        /// </summary>
        /// <param name="targetIpAddress">The IP address of the target.</param>
        /// <param name="targetUserName">The username to access the API.</param>
        /// <param name="targetPassword">The password to access the API.</param>
        public MsIotApiWrapper(string targetIpAddress, string targetUserName, string targetPassword)
        {
            this.targetIpAddress = targetIpAddress;
            this.targetUserName = targetUserName;
            this.targetPassword = targetPassword;
            this.targetBaseUri = "http://" + targetIpAddress + ":8080";
            this.authenticationHeaderValue = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{targetUserName}:{targetPassword}")));            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MsIotApiWrapper"/> class.
        /// </summary>
        /// <param name="targetIpAddress">The IP address of the target.</param>
        /// <param name="targetUserName">The username to access the API.</param>
        /// <param name="targetPassword">The password to access the API.</param>
        /// <param name="successTestTimeoutMinutes">The timespan in minutes which is allowed to pass until a time consuming operation (like sideloading) times out.</param>
        public MsIotApiWrapper(string targetIpAddress, string targetUserName, string targetPassword, byte successTestTimeoutMinutes) : this(targetIpAddress, targetUserName, targetPassword)
        {
            this.SideloadSuccessTimeoutMillisecs = successTestTimeoutMinutes * 60 * 1000;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MsIotApiWrapper"/> class.
        /// </summary>
        /// <param name="targetIpAddress">The IP address of the target.</param>
        /// <param name="targetUserName">The username to access the API.</param>
        /// <param name="targetPassword">The password to access the API.</param>
        /// <param name="httpMessageHandler">The HttpMessageHandler which shall be used for transporting the REST API Calls. You may use a mock handler for unit testing.</param>
        public MsIotApiWrapper(string targetIpAddress, string targetUserName, string targetPassword, HttpMessageHandler httpMessageHandler) : this(targetIpAddress, targetUserName, targetPassword)
        {
            if (httpMessageHandler != null) this.httpMessageHandler = httpMessageHandler;
            else this.httpMessageHandler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MsIotApiWrapper"/> class.
        /// </summary>
        /// <param name="targetIpAddress">The IP address of the target.</param>
        /// <param name="targetUserName">The username to access the API.</param>
        /// <param name="targetPassword">The password to access the API.</param>
        /// <param name="httpMessageHandler">The HttpMessageHandler which shall be used for transporting the REST API Calls. You may use a mock handler for unit testing.</param>
        /// <param name="successTestTimeoutMinutes">The timespan in minutes which is allowed to pass until a time consuming operation (like sideloading) times out.</param>
        public MsIotApiWrapper(string targetIpAddress, string targetUserName, string targetPassword, HttpMessageHandler httpMessageHandler, byte successTestTimeoutMinutes) : this(targetIpAddress, targetUserName, targetPassword, successTestTimeoutMinutes)
        {
            if (httpMessageHandler != null) this.httpMessageHandler = httpMessageHandler;
            else this.httpMessageHandler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate };
        }

        /// <summary>
        /// Submits a HTTP request to the API endpoint.
        /// </summary>
        /// <remarks>The client tries HTTP-Basic-Auth credentials which were supplied when creating an instance of this class.</remarks>
        /// <param name="apiUri">The endpoint URL.</param>
        /// <param name="httpMethod">The <see cref="HttpMethod"/></param>
        /// <param name="content">The content which may be submitted by a POST request.</param>
        /// <returns>Returns the <see cref="HttpResponseMessage"/></returns>
        private async Task<HttpResponseMessage> AuthenticatedApiRequestAsync(string apiUri, HttpMethod httpMethod, object content = null)
        {
            using (HttpClient httpClient = new HttpClient( this.httpMessageHandler ))
            {
                httpClient.BaseAddress = new Uri(apiUri);
                httpClient.DefaultRequestHeaders.Authorization = this.authenticationHeaderValue;
                switch (httpMethod)
                {
                    case HttpMethod.GET:
                        return await httpClient.GetAsync("");
                    case HttpMethod.DELETE:
                        return await httpClient.DeleteAsync("");
                    case HttpMethod.POST:
                        if (content is StringContent) return await httpClient.PostAsync("", content as StringContent);
                        else
                        if (content is MultipartFormDataContent) return await httpClient.PostAsync("", content as MultipartFormDataContent);
                        else // other types could follow
                        return await httpClient.PostAsync("", null);
                    default:
                        return new HttpResponseMessage(HttpStatusCode.NotImplemented);
                }
            }
        }

        /// <summary>
        /// Gets a list of the installed packages on the target asynchronously.
        /// </summary>
        /// <returns>Returns a List of installed <see cref="AppxPackage"/>s</returns>
        /// <exception cref="Exception">The installed packages could not be queried.</exception>
        public async Task<List<AppxPackage>> GetInstalledAppsAsync()
        {
            List<AppxPackage> packageList = new List<AppxPackage>();
            HttpResponseMessage httpResponseMessage = await this.AuthenticatedApiRequestAsync(this.targetBaseUri + "/api/appx/packagemanager/packages", HttpMethod.GET);
            httpResponseMessage.EnsureSuccessStatusCode();
            JObject requestResultJsonObject = JObject.Parse(await httpResponseMessage.Content.ReadAsStringAsync());
            JToken installedPackages = requestResultJsonObject["InstalledPackages"];
            if (installedPackages == null) throw new Exception("MsIotApiWrapper.GetInstalledAppsAsync: Unable to query for \"InstalledPackages\" on the target.");
            // todo : test ob vorhanden
            foreach (JToken installedPackage in installedPackages)
            {
                try
                {                    
                    AppxPackage AppxPackage = new AppxPackage
                    {
                        PackageFullName = installedPackage["PackageFullName"].ToString(),
                        PackageName = installedPackage["Name"].ToString(),
                        PackageRelativeId = installedPackage["PackageRelativeId"].ToString(),
                        CanUninstall = Convert.ToBoolean(installedPackage["CanUninstall"].ToString())
                    };
                    packageList.Add(AppxPackage);
                }
                catch
                {
                    continue;
                }
            }
            return packageList;
        }

        /// <summary>
        /// The synchronous version of <see cref="GetInstalledAppsAsync"/>
        /// </summary>
        public List<AppxPackage> GetInstalledApps()
        {
            Task<List<AppxPackage>> getInstalledAppsTask = Task.Run(async () => await this.GetInstalledAppsAsync());
            Task.WaitAll(getInstalledAppsTask);
            return getInstalledAppsTask.Result;
        }

        /// <summary>
        /// Removes a package from the target asynchronously.
        /// </summary>
        /// <param name="appxPackage">The package to remove.</param>
        /// <exception cref="Exception">The provided package is marked as uninstallable.</exception>
        public async Task RemoveAppxPackageFromTargetAsync(AppxPackage appxPackage)
        {
            if (!appxPackage.CanUninstall) throw new Exception("MsIotApiWrapper.RemoveAppxPackageFromTarget: Package " + appxPackage.PackageName + " is marked as non-removable on the target.");
            HttpResponseMessage httpResponseMessage = await this.AuthenticatedApiRequestAsync(this.targetBaseUri + "/api/appx/packagemanager/package?package=" + appxPackage.PackageFullName, HttpMethod.DELETE);
            httpResponseMessage.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// The synchronous version of <see cref="RemoveAppxPackageFromTargetAsync(AppxPackage)"/>
        /// </summary>
        public void RemoveAppxPackageFromTarget(AppxPackage appxPackage)
        {
            Task removeAppxPackageFromTargetTask = Task.Run(async () => await this.RemoveAppxPackageFromTargetAsync(appxPackage));
            Task.WaitAll(removeAppxPackageFromTargetTask);
        }

        /// <summary>
        /// Tests if the package sideload was successful asynchronously.
        /// </summary>
        /// <remarks>The ms-iot api returns HTTP 204 as long as the installation progresses. Polling for HTTP 200 ensures successfull installation. The test times out after <see cref="SideloadSuccessTimeoutMillisecs"/> milliseconds. The poll interval can be set via <see cref="SideloadSuccessPollIntervalMillisecs"/>.</remarks>
        /// <exception cref="Exception">The test timed out and the installation success could not be verified.</exception>
        private async Task<bool> TestSideloadSuccessAsync()
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Reset();
            stopWatch.Start();
            while (stopWatch.ElapsedMilliseconds < this.SideloadSuccessTimeoutMillisecs)
            {
                // poll for 200 status code
                HttpResponseMessage httpResponseMessage = await this.AuthenticatedApiRequestAsync(this.targetBaseUri + "/api/app/packagemanager/state", HttpMethod.GET);
                httpResponseMessage.EnsureSuccessStatusCode();
                if (httpResponseMessage.StatusCode == HttpStatusCode.OK)
                {
                    try
                    {                        
                        JObject requestResultJsonObject = JObject.Parse(await httpResponseMessage.Content.ReadAsStringAsync());
                        bool success = Convert.ToBoolean(requestResultJsonObject["Success"]);
                        if (success) return true;
                        else return false;
                    }
                    catch { }
                }                          
                // if not 200 or response flaw: wait and retry
                await Task.Delay(TimeSpan.FromMilliseconds(this.SideloadSuccessPollIntervalMillisecs));
            }

            // we got here when the operation timed out
            throw new Exception("MsIotApiWrapper.TestSideloadSuccessAsync: The operation timed out");
        }

        /// <summary>
        /// Sideloads an .appx file on the target asynchronously. The operation times out after <see cref="SideloadSuccessTimeoutMillisecs"/> milliseconds.
        /// </summary>
        /// <param name="filePath">Path to the .appx file.</param>
        /// <exception cref="Exception">The uploaded file was not accepted as being installable.</exception>
        /// <exception cref="Exception">The uploaded file could not be installed.</exception>
        public async Task SideloadAppxPackageAsync(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            byte[] fileData = File.ReadAllBytes(filePath);
            MultipartFormDataContent multipartFormDataContent = new MultipartFormDataContent();
            multipartFormDataContent.Add(new StreamContent(new MemoryStream(fileData)), "\""+fileName+"\"", "\""+ fileName + "\"");
            HttpResponseMessage httpResponseMessage = await this.AuthenticatedApiRequestAsync(this.targetBaseUri + "/api/app/packagemanager/package?package=" + fileName, HttpMethod.POST, multipartFormDataContent);
            httpResponseMessage.EnsureSuccessStatusCode();
            JObject requestResultJsonObject = JObject.Parse(await httpResponseMessage.Content.ReadAsStringAsync());
            JToken reason = requestResultJsonObject["Reason"];
            if (reason == null || !reason.ToString().Contains("accepted")) throw new Exception("MsIotApiWrapper.SideloadAppxPackage: File upload request succeeded but the file was not accepted.");
            if (!await TestSideloadSuccessAsync()) throw new Exception("MsIotApiWrapper.SideloadAppxPackage: Uploaded file could not be processed by the target's packagemanager.");
        }

        /// <summary>
        /// The synchronous version of <see cref="SideloadAppxPackageAsync(string)"/>
        /// </summary>
        public void SideloadAppxPackage(string filePath)
        {
            Task sideloadAppxPackageTask = Task.Run(async () => await this.SideloadAppxPackageAsync(filePath));
            Task.WaitAll(sideloadAppxPackageTask);
        }

        /// <summary>
        /// Sets the default startup app asynchronously.
        /// </summary>
        /// <param name="app">The package that shall run on the device's startup</param>
        /// <remarks>This operation occurs in two steps: 1) Setting the app via the app's name and verification if the app was set as startup app.</remarks>
        /// <exception cref="Exception">The startup setting could not be verified, but it maybe functional.</exception>
        public async Task SetDefaultStartupAppAsync(AppxPackage app)
        {
            // converts the appId to UFT8 Base64 encoded string of the string-bytes
            string appId = Convert.ToBase64String(Encoding.UTF8.GetBytes(app.PackageRelativeId));
            
            // request set the startup app 
            HttpResponseMessage httpResponseMessage = await this.AuthenticatedApiRequestAsync(this.targetBaseUri + "/api/iot/appx/default?appid=" + appId, HttpMethod.POST, new StringContent(""));
            httpResponseMessage.EnsureSuccessStatusCode();

            // test if the app is now really set as startup
            httpResponseMessage = await this.AuthenticatedApiRequestAsync(this.targetBaseUri + "/api/iot/appx/default", HttpMethod.GET);
            httpResponseMessage.EnsureSuccessStatusCode();
            JObject requestResultJsonObject = JObject.Parse(await httpResponseMessage.Content.ReadAsStringAsync());            
            JToken appStartupInfo;
            bool isStartup = false;
            try
            {
                appStartupInfo = requestResultJsonObject["AppPackages"]
                    .Where(appx => appx["PackageFullName"].ToString()     // the property field "PackageFullName" seems to be a mistake, it should be PackageRelativeId. See https://social.msdn.microsoft.com/Forums/en-US/8984391b-da0a-4882-bf05-72f5fff90e0f/msiot-api-default-app-returns-wrong-property-name?forum=WindowsIoT
                    .Contains(app.PackageRelativeId))
                    .FirstOrDefault();
                isStartup = Convert.ToBoolean(appStartupInfo["IsStartup"]);
            }
            catch
            {
                throw new Exception("MsIotApiWrapper.SetDefaultStartupAppAsync: The server's response does not contain information about the desired startupp app.");
            }
            if (appStartupInfo == null || !isStartup)
            { 
                throw new Exception("MsIotApiWrapper.SetDefaultStartupAppAsync: Could not verify that " + app.PackageRelativeId + " was set as the target's startup app.");
            }
        }

        /// <summary>
        /// The synchronous version of <see cref="SetDefaultStartupAppAsync(AppxPackage)"/>
        /// </summary>
        public void SetDefaultStartupApp(AppxPackage app)
        {
            Task setDefaultStartupAppTask = Task.Run(async () => await this.SetDefaultStartupAppAsync(app));
            Task.WaitAll(setDefaultStartupAppTask);
        }

        /// <summary>
        /// Requests the target to reboot asynchronously.
        /// </summary>
        public async Task RebootTargetAsync()
        {
            HttpResponseMessage httpResponseMessage = await this.AuthenticatedApiRequestAsync(this.targetBaseUri + "/api/control/restart", HttpMethod.POST, new StringContent(""));
            httpResponseMessage.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// The synchronous version of <see cref="RebootTargetAsync"/>
        /// </summary>
        public void RebootTarget()
        {
            Task rebootTargetTask = Task.Run(async () => await this.RebootTargetAsync());
            Task.WaitAll(rebootTargetTask);
        }
    }
}
