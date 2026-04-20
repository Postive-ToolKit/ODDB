using System;
using TeamODD.ODDB.Runtime.Interfaces;
using TeamODD.ODDB.Runtime.Utils.Converters;

namespace TeamODD.ODDB.Editors.Commands
{
    public class RemoveViewItemCommand : BaseCommand
    {
        private readonly IRepository<IView> _repo;
        private readonly ODDBID _id;
        private readonly string _displayName;
        private readonly Action<string> _notifyChanged;
        private IView _removedItem;

        public override string Name => _displayName;

        public RemoveViewItemCommand(IRepository<IView> repo, ODDBID id, string displayName, Action<string> notifyChanged)
        {
            _repo = repo;
            _id = id;
            _displayName = displayName;
            _notifyChanged = notifyChanged;
        }

        public override void Execute()
        {
            _removedItem = _repo.Read(_id);
            if (_removedItem == null)
                return;
            _repo.Delete(_id);
            _notifyChanged?.Invoke(_id);
        }

        public override void Undo()
        {
            if (_removedItem == null)
                return;
            _repo.Update(_id, _removedItem);
            _notifyChanged?.Invoke(_id);
        }
    }
}
