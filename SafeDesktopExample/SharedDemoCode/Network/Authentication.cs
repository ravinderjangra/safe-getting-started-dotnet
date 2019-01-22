using SafeApp;
#if SAFE_APP_MOCK
using SafeApp.MockAuthBindings;
#endif
using System;
using System.Threading.Tasks;
using SafeApp.Utilities;
using SharedDemoCode;

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

                // Generating random mock account
                var location = Helpers.GenerateRandomString(10);
                var password = Helpers.GenerateRandomString(10);
                var invitation = Helpers.GenerateRandomString(15);
                var authenticator = await Authenticator.CreateAccountAsync(location, password, invitation);

                // Authentication
                var (_, reqMsg) = await Helpers.GenerateEncodedAppRequestAsync();
                var ipcReq = await authenticator.DecodeIpcMessageAsync(reqMsg);
                var authIpcReq = ipcReq as AuthIpcReq;
                var resMsg = await authenticator.EncodeAuthRespAsync(authIpcReq, true);
                var ipcResponse = await Session.DecodeIpcMessageAsync(resMsg);
                var authResponse = ipcResponse as AuthIpcMsg;

                // return a new session
                var session = await Session.AppRegisteredAsync(ConsoleAppConstants.AppId, authResponse.AuthGranted);
                return session;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                throw ex;
            }
        }
#endif

        public static async Task AuthenticationWithBrowserAsync()
        {
            try
            {
                // Generate and send auth request to safe-browser for authentication.
                Console.WriteLine("Requesting authentication from mock Safe browser");
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

        public static async Task ProcessAuthenticationResponse(string authResponse)
        {
            try
            {
                // Decode auth response and initialise a new session
                var encodedRequest = Helpers.GetRequestData(authResponse);
                var decodeResult = await Session.DecodeIpcMessageAsync(encodedRequest);
                if (decodeResult.GetType() == typeof(AuthIpcMsg))
                {
                    var ipcMsg = decodeResult as AuthIpcMsg;
                    Console.WriteLine("Auth Reqest Granted from Authenticator");

                    // Create session object
                    if (ipcMsg != null)
                    {
                        // Initialise a new session
                        var session = await Session.AppRegisteredAsync(ConsoleAppConstants.AppId, ipcMsg.AuthGranted);
                        DataOperations.InitialiseSession(session);
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
                Console.WriteLine("Exception: " + ex.Message);
            }
        }
    }
}
