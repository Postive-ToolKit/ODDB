using System;
using TeamODD.ODDB.Runtime;

namespace TeamODD.ODDB.Editors.Commands
{
    public sealed class SetRowIdCommand : BaseCommand
    {
        private readonly Table _table;
        private readonly string _oldId;
        private readonly string _newId;
        private readonly Action<string> _notifyChanged;
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
            if (!_captured || SameId(_oldId, _newId))
                return;

            ReKey(_newId, _oldId);
        }

        private void CaptureOriginal()
        {
            if (_captured)
                return;

            _captured = _table?.GetRow(_oldId) != null;
        }

        private void ReKey(string fromId, string toId)
        {
            if (_table != null && _table.RekeyRow(fromId, toId))
                _notifyChanged?.Invoke(_table.ID.ToString());
        }

        private static bool SameId(string first, string second)
        {
            return string.Equals(first, second, StringComparison.Ordinal);
        }
    }
}
