using System;
using TeamODD.ODDB.Runtime.Interfaces;

namespace TeamODD.ODDB.Editors.Commands
{
    public sealed class MoveViewItemCommand : BaseCommand
    {
        private readonly IRepository<IView> _repository;
        private readonly int _oldIndex;
        private readonly int _newIndex;
        private readonly Action<string> _notifyChanged;
        private readonly string _viewId;

        public override string Name => "Move View Item";

        public MoveViewItemCommand(
            IRepository<IView> repository,
            int oldIndex,
            int newIndex,
            Action<string> notifyChanged)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _oldIndex = oldIndex;
            _newIndex = newIndex;
            _notifyChanged = notifyChanged;
            _viewId = repository.Read(oldIndex)?.ID.ToString();
        }

        public override void Execute()
        {
            _repository.Move(_oldIndex, _newIndex);
            if (!string.IsNullOrEmpty(_viewId))
                _notifyChanged?.Invoke(_viewId);
        }

        public override void Undo()
        {
            _repository.Move(_newIndex, _oldIndex);
            if (!string.IsNullOrEmpty(_viewId))
                _notifyChanged?.Invoke(_viewId);
        }
    }
}
