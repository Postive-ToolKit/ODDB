using System;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Interfaces;

namespace TeamODD.ODDB.Editors.Commands
{
    public class RemoveFieldCommand : BaseCommand
    {
        private readonly IView _view;
        private readonly int _globalIndex;
        private readonly Action<string> _notifyChanged;
        private Field _removedField;

        public override string Name => "Remove Field";

        public RemoveFieldCommand(IView view, int globalIndex, Action<string> notifyChanged)
        {
            _view = view;
            _globalIndex = globalIndex;
            _notifyChanged = notifyChanged;
        }

        public override void Execute()
        {
            if (_globalIndex >= 0 && _globalIndex < _view.TotalFields.Count)
            {
                _removedField = _view.TotalFields[_globalIndex];
                _view.RemoveField(_globalIndex);
                _notifyChanged?.Invoke(_view.ID);
            }
        }

        public override void Undo()
        {
            if (_removedField != null)
            {
                _view.InsertField(_globalIndex, _removedField);
                _notifyChanged?.Invoke(_view.ID);
            }
        }
    }
}
