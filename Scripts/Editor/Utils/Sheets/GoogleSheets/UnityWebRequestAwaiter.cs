using System.Runtime.CompilerServices;
using UnityEngine.Networking;

namespace TeamODD.ODDB.Editors.Utils.Sheets.GoogleSheets
{
    public static class UnityWebRequestAwaiter
    {
        public static TaskAwaiter GetAwaiter(this UnityWebRequestAsyncOperation asyncOp)
        {
            var tcs = new System.Threading.Tasks.TaskCompletionSource<object>();
            asyncOp.completed += obj => { tcs.SetResult(null); };
            return ((System.Threading.Tasks.Task)tcs.Task).GetAwaiter();
        }
    }
}