using System.Threading;
using System.Threading.Tasks;

namespace TeamODD.ODDB.Runtime.Async
{
    /// <summary>
    /// Bridges generated ODDB async fields to the application's asset manager.
    /// </summary>
    public interface IAsyncLoader
    {
        Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default);

        void Release<T>(T asset);
    }
}
