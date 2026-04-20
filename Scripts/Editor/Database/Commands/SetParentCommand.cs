using System;
using TeamODD.ODDB.Runtime.Interfaces;

namespace TeamODD.ODDB.Editors.Commands
{
    public class SetParentCommand : BaseCommand
    {
        private readonly IView _view;
        private readonly IView _oldParent;
        private readonly IView _newParent;
        private readonly Action<string> _notifyChanged;

        public override string Name => "Set Parent View";

        public SetParentCommand(IView view, IView newParent, Action<string> notifyChanged)
        {
            _view = view;
            _newParent = newParent;
            _oldParent = view.ParentView;
            _notifyChanged = notifyChanged;
        }

        public override void Execute()
        {
            _view.ParentView = _newParent;
            _notifyChanged?.Invoke(_view.ID);
        }

        public override void Undo()
        {
            _view.ParentView = _oldParent;
            _notifyChanged?.Invoke(_view.ID);
        }
    }
}
