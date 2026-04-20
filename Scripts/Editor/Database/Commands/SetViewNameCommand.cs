using System;
using TeamODD.ODDB.Runtime.Interfaces;

namespace TeamODD.ODDB.Editors.Commands
{
    public class SetViewNameCommand : BaseCommand
    {
        private readonly IView _view;
        private readonly string _oldName;
        private readonly string _newName;
        private readonly Action<string> _notifyChanged;

        public override string Name => "Set View Name";

        public SetViewNameCommand(IView view, string newName, Action<string> notifyChanged)
        {
            _view = view;
            _newName = newName;
            _oldName = view.Name;
            _notifyChanged = notifyChanged;
        }

        public override void Execute()
        {
            _view.Name = _newName;
            _notifyChanged?.Invoke(_view.ID);
        }

        public override void Undo()
        {
            _view.Name = _oldName;
            _notifyChanged?.Invoke(_view.ID);
        }
    }
}
