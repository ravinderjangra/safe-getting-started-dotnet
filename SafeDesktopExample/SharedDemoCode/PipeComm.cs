using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using App.Network;

namespace App
{
    public static class PipeComm
    {
        private const string PipeName = "PIPE_SafeConsoleApp";
        private static readonly object _namedPiperServerThreadLock = new object();
        private static NamedPipeServerStream _namedPipeServerStream;
        private static NamedPipePayload _namedPipePayload;
        private static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Starts a new pipe server if one isn't already active.
        /// </summary>
        public static void NamedPipeServerCreateServer()
        {
            // Create a new pipe accessible by local authenticated users, disallow network
            var sidNetworkService = new SecurityIdentifier(WellKnownSidType.NetworkServiceSid, null);
            var sidWorld = new SecurityIdentifier(WellKnownSidType.WorldSid, null);

            var pipeSecurity = new PipeSecurity();

            // Deny network access to the pipe
            var accessRule = new PipeAccessRule(sidNetworkService, PipeAccessRights.ReadWrite, AccessControlType.Deny);
            pipeSecurity.AddAccessRule(accessRule);

            // Alow Everyone to read/write
            accessRule = new PipeAccessRule(sidWorld, PipeAccessRights.ReadWrite, AccessControlType.Allow);
            pipeSecurity.AddAccessRule(accessRule);

            // Current user is the owner
            var sidOwner = WindowsIdentity.GetCurrent().Owner;
            if (sidOwner != null)
            {
                accessRule = new PipeAccessRule(sidOwner, PipeAccessRights.FullControl, AccessControlType.Allow);
                pipeSecurity.AddAccessRule(accessRule);
            }

            // Create pipe and start the async connection wait
            _namedPipeServerStream = new NamedPipeServerStream(
                PipeName,
                PipeDirection.In,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous,
                0,
                0,
                pipeSecurity);

            // Begin async wait for connections
            _namedPipeServerStream.BeginWaitForConnection(NamedPipeServerConnectionCallback, _namedPipeServerStream);
        }

        /// <summary>
        /// The function called when a client connects to the named pipe. Note: This method is called on a non-UI thread.
        /// </summary>
        /// <param name="iAsyncResult"></param>
        public static async void NamedPipeServerConnectionCallback(IAsyncResult iAsyncResult)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                // End waiting for the connection
                _namedPipeServerStream.EndWaitForConnection(iAsyncResult);

                // Read data and prevent access to _namedPipePayload during threaded operations
                // Read data from client
                IFormatter f = new BinaryFormatter();
                _namedPipePayload = (NamedPipePayload)(f.Deserialize(_namedPipeServerStream));

                if (_namedPipePayload.SignalQuit)
                {
                    return;
                }

                // Console.WriteLine("Got Authenticaiton Response.");
                await Authentication.ProcessAuthenticationResponse(_namedPipePayload.Arguments);
            }
            catch (ObjectDisposedException)
            {
                // EndWaitForConnection will exception when someone closes the pipe before connection made
                // In that case we dont create any more pipes and just return
                // This will happen when app is closing and our pipe is closed/disposed
                return;
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                // Close the original pipe (we will create a new one each time)
                _namedPipeServerStream.Dispose();
                semaphoreSlim.Release();
            }

            // Create a new pipe for next connection
            NamedPipeServerCreateServer();
        }

        /// <summary>
        /// Uses a named pipe to send the currently parsed options to an already running instance.
        /// </summary>
        /// <param name="namedPipePayload"></param>
        public static void NamedPipeClientSendOptions(NamedPipePayload namedPipePayload)
        {
            try
            {
                using (var namedPipeClientStream = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
                {
                    namedPipeClientStream.Connect(3000); // Maximum wait 3 seconds
                    IFormatter f = new BinaryFormatter();
                    f.Serialize(namedPipeClientStream, namedPipePayload);

                    using (StreamReader sr = new StreamReader(namedPipeClientStream))
                    {
                        while (true)
                        {
                            string buffer;
                            try
                            {
                                buffer = sr.ReadLine();
                            }
                            catch
                            {
                                // read error has occurred
                                break;
                            }

                            // client has disconnected
                            if (buffer.Length == 0)
                            {
                                break;
                            }

                            // Console.WriteLine(buffer);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Error connecting or sending
            }
        }
    }
}
