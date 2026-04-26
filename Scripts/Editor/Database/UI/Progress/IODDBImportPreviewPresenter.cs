using System.Threading;
using System.Threading.Tasks;
using TeamODD.ODDB.Editors.Utils.Sheets.Diff;
using TeamODD.ODDB.Editors.Utils.Sheets.Validation;

namespace TeamODD.ODDB.Editors.UI.Progress
{
    internal interface IODDBImportPreviewPresenter
    {
        Task<bool> ShowImportPreviewAsync(
            SheetImportDiffReport diffReport,
            SheetValidationReport validationReport,
            CancellationToken ct);
    }
}
