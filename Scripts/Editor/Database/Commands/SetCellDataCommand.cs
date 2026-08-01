using System;
using TeamODD.ODDB.Runtime;

namespace TeamODD.ODDB.Editors.Commands
{
    /// <summary>
    /// Adds undo/redo and history tracking around the Core Table.SetCellData mutation.
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
            if (_table.SetCellData(_rowId, _fieldIndex, _newValue, _direct))
                _notifyChanged?.Invoke(_table.ID);
        }

        public override void Undo()
        {
            if (!_captured) return;
            var cell = ResolveCell();
            if (cell == null) return;

            if (_table.SetCellData(_rowId, _fieldIndex, _oldSerializedData, direct: true))
                _notifyChanged?.Invoke(_table.ID);
        }

        private Cell ResolveCell()
        {
            return _table?.GetCell(_rowId, _fieldIndex);
        }
    }
}
