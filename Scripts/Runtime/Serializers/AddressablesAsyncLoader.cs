#if ADDRESSABLE_EXIST
using System;
using System.Threading;
using System.Threading.Tasks;
using TeamODD.ODDB.Runtime.Async;
using TeamODD.ODDB.Runtime.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace TeamODD.ODDB.Runtime.Serializers
{
    /// <summary>
    /// Default Addressables adapter for ODDB async fields. It is registered automatically
    /// when ODDBRuntimeSettings.UseAddressableAutoLoad is enabled, or can be registered manually.
    /// </summary>
    public sealed class AddressablesAsyncLoader : IAsyncLoader
    {
        public Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
                return Task.FromResult(default(T));

            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(key);
            var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            CancellationTokenRegistration cancellationRegistration = default;

            if (cancellationToken.CanBeCanceled)
            {
                cancellationRegistration = cancellationToken.Register(
                    () => completion.TrySetCanceled());
            }

            handle.Completed += operation =>
            {
                cancellationRegistration.Dispose();

                if (operation.Status == AsyncOperationStatus.Succeeded)
                {
                    if (!completion.TrySetResult(operation.Result))
                        Addressables.Release(operation);
                    return;
                }

                Exception exception = operation.OperationException
                    ?? new InvalidOperationException(
                        $"Addressables failed to load '{key}' as {typeof(T).FullName}.");
                completion.TrySetException(exception);
                Addressables.Release(operation);
            };

            return completion.Task;
        }

        public void Release<T>(T asset)
        {
            if (ReferenceEquals(asset, null))
                return;

            Addressables.Release(asset);
        }
    }

    internal static class AddressablesAsyncLoaderBoot
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void InstallRuntimeDefault()
        {
            InstallIfEnabled();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void InstallEditorDefault()
        {
            InstallIfEnabled();
        }
#endif

        internal static bool InstallIfEnabled()
        {
            var settings = ODDBRuntimeSettings.TryLoad();
            if (settings == null || !settings.UseAddressableAutoLoad)
                return false;

            return ODDB.TryRegisterAsyncLoader(new AddressablesAsyncLoader());
        }
    }
}
#endif
