using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TeamODD.ODDB.Editors.Utils.Sheets
{
    public interface ISheetBackend
    {
        string DisplayName { get; }

        bool SupportsPartial { get; }

        Task<BackendContext> PrepareAsync(ExportScope scope, BackendIntent intent);

        Task<IReadOnlyList<SheetInfo>> LoadAsync(
            BackendContext ctx,
            IProgress<float> progress,
            CancellationToken ct);

        Task SaveAsync(
            BackendContext ctx,
            IReadOnlyList<SheetInfo> sheets,
            IProgress<float> progress,
            CancellationToken ct);
    }
}
