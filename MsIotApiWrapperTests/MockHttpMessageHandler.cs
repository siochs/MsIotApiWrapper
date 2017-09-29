using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MsIotApiWrapperTests
{
    /// <summary>
    /// This class represents a message handler that emulates the endpoint of a ms-iot REST API servicing device. Only a few methods are provided yet.
    /// </summary>
    class MockHttpMessageHandler : HttpMessageHandler
    {
        private delegate HttpResponseMessage ResponseMessageBuilderDelegate();
        private ResponseMessageBuilderDelegate responseMessageBuilderDelegate;
        private byte _acceptMessageCounter = 1;
        private byte AcceptMessageCounter
        {
            get { return _acceptMessageCounter; }
            set { if (value == 4) _acceptMessageCounter = 1; else _acceptMessageCounter = value; }
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // this is a basic auth password check
            if (!request.Headers.Authorization.Scheme.Equals("Basic") || !request.Headers.Authorization.Parameter.Equals("dXNlcjpwYXNzd29yZA==")) // mock credentials: "user:password"
            {
                return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
            }

            switch (request.Method.Method)
            {
                case "GET":
                    // get a list of all installed appx packages from the packagemanager
                    if (request.RequestUri.AbsoluteUri.Equals("http://127.0.0.1:8080/api/appx/packagemanager/packages"))
                        return BuildPackageManagerListInstalledAppsResponseMessage();
                    // get the current state of the packagemanager
                    if (request.RequestUri.AbsoluteUri.Equals("http://127.0.0.1:8080/api/app/packagemanager/state"))
                    {
                        // to get the package manager state an action has to be performed first via POST. Otherwise you get 404 "no install action"
                        if (this.responseMessageBuilderDelegate == null) this.responseMessageBuilderDelegate = new ResponseMessageBuilderDelegate(BuildPackageManagerFoundNoInstallationActionResponseMessage);
                        Task<HttpResponseMessage> buildPackageManagerStateTask = Task.Run(() => this.responseMessageBuilderDelegate());
                        return await buildPackageManagerStateTask; 
                    }
                    // get a list of apps that can run as startup default app
                    if (request.RequestUri.AbsoluteUri.Equals("http://127.0.0.1:8080/api/iot/appx/default"))
                        return BuildListOfDefaultAppsResponseMessage();

                    break;

                case "POST":
                    // post a working appx file to the packagemanager for installation. The following "state" GET request will first tell NoContent and after 3 polls OK with "Success=true" in the content.
                    if (request.RequestUri.AbsoluteUri.Equals("http://127.0.0.1:8080/api/app/packagemanager/package?package=workingTestFile.appx") && await ContentIsTestFile(request.Content, "workingTestFile.appx"))
                    {
                        this.responseMessageBuilderDelegate = new ResponseMessageBuilderDelegate(BuildPackageManagerStateFirstNoContentThenOKSuccessResponseMessage);
                        return BuildPackageManagerSideloadAcceptedResponseMessage();
                    }
                    // post a not working appx file to the packagemanager for installation. The following "state" GET request will first tell NoContent and after 3 polls OK with "Success=false" in the content.
                    if (request.RequestUri.AbsoluteUri.Equals("http://127.0.0.1:8080/api/app/packagemanager/package?package=notWorkingTestFile.appx"))
                    {
                        this.responseMessageBuilderDelegate = new ResponseMessageBuilderDelegate(BuildPackageManagerStateFirstNoContentThenOKNoSuccessResponseMessage);
                        return BuildPackageManagerSideloadAcceptedResponseMessage();
                    }
                    // post a working appx file to the packagemanager for installation. The following "state" GET request will always return NoContent. Because the response will never be OK, this will lead to a timeout.
                    if (request.RequestUri.AbsoluteUri.Equals("http://127.0.0.1:8080/api/app/packagemanager/package?package=timeoutTestFile.appx"))
                    {
                        this.responseMessageBuilderDelegate = new ResponseMessageBuilderDelegate(() => { return new HttpResponseMessage(System.Net.HttpStatusCode.NoContent); });
                        return BuildPackageManagerSideloadAcceptedResponseMessage();
                    }
                    // set the default startup app for an existing app
                    if (request.RequestUri.AbsoluteUri.Equals("http://127.0.0.1:8080/api/iot/appx/default?appid=RXhpc3RpbmdQYWNrYWdlUmVsYXRpdmVJZA==")) //appid = ExistingPackageRelativeId
                        return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
                    // set the default startup app for a not existing app
                    if (request.RequestUri.AbsoluteUri.Equals("http://127.0.0.1:8080/api/iot/appx/default?appid=Tm90RXhpc3RpbmdQYWNrYWdlUmVsYXRpdmVJZA==")) //appid = NotExistingPackageRelativeId
                        return BuildStartupAppCouldNotBeSetResponseMessage();
                    // reboot the system
                    if (request.RequestUri.AbsoluteUri.Equals("http://127.0.0.1:8080/api/control/restart"))
                        return new HttpResponseMessage(System.Net.HttpStatusCode.OK);

                    break;

                case "DELETE":
                    // tell the packagemanager to remove a not existing app
                    if (request.RequestUri.AbsoluteUri.Equals("http://127.0.0.1:8080/api/appx/packagemanager/package?package=DoesNotExist"))
                        return BuildPackageManagerCouldNotRemoveNotExistingAppResponseMessage();
                    // tell the packagemanager to remove an existing app
                    if (request.RequestUri.AbsoluteUri.Equals("http://127.0.0.1:8080/api/appx/packagemanager/package?package=Exists"))
                        return new HttpResponseMessage(System.Net.HttpStatusCode.OK);  

                    break;

                default:
                    break;                    
            }

            // we get here when none of the test api-calls above match
            throw new NotImplementedException("MockHttpMessageHandler: Not implemented api call.");
        }

        private HttpResponseMessage BuildPackageManagerFoundNoInstallationActionResponseMessage()
        {
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
            httpResponseMessage.Content = new StringContent("{\"Reason\":\"No installation action was found\"}");
            return httpResponseMessage;
        }

        private HttpResponseMessage BuildListOfDefaultAppsResponseMessage()
        {
            HttpResponseMessage httpResponseMessage;
            httpResponseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            httpResponseMessage.Content = new StringContent("{\"DefaultApp\":\"IoTCoreDefaultApp_1w720vyc4ccym!App\",\"AppPackages\":[{\"IsStartup\":false,\"PackageFullName\":\"IoTCoreDefaultApp_1w720vyc4ccym!App\"},{\"IsStartup\":false,\"PackageFullName\":\"IoTUAPOOBE_cw5n1h2txyewy!App\"},{\"IsStartup\":false,\"PackageFullName\":\"Microsoft.Windows.Cortana_cw5n1h2txyewy!CortanaUI\"},{\"IsStartup\":true,\"PackageFullName\":\"ExistingPackageRelativeId\"}]}");
            return httpResponseMessage;
        }

        private HttpResponseMessage BuildStartupAppCouldNotBeSetResponseMessage()
        {
            HttpResponseMessage httpResponseMessage;
            httpResponseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
            httpResponseMessage.Content = new StringContent("{\"ErrorCode\":2147942487,\"ErrorSource\":\"notExisting\",\"Status\":\"Set Startup AppX failed\"}");
            return httpResponseMessage;
        }

        private HttpResponseMessage BuildPackageManagerStateFirstNoContentThenOKSuccessResponseMessage()
        {
            // the first 3 calls of this method will return NoContent == pseudo 'work in progress'. 
            // the last call will return OK with "Success=true" in the content.
            HttpResponseMessage httpResponseMessage;
            if (this.AcceptMessageCounter == 3) 
            {
                httpResponseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
                httpResponseMessage.Content = new StringContent("{\"Code\":0,\"CodeText\":\"The operation completed successfully.\\r\\n\",\"Reason\":\"Success\",\"Success\":true}");
            }
            else
            {
                httpResponseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.NoContent);
            }
            this.AcceptMessageCounter++;
            return httpResponseMessage;
        }

        private HttpResponseMessage BuildPackageManagerStateFirstNoContentThenOKNoSuccessResponseMessage()
        {
            // the first 3 calls of this method will return NoContent == pseudo 'work in progress'. 
            // the last call will return OK with "Success=false" in the content.
            HttpResponseMessage httpResponseMessage;
            if (this.AcceptMessageCounter == 3) // after 3 retries give a OK response but with failure content
            {
                httpResponseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
                httpResponseMessage.Content = new StringContent("{\"Code\":-2147024883,\"CodeText\":\"The data is invalid.\\r\\n\",\"Reason\":\"error 0x8007000D: Opening the package from location notWorkingTestFile.appx failed.\",\"Success\":false}");
            }
            else
            {
                httpResponseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.NoContent);
            }
            this.AcceptMessageCounter++;
            return httpResponseMessage;
        }

        private async Task<bool> ContentIsTestFile(HttpContent httpContent, string fileName)
        {
            if (httpContent is MultipartFormDataContent)
            {
                MultipartFormDataContent multipartFormDataContent = httpContent as MultipartFormDataContent;
                string multipartContentAsString = await multipartFormDataContent.ReadAsStringAsync();
                if (multipartContentAsString.Contains("Content-Disposition: form-data; name=\"" + fileName + "\"; filename=\"" + fileName + "\"; filename*=utf-8''%22workingTestFile.appx%22\r\n\r\nDummy content for test case.\r\n"))
                    return true;
                else
                    return false;
            }

            return false;
        }

        private HttpResponseMessage BuildPackageManagerSideloadAcceptedResponseMessage()
        {
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.Accepted);
            httpResponseMessage.Content = new StringContent("{\"Reason\":\"Deploy request accepted and being processed\"}");
            return httpResponseMessage;
        }

        private HttpResponseMessage BuildPackageManagerCouldNotRemoveNotExistingAppResponseMessage()
        {
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
            httpResponseMessage.Content = new StringContent("{\"Code\":-2147023728,\"CodeText\":\"Element not found.\\r\\n\",\"Reason\":\"Failed to retrieve package\",\"Success\":false}");
            return httpResponseMessage;
        }

        private HttpResponseMessage BuildPackageManagerListInstalledAppsResponseMessage()
        {
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            httpResponseMessage.Content = new StringContent("{\"InstalledPackages\":[{\"AppListEntry\":0,\"CanUninstall\":true,\"Name\":\"Search\",\"PackageFamilyName\":\"Microsoft.Windows.Cortana\",\"PackageFullName\":\"Microsoft.Windows.Cortana_1.8.12.15063_neutral_neutral_cw5n1h2txyewy\",\"PackageOrigin\":2,\"PackageRelativeId\":\"Microsoft.Windows.Cortana_cw5n1h2txyewy!CortanaUI\",\"Publisher\":\"CN=Microsoft Windows, O=Microsoft Corporation, L=Redmond, S=Washington, C=US\",\"Version\":{\"Build\":12,\"Major\":1,\"Minor\":8,\"Revision\":15063},\"RegisteredUsers\":[{\"UserDisplayName\":\"MINWINPC\\\\DefaultAccount\",\"UserSID\":\"S-1-5-21-2702878673-795188819-444038987-503\"}]},{\"AppListEntry\":0,\"CanUninstall\":true,\"Name\":\"IoTUAPOOBE\",\"PackageFamilyName\":\"IoTUAPOOBE\",\"PackageFullName\":\"IoTUAPOOBE_1.0.0.0_neutral__cw5n1h2txyewy\",\"PackageOrigin\":2,\"PackageRelativeId\":\"IoTUAPOOBE_cw5n1h2txyewy!App\",\"Publisher\":\"CN=Microsoft Windows, O=Microsoft Corporation, L=Redmond, S=Washington, C=US\",\"Version\":{\"Build\":0,\"Major\":1,\"Minor\":0,\"Revision\":0},\"RegisteredUsers\":[{\"UserDisplayName\":\"MINWINPC\\\\DefaultAccount\",\"UserSID\":\"S-1-5-21-2702878673-795188819-444038987-503\"}]},{\"AppListEntry\":0,\"CanUninstall\":true,\"Name\":\"IoTCoreDefaultApp\",\"PackageFamilyName\":\"IoTCoreDefaultApp\",\"PackageFullName\":\"IoTCoreDefaultApp_1.0.1702.21000_arm__1w720vyc4ccym\",\"PackageOrigin\":5,\"PackageRelativeId\":\"IoTCoreDefaultApp_1w720vyc4ccym!App\",\"Publisher\":\"CN=MSFT\",\"Version\":{\"Build\":1702,\"Major\":1,\"Minor\":0,\"Revision\":21000},\"RegisteredUsers\":[{\"UserDisplayName\":\"MINWINPC\\\\DefaultAccount\",\"UserSID\":\"S-1-5-21-2702878673-795188819-444038987-503\"}]},{\"AppListEntry\":1,\"CanUninstall\":true,\"Name\":\"IoTOnboardingTask\",\"PackageFamilyName\":\"IoTOnboardingTask-uwp\",\"PackageFullName\":\"IoTOnboardingTask-uwp_1.0.1612.2000_arm__1w720vyc4ccym\",\"PackageOrigin\":5,\"PackageRelativeId\":\"IoTOnboardingTask-uwp_1w720vyc4ccym!App\",\"Publisher\":\"CN=MSFT\",\"Version\":{\"Build\":1612,\"Major\":1,\"Minor\":0,\"Revision\":2000},\"RegisteredUsers\":[{\"UserDisplayName\":\"MINWINPC\\\\DefaultAccount\",\"UserSID\":\"S-1-5-21-2702878673-795188819-444038987-503\"}]}]}");
            return httpResponseMessage;
        }
    }
}
