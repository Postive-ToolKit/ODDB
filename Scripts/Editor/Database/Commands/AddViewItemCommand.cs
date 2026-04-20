using System;
using TeamODD.ODDB.Runtime.Interfaces;
using TeamODD.ODDB.Runtime.Utils.Converters;

namespace TeamODD.ODDB.Editors.Commands
{
    public class AddViewItemCommand : BaseCommand
    {
        private readonly IRepository<IView> _repo;
        private readonly string _displayName;
        private readonly Action<string> _notifyChanged;
        private IView _createdItem;
        private ODDBID _createdId;

        public override string Name => _displayName;

        public AddViewItemCommand(IRepository<IView> repo, string displayName, Action<string> notifyChanged)
        {
            _repo = repo;
            _displayName = displayName;
            _notifyChanged = notifyChanged;
        }

        public override void Execute()
        {
            if (_createdItem == null)
            {
                _createdItem = _repo.Create();
                _createdId = _createdItem.ID;
            }
            else
            {
                _repo.Update(_createdId, _createdItem);
            }
            _notifyChanged?.Invoke(_createdId);
        }

        public override void Undo()
        {
            if (_createdId != null)
            {
                _repo.Delete(_createdId);
                _notifyChanged?.Invoke(_createdId);
            }
        }
    }
}
