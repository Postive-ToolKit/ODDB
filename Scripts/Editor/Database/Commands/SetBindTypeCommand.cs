using System;
using TeamODD.ODDB.Runtime.Interfaces;

namespace TeamODD.ODDB.Editors.Commands
{
    public class SetBindTypeCommand : BaseCommand
    {
        private readonly IView _view;
        private readonly Type _oldType;
        private readonly Type _newType;
        private readonly Action<string> _notifyChanged;

        public override string Name => "Set Bind Type";

        public SetBindTypeCommand(IView view, Type newType, Action<string> notifyChanged)
        {
            _view = view;
            _newType = newType;
            _oldType = view.BindType;
            _notifyChanged = notifyChanged;
        }

        public override void Execute()
        {
            _view.BindType = _newType;
            _notifyChanged?.Invoke(_view.ID);
        }

        public override void Undo()
        {
            _view.BindType = _oldType;
            _notifyChanged?.Invoke(_view.ID);
        }
    }
}
