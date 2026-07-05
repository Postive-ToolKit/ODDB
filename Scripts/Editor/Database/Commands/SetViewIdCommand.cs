using System;
using System.Linq;
using TeamODD.ODDB.Runtime.Interfaces;
using TeamODD.ODDB.Runtime.Utils.Converters;

namespace TeamODD.ODDB.Editors.Commands
{
    public sealed class SetViewIdCommand : BaseCommand
    {
        private readonly IRepository<IView> _repository;
        private readonly ODDBID _oldId;
        private readonly ODDBID _newId;
        private readonly Action<string> _notifyChanged;
        private IView _view;
        private int _repositoryIndex = -1;
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
            if (!_captured || _view == null || SameId(_oldId, _newId))
                return;

            ReKey(_newId, _oldId);
            Notify(_newId, _oldId);
        }

        private void CaptureOriginal()
        {
            if (_captured)
                return;

            _view = _repository.Read(_oldId);
            _repositoryIndex = IndexOf(_view);
            _captured = true;
        }

        private void ReKey(ODDBID fromId, ODDBID toId)
        {
            var view = _repository.Read(fromId);
            if (view == null)
                return;

            _repository.Delete(fromId);
            view.ID = toId;
            _repository.Update(toId, view);
            RestoreRepositoryIndex(view);
        }

        private void RestoreRepositoryIndex(IView view)
        {
            if (_repositoryIndex < 0)
                return;

            var currentIndex = IndexOf(view);
            if (currentIndex < 0 || currentIndex == _repositoryIndex)
                return;

            _repository.Move(currentIndex, _repositoryIndex);
        }

        private int IndexOf(IView view)
        {
            if (view == null)
                return -1;
            return _repository.GetAll().ToList().IndexOf(view);
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
