using System;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Interfaces;

namespace TeamODD.ODDB.Editors.Commands
{
    public class AddFieldCommand : BaseCommand
    {
        private readonly IView _view;
        private readonly Field _field;
        private readonly Action<string> _notifyChanged;

        public override string Name => "Add Field";

        public AddFieldCommand(IView view, Field field, Action<string> notifyChanged)
        {
            _view = view;
            _field = field;
            _notifyChanged = notifyChanged;
        }

        public override void Execute()
        {
            _view.AddField(_field);
            _notifyChanged?.Invoke(_view.ID);
        }

        public override void Undo()
        {
            int scopedIndex = _view.ScopedFields.IndexOf(_field);
            if (scopedIndex != -1)
            {
                int globalIndex = scopedIndex;
                if (_view.ParentView != null)
                    globalIndex += _view.ParentView.TotalFields.Count;

                _view.RemoveField(globalIndex);
                _notifyChanged?.Invoke(_view.ID);
            }
        }
    }
}
