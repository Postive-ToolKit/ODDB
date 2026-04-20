using System;
using TeamODD.ODDB.Runtime;

namespace TeamODD.ODDB.Editors.Commands
{
    public class RemoveRowCommand : BaseCommand
    {
        private readonly Table _table;
        private readonly string _rowId;
        private readonly Action<string> _notifyChanged;
        private Row _removedRow;

        public override string Name => "Remove Row";

        public RemoveRowCommand(Table table, string rowId, Action<string> notifyChanged)
        {
            _table = table;
            _rowId = rowId;
            _notifyChanged = notifyChanged;
        }

        public override void Execute()
        {
            _removedRow = _table.GetRow(_rowId);
            _table.RemoveRow(_rowId);
            _notifyChanged?.Invoke(_table.ID);
        }

        public override void Undo()
        {
            if (_removedRow != null)
            {
                _table.RestoreRow(_removedRow);
                _notifyChanged?.Invoke(_table.ID);
            }
        }
    }
}
