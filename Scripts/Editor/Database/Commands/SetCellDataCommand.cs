using System;
using TeamODD.ODDB.Runtime;

namespace TeamODD.ODDB.Editors.Commands
{
    /// <summary>
    /// Mutates a single cell value through the command pipeline so external callers
    /// (e.g. MCP tools) get undo/redo and history tracking. The Editor UI continues
    /// to mutate cells directly via SerializedProperty and does not route through here.
    /// </summary>
    public class SetCellDataCommand : BaseCommand
    {
        private readonly Table _table;
        private readonly string _rowId;
        private readonly int _fieldIndex;
        private readonly object _newValue;
        private readonly bool _direct;
        private readonly Action<string> _notifyChanged;

        private string _oldSerializedData;
        private bool _captured;

        public override string Name => "Set Cell Data";

        public SetCellDataCommand(
            Table table,
            string rowId,
            int fieldIndex,
            object newValue,
            Action<string> notifyChanged,
            bool direct = false)
        {
            _table = table;
            _rowId = rowId;
            _fieldIndex = fieldIndex;
            _newValue = newValue;
            _direct = direct;
            _notifyChanged = notifyChanged;
        }

        public override void Execute()
        {
            var cell = ResolveCell();
            if (cell == null) return;

            if (!_captured)
            {
                _oldSerializedData = cell.SerializedData;
                _captured = true;
            }
            cell.SetData(_newValue, _direct);
            _notifyChanged?.Invoke(_table.ID);
        }

        public override void Undo()
        {
            if (!_captured) return;
            var cell = ResolveCell();
            if (cell == null) return;

            cell.SetData(_oldSerializedData, direct: true);
            _notifyChanged?.Invoke(_table.ID);
        }

        private Cell ResolveCell()
        {
            if (_table == null || string.IsNullOrEmpty(_rowId)) return null;
            var row = _table.GetRow(_rowId);
            if (row == null) return null;
            return row.GetData(_fieldIndex);
        }
    }
}
