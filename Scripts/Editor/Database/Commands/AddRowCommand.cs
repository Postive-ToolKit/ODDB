using System;
using TeamODD.ODDB.Runtime;

namespace TeamODD.ODDB.Editors.Commands
{
    public class AddRowCommand : BaseCommand
    {
        private readonly Table _table;
        private readonly Action<string> _notifyChanged;
        private Row _createdRow;

        public override string Name => "Add Row";

        public AddRowCommand(Table table, Action<string> notifyChanged)
        {
            _table = table;
            _notifyChanged = notifyChanged;
        }

        public override void Execute()
        {
            if (_createdRow == null)
            {
                _createdRow = _table.AddRow();
            }
            else
            {
                _table.RestoreRow(_createdRow);
            }
            _notifyChanged?.Invoke(_table.ID);
        }

        public override void Undo()
        {
            if (_createdRow != null)
            {
                _table.RemoveRow(_createdRow.ID);
                _notifyChanged?.Invoke(_table.ID);
            }
        }
    }
}
