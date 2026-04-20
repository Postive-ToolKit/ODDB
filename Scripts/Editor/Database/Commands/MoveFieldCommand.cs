using System;
using TeamODD.ODDB.Runtime.Interfaces;

namespace TeamODD.ODDB.Editors.Commands
{
    public class MoveFieldCommand : BaseCommand
    {
        private readonly IView _view;
        private readonly int _oldIndex;
        private readonly int _newIndex;
        private readonly Action<string> _notifyChanged;

        public override string Name => "Move Field";

        public MoveFieldCommand(IView view, int oldIndex, int newIndex, Action<string> notifyChanged)
        {
            _view = view;
            _oldIndex = oldIndex;
            _newIndex = newIndex;
            _notifyChanged = notifyChanged;
        }

        public override void Execute()
        {
            _view.MoveField(_oldIndex, _newIndex);
            _notifyChanged?.Invoke(_view.ID);
        }

        public override void Undo()
        {
            _view.MoveField(_newIndex, _oldIndex);
            _notifyChanged?.Invoke(_view.ID);
        }
    }
}
