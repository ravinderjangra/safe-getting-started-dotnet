using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SafeApp;
using SafeApp.Utilities;

namespace App.Network
{
    public class DataOperations
    {
        private static Session _session;

        private MDataInfo _mdinfo;

        public static void InitialiseSession(Session session)
        {
            try
            {
                _session = session;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        internal async Task CreateMutableData()
        {
            try
            {
                Console.WriteLine("\nCreating new mutable data");
                const ulong tagType = 15010;
                _mdinfo = await _session.MDataInfoActions.RandomPrivateAsync(tagType);

                var mDataPermissionSet = new PermissionSet { Insert = true, ManagePermissions = true, Read = true, Update = true, Delete = true };
                using (var permissionsH = await _session.MDataPermissions.NewAsync())
                {
                    using (var appSignKeyH = await _session.Crypto.AppPubSignKeyAsync())
                    {
                        await _session.MDataPermissions.InsertAsync(permissionsH, appSignKeyH, mDataPermissionSet);
                        await _session.MData.PutAsync(_mdinfo, permissionsH, NativeHandle.EmptyMDataEntries);
                    }
                }
                Console.WriteLine("Mutable data created successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.ReadLine();
                Environment.Exit(1);
            }
        }

        internal async Task AddEntry(string key, string value)
        {
            try
            {
                using (var entryActionsH = await _session.MDataEntryActions.NewAsync())
                {
                    var encryptedKey = await _session.MDataInfoActions.EncryptEntryKeyAsync(_mdinfo, key.ToUtfBytes());
                    var encryptedValue = await _session.MDataInfoActions.EncryptEntryValueAsync(_mdinfo, value.ToUtfBytes());
                    await _session.MDataEntryActions.InsertAsync(entryActionsH, encryptedKey, encryptedValue);
                    await _session.MData.MutateEntriesAsync(_mdinfo, entryActionsH);
                }
                Console.WriteLine("Entry Added");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        internal async Task<List<MDataEntry>> GetEntries()
        {
            List<MDataEntry> entries = new List<MDataEntry>();
            try
            {
                using (var entriesHandle = await _session.MDataEntries.GetHandleAsync(_mdinfo))
                {
                    var encryptedEntries = await _session.MData.ListEntriesAsync(entriesHandle);
                    foreach (var entry in encryptedEntries)
                    {
                        if (entry.Value.Content.Count != 0)
                        {
                            var decryptedKey = await _session.MDataInfoActions.DecryptAsync(_mdinfo, entry.Key.Key.ToList());
                            var decryptedValue = await _session.MDataInfoActions.DecryptAsync(_mdinfo, entry.Value.Content.ToList());
                            entries.Add(new MDataEntry()
                            {
                                Key = new MDataKey() { Key = decryptedKey },
                                Value = new MDataValue { Content = decryptedValue, EntryVersion = entry.Value.EntryVersion }
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            return entries;
        }

        internal async Task UpdateEntry(string key, string newValue)
        {
            try
            {
                var keyToUpdate = await _session.MDataInfoActions.EncryptEntryKeyAsync(_mdinfo, key.ToUtfBytes());
                var newValueToUpdate = await _session.MDataInfoActions.EncryptEntryValueAsync(_mdinfo, newValue.ToUtfBytes());
                using (var entriesHandle = await _session.MDataEntryActions.NewAsync())
                {
                    var value = await _session.MData.GetValueAsync(_mdinfo, keyToUpdate);
                    await _session.MDataEntryActions.UpdateAsync(entriesHandle, keyToUpdate, newValueToUpdate, value.Item2 + 1);
                    await _session.MData.MutateEntriesAsync(_mdinfo, entriesHandle);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        internal async Task DeleteEntry(string key)
        {
            try
            {
                var keyToDelete = await _session.MDataInfoActions.EncryptEntryKeyAsync(_mdinfo, key.ToUtfBytes());
                using (var entriesHandle = await _session.MDataEntryActions.NewAsync())
                {
                    var value = await _session.MData.GetValueAsync(_mdinfo, keyToDelete);
                    await _session.MDataEntryActions.DeleteAsync(entriesHandle, keyToDelete, value.Item2 + 1);
                    await _session.MData.MutateEntriesAsync(_mdinfo, entriesHandle);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
