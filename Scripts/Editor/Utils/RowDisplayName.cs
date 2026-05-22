using TeamODD.ODDB.Editors.Settings;
using TeamODD.ODDB.Runtime;

namespace TeamODD.ODDB.Editors.Utils
{
    /// <summary>
    /// Editor-only helper that resolves a Row's display name according to the
    /// <see cref="ODDBEditorSettings.UseFirstColumnAsRowName"/> preference.
    /// Runtime code uses Row.GetName() which always returns the ID.
    /// </summary>
    public static class RowDisplayName
    {
        public static string For(Row row)
        {
            if (row == null) return string.Empty;
            var settings = ODDBEditorSettings.Setting;
            if (settings != null && settings.UseFirstColumnAsRowName && row.Cells.Count > 0)
            {
                var first = row.Cells[0]?.GetData()?.ToString();
                if (!string.IsNullOrEmpty(first))
                    return first;
            }
            return row.ID.ToString();
        }
    }
}
