using SafeApp;
#if SAFE_APP_MOCK
using SafeApp.MockAuthBindings;
#endif
using SafeApp.Utilities;
using SharedDemoCode;
using System;
using System.Threading.Tasks;

namespace App.Network
{
    public class Authentication
    {
#if SAFE_APP_MOCK
        public static async Task<Session> MockAuthenticationAsync()
        {
            try
            {
                // Create a mock safe account to perform authentication.
                // We use this method while developing the app or working with tests.
                // This way we don't have to authenticate using safe-browser.

                // Generating random credentials
                var location = Helpers.GetRandomString(10);
                var password = Helpers.GetRandomString(10);
                var invitation = Helpers.GetRandomString(15);
                var authenticator = await Authenticator.CreateAccountAsync(location, password, invitation);
                authenticator = await Authenticator.LoginAsync(location, password);

                // Authentication and Logging
                var (_, reqMsg) = await Helpers.GenerateEncodedAppRequestAsync();
                var ipcReq = await authenticator.DecodeIpcMessageAsync(reqMsg);
                var authIpcReq = ipcReq as AuthIpcReq;
                var resMsg = await authenticator.EncodeAuthRespAsync(authIpcReq, true);
                var ipcResponse = await Session.DecodeIpcMessageAsync(resMsg);
                var authResponse = ipcResponse as AuthIpcMsg;

                // Initialize a new session
                var session = await Session.AppRegisteredAsync(ConsoleAppConstants.AppId, authResponse.AuthGranted);
                return session;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                throw ex;
            }
        }

        public static async Task MockAuthenticationWithBrowserAsync()
        {
            try
            {
                // Send request to mock safe-browser for authentication.
                // Use a mock account credentials in safe-browser and authenticate using the same.
                var encodedReq = await Helpers.GenerateEncodedAppRequestAsync();
                var url = Helpers.UrlFormat(encodedReq.Item2, true);
                System.Diagnostics.Process.Start(url);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                throw ex;
            }
        }
#else
        public static async Task NonMockAuthenticationWithBrowserAsync()
        {
            try
            {
                // Send request to non-mock safe-browser for authentication.
                // Login in safe-browser to authenticate.
                Console.WriteLine("Requesting authentication from Peruse");
                var encodedReq = await Helpers.GenerateEncodedAppRequestAsync();
                var url = Helpers.UrlFormat(encodedReq.Item2, true);
                System.Diagnostics.Process.Start(url);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                throw ex;
            }
        }
#endif

        public static async Task ProcessAuthenticationResponse(string authResponse)
        {
            try
            {
                var encodedRequest = Helpers.GetRequestData(authResponse);
                var decodeResult = await Session.DecodeIpcMessageAsync(encodedRequest);
                if (decodeResult.GetType() == typeof(AuthIpcMsg))
                {
                    var ipcMsg = decodeResult as AuthIpcMsg;
                    Console.WriteLine("Auth Reqest Granted from Authenticator");
                    // Create session object
                    if (ipcMsg != null)
                    {
                        // Initialize a new session
                        var session = await Session.AppRegisteredAsync(ConsoleAppConstants.AppId, ipcMsg.AuthGranted);

                        // Perform Mutable Data operations
                        var mdOperations = new MutableDataOperations(session);
                        await mdOperations.PerformMDataOperations();
                    }
                }
                else
                {
                    Console.WriteLine("Auth Request is not Granted");
                    throw new Exception("Auth Request not granted.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);;
            }
        }
    }
}
