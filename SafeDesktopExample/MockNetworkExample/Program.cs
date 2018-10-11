using App;
using App.Network;
using SharedDemoCode;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MockNetworkExample
{
    internal class Program
    {
        private static Mutex mutex = null;
        private static bool _firstApplicationInstance;

        private static async Task Main()
        {
            string[] args = Environment.GetCommandLineArgs();
            Console.WriteLine("SafeNetwork Console Application");
            try
            {
                if (IsApplicationFirstInstance())
                {
                    //args[0] is always the path to the application
                    Helpers.RegisterAppProtocol(args[0]);
                    //^the method posted before, that edits registry

                    // Create a new pipe - it will return immediately and async wait for connections
                    PipeComm.NamedPipeServerCreateServer();

                    Console.WriteLine("Press Y/y to use safe-mock-browser for authentication : ");
                    var key = Console.ReadKey().KeyChar;
                    if (key == 'Y' || key == 'y')
                    {
                        // Request authentication from mock browser
                        await Authentication.MockAuthenticationWithBrowserAsync();
                    }
                    else
                    {
                        // Create session from mock authentication
                        var session = await Authentication.MockAuthenticationAsync();

                        // Perform Mutable Data operations
                        var mdOperations = new MutableDataOperations(session);
                        await mdOperations.PerformMDataOperations();
                    }
                }
                else
                {
                    // We are not the first instance, send the named pipe message with our payload and stop loading
                    if (args.Length >= 2)
                    {
                        // We are not the first instance, send the named pipe message with our payload and stop loading
                        var namedPipePayload = new NamedPipePayload
                        {
                            SignalQuit = false,
                            Arguments = args[1]
                        };

                        // Send the message
                        PipeComm.NamedPipeClientSendOptions(namedPipePayload);
                    }
                    // Close app
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
            }
            Console.ReadLine();
        }

        private static bool IsApplicationFirstInstance()
        {
            // Allow for multiple runs but only try and get the mutex once
            if (mutex == null)
            {
                mutex = new Mutex(true, ConsoleAppConstants.AppName, out _firstApplicationInstance);
            }
            return _firstApplicationInstance;
        }
    }
}
