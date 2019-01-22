using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using SafeApp;
using SafeApp.Utilities;
using SafeTodoExample.Helpers;
using SafeTodoExample.Model;
using SafeTodoExample.Service;
using Xamarin.Forms;
#if SAFE_APP_MOCK
using SafeApp.MockAuthBindings;
#endif

[assembly: Dependency(typeof(AppService))]

namespace SafeTodoExample.Service
{
    public class AppService : ObservableObject, IDisposable
    {
        private const string AppContainerListKey = "MySafeTodo";
        public const string AuthDeniedMessage = "Failed to receive Authentication.";
        private Session _session;
        private MDataInfo _mDataInfo;

        public bool IsSessionAvailable => _session != null;

        public AppService()
        {
            Session.Disconnected += OnSessionDisconnected;
        }

        public void Dispose()
        {
            FreeState();
            GC.SuppressFinalize(this);
        }

        ~AppService()
        {
            FreeState();
        }

        public void FreeState()
        {
            Session.Disconnected -= OnSessionDisconnected;
            _session?.Dispose();
            _session = null;
            _mDataInfo.Name = null;
        }

        private void OnSessionDisconnected(object obj, EventArgs e)
        {
            if (!obj.Equals(_session))
            {
                return;
            }

            Device.BeginInvokeOnMainThread(
              () =>
              {
                  _session?.Dispose();
              });
        }

        public async Task<(uint, string)> GenerateEncodedAuthReqAsync()
        {
            var authReq = new AuthReq
            {
                AppContainer = true,
                App = new AppExchangeInfo
                {
                    Id = Constants.AppId,
                    Scope = string.Empty,
                    Name = Constants.AppName,
                    Vendor = Constants.Vendor
                },
                Containers = new List<ContainerPermissions>()
            };
            return await Session.EncodeAuthReqAsync(authReq);
        }

        #region MutableData Operation

        public async Task GetMdInfoAsync()
        {
            try
            {
                // Retrieve MDataInfo from App container and deserialise
                var appContainerMDataInfo = await _session.AccessContainer.GetMDataInfoAsync("apps/" + Constants.AppId);
                var encryptedAppKey = await _session.MDataInfoActions.EncryptEntryKeyAsync(appContainerMDataInfo, AppContainerListKey.ToUtfBytes());
                var encryptedValue = await _session.MData.GetValueAsync(appContainerMDataInfo, encryptedAppKey);
                if (encryptedValue.Item1 != null)
                {
                    var plainValue = await _session.MDataInfoActions.DecryptAsync(appContainerMDataInfo, encryptedValue.Item1);
                    _mDataInfo = await _session.MDataInfoActions.DeserialiseAsync(plainValue);
                }
            }
            catch (FfiException ex)
            {
                Debug.WriteLine("Error : " + ex.Message);
                await CreateMutableData();
                await StoreMdInfoAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error : " + ex.Message);
                throw;
            }
        }

        public async Task StoreMdInfoAsync()
        {
            try
            {
                // Serialise and store MDataInfo in App container
                var serialisedMDataInfo = await _session.MDataInfoActions.SerialiseAsync(_mDataInfo);
                var appContainerMDataInfo = await _session.AccessContainer.GetMDataInfoAsync("apps/" + Constants.AppId);
                var encryptedAppKey = await _session.MDataInfoActions.EncryptEntryKeyAsync(appContainerMDataInfo, AppContainerListKey.ToUtfBytes());
                var encryptedMDataInfo = await _session.MDataInfoActions.EncryptEntryValueAsync(appContainerMDataInfo, serialisedMDataInfo);
                using (var appContEntActH = await _session.MDataEntryActions.NewAsync())
                {
                    await _session.MDataEntryActions.InsertAsync(appContEntActH, encryptedAppKey, encryptedMDataInfo);
                    await _session.MData.MutateEntriesAsync(appContainerMDataInfo, appContEntActH);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error : " + ex.Message);
                throw;
            }
        }

        private async Task CreateMutableData()
        {
            // Create a new random private mutable data
            const ulong tagType = 16000;
            _mDataInfo = await _session.MDataInfoActions.RandomPrivateAsync(tagType);

            var mDataPermissionSet = new PermissionSet
            {
                Insert = true,
                ManagePermissions = true,
                Read = true,
                Update = true,
                Delete = true
            };

            // Insert permission set
            using (var permissionsH = await _session.MDataPermissions.NewAsync())
            {
                using (var appSignKeyH = await _session.Crypto.AppPubSignKeyAsync())
                {
                    await _session.MDataPermissions.InsertAsync(permissionsH, appSignKeyH, mDataPermissionSet);
                    await _session.MData.PutAsync(_mDataInfo, permissionsH, NativeHandle.EmptyMDataEntries);
                }
            }
        }

        public async Task AddItemAsync(TodoItem todoItem)
        {
            try
            {
                // Add entries to mutable data
                using (var entriesHandle = await _session.MDataEntryActions.NewAsync())
                {
                    var encryptedKey = await _session.MDataInfoActions.EncryptEntryKeyAsync(_mDataInfo, todoItem.Title.ToUtfBytes());
                    var encryptedValue = await _session.MDataInfoActions.EncryptEntryValueAsync(_mDataInfo, todoItem.Serialize());
                    await _session.MDataEntryActions.InsertAsync(entriesHandle, encryptedKey, encryptedValue);
                    await _session.MData.MutateEntriesAsync(_mDataInfo, entriesHandle);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error : " + ex.Message);
                throw;
            }
        }

        public async Task<List<TodoItem>> GetItemAsync()
        {
            // Create a list to hold TodoItems fetched from the network
            var todoItems = new List<TodoItem>();
            try
            {
                // Retrieve the mutable data entries from the network and decrypt
                using (var entriesHandle = await _session.MDataEntries.GetHandleAsync(_mDataInfo))
                {
                    var encryptedEntries = await _session.MData.ListEntriesAsync(entriesHandle);
                    foreach (var entry in encryptedEntries)
                    {
                        if (entry.Value.Content.Count <= 0)
                        {
                            continue;
                        }

                        var decryptedKey = await _session.MDataInfoActions.DecryptAsync(_mDataInfo, entry.Key.Key);
                        var decryptedValue = await _session.MDataInfoActions.DecryptAsync(_mDataInfo, entry.Value.Content);
                        var deserializedValue = decryptedValue.Deserialize();
                        todoItems.Add(deserializedValue as TodoItem);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error : " + ex.Message);
                throw;
            }
            return todoItems;
        }

        public async Task UpdateItemAsync(TodoItem todoItem)
        {
            try
            {
                // Update mutable data entry
                using (var entriesHandle = await _session.MDataEntryActions.NewAsync())
                {
                    var keyToUpdate = await _session.MDataInfoActions.EncryptEntryKeyAsync(_mDataInfo, todoItem.Title.ToUtfBytes());
                    var newValueToUpdate = await _session.MDataInfoActions.EncryptEntryValueAsync(_mDataInfo, todoItem.Serialize());
                    var value = await _session.MData.GetValueAsync(_mDataInfo, keyToUpdate);
                    await _session.MDataEntryActions.UpdateAsync(entriesHandle, keyToUpdate, newValueToUpdate, value.Item2 + 1);
                    await _session.MData.MutateEntriesAsync(_mDataInfo, entriesHandle);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error : " + ex.Message);
                throw;
            }
        }

        public async Task DeleteItemAsync(TodoItem todoItem)
        {
            try
            {
                // Delete mutable data entry
                using (var entriesHandle = await _session.MDataEntryActions.NewAsync())
                {
                    var keyToDelete = await _session.MDataInfoActions.EncryptEntryKeyAsync(_mDataInfo, todoItem.Title.ToUtfBytes());
                    var value = await _session.MData.GetValueAsync(_mDataInfo, keyToDelete);
                    await _session.MDataEntryActions.DeleteAsync(entriesHandle, keyToDelete, value.Item2 + 1);
                    await _session.MData.MutateEntriesAsync(_mDataInfo, entriesHandle);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error : " + ex.Message);
                throw;
            }
        }

        #endregion

        #region Test Network Authentication

        public async Task ProcessNonMockAuthentication()
        {
            var encodedAuthReq = await GenerateEncodedAuthReqAsync();
            var url = UrlFormat.Format(Constants.AppId, encodedAuthReq.Item2, true);
            Device.BeginInvokeOnMainThread(() => { Device.OpenUri(new Uri(url)); });
        }

        public async Task HandleUrlActivationAsync(string url)
        {
            try
            {
                // Decode auth response and Initialise a new session
                var encodedRequest = UrlFormat.GetRequestData(url);
                var decodeResult = await Session.DecodeIpcMessageAsync(encodedRequest);
                if (decodeResult.GetType() == typeof(AuthIpcMsg))
                {
                    var ipcMsg = decodeResult as AuthIpcMsg;

                    if (ipcMsg != null)
                    {
                        _session = await Session.AppRegisteredAsync(Constants.AppId, ipcMsg.AuthGranted);
                        DialogHelper.ShowToast("Auth Granted", DialogType.Success);
                        MessagingCenter.Send(this, MessengerConstants.NavigateToItemPage);
                    }
                }
                else
                {
                    Debug.WriteLine("Auth Req is not Auth Granted");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Description: {ex.Message}", "OK");
                DialogHelper.ShowToast(AuthDeniedMessage, DialogType.Error);
            }
        }

        #endregion

        #region Mock Authentication
#if SAFE_APP_MOCK
        private Authenticator _authenticator;

        private async Task CreateAccountAsync()
        {
            try
            {
                // Create a mock account
                var location = Misc.GetRandomString(10);
                var password = Misc.GetRandomString(10);
                var invitation = Misc.GetRandomString(15);
                _authenticator = await Authenticator.CreateAccountAsync(location, password, invitation);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async Task<Session> CreateTestAppAsync()
        {
            // Authenticate using mock authentication API
            var (_, reqMsg) = await GenerateEncodedAuthReqAsync();
            var ipcReq = await _authenticator.DecodeIpcMessageAsync(reqMsg);
            var authIpcReq = ipcReq as AuthIpcReq;
            var resMsg = await _authenticator.EncodeAuthRespAsync(authIpcReq, true);
            var ipcResponse = await Session.DecodeIpcMessageAsync(resMsg);
            var authResponse = ipcResponse as AuthIpcMsg;
            return await Session.AppRegisteredAsync(Constants.AppId, authResponse.AuthGranted);
        }

        public async Task ProcessMockAuthentication()
        {
            await CreateAccountAsync();
            _session = await CreateTestAppAsync();
        }

        public async Task LogoutAsync()
        {
            await Task.Run(() =>
            {
                _authenticator.Dispose();
                Dispose();
            });
        }
#else
        public async Task LogoutAsync()
        {
            await Task.Run(() => { Dispose(); });
        }
#endif
        #endregion
    }
}
