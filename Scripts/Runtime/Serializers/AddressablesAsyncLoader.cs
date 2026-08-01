#if ADDRESSABLE_EXIST
using System;
using System.Threading;
using System.Threading.Tasks;
using TeamODD.ODDB.Runtime.Async;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace TeamODD.ODDB.Runtime.Serializers
{
    /// <summary>
    /// Optional Addressables adapter for ODDB async fields.
    /// Register an instance with ODDB.RegisterAsyncLoader during application startup.
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
}
#endif
