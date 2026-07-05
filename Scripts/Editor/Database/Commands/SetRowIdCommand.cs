using System;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Utils.Converters;

namespace TeamODD.ODDB.Editors.Commands
{
    public sealed class SetRowIdCommand : BaseCommand
    {
        private readonly Table _table;
        private readonly string _oldId;
        private readonly string _newId;
        private readonly Action<string> _notifyChanged;
        private Row _row;
        private int _rowIndex = -1;
        private bool _captured;

        public override string Name => "Set Row ID";

        public SetRowIdCommand(Table table, string oldId, string newId, Action<string> notifyChanged)
        {
            _table = table;
            _oldId = oldId;
            _newId = newId;
            _notifyChanged = notifyChanged;
        }

        public override void Execute()
        {
            if (SameId(_oldId, _newId))
                return;

            CaptureOriginal();
            ReKey(_oldId, _newId);
        }

        public override void Undo()
        {
            if (!_captured || _row == null || SameId(_oldId, _newId))
                return;

            ReKey(_newId, _oldId);
        }

        private void CaptureOriginal()
        {
            if (_captured)
                return;

            _row = _table?.GetRow(_oldId);
            _rowIndex = IndexOf(_row);
            _captured = true;
        }

        private void ReKey(string fromId, string toId)
        {
            var row = _table?.GetRow(fromId);
            if (row == null)
                return;

            _table.RemoveRow(fromId);
            row.ID = new ODDBID(toId);
            _table.RestoreRow(row);
            RestoreRowIndex(row);
            _notifyChanged?.Invoke(_table.ID.ToString());
        }

        private void RestoreRowIndex(Row row)
        {
            if (row == null || _rowIndex < 0)
                return;

            var currentIndex = IndexOf(row);
            if (currentIndex < 0 || currentIndex == _rowIndex)
                return;

            _table.Rows.RemoveAt(currentIndex);
            var targetIndex = Math.Min(_rowIndex, _table.Rows.Count);
            _table.Rows.Insert(targetIndex, row);
        }

        private int IndexOf(Row row)
        {
            return row == null ? -1 : _table.Rows.IndexOf(row);
        }

        private static bool SameId(string first, string second)
        {
            return string.Equals(first, second, StringComparison.Ordinal);
        }
    }
}
