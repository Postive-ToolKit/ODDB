using System;
using TeamODD.ODDB.Runtime.Interfaces;
using TeamODD.ODDB.Runtime.Mutations;
using TeamODD.ODDB.Runtime.Utils.Converters;

namespace TeamODD.ODDB.Editors.Commands
{
    public sealed class SetViewIdCommand : BaseCommand
    {
        private readonly IRepository<IView> _repository;
        private readonly ODDBID _oldId;
        private readonly ODDBID _newId;
        private readonly Action<string> _notifyChanged;
        private bool _captured;

        public override string Name => "Set View ID";

        public SetViewIdCommand(
            IRepository<IView> repository,
            ODDBID oldId,
            ODDBID newId,
            Action<string> notifyChanged)
        {
            _repository = repository;
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
            Notify(_oldId, _newId);
        }

        public override void Undo()
        {
            if (!_captured || SameId(_oldId, _newId))
                return;

            ReKey(_newId, _oldId);
            Notify(_newId, _oldId);
        }

        private void CaptureOriginal()
        {
            if (_captured)
                return;

            _captured = _repository.Read(_oldId) != null;
        }

        private void ReKey(ODDBID fromId, ODDBID toId)
        {
            ODDBMutations.RekeyRepositoryItem(_repository, fromId, toId);
        }

        private void Notify(ODDBID staleId, ODDBID activeId)
        {
            _notifyChanged?.Invoke(staleId.ToString());
            _notifyChanged?.Invoke(activeId.ToString());
        }

        private static bool SameId(ODDBID first, ODDBID second)
        {
            return string.Equals(first?.ToString(), second?.ToString(), StringComparison.Ordinal);
        }
    }
}
