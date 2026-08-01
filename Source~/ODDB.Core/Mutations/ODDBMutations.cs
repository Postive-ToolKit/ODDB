using System;
using System.Collections.Generic;
using System.Linq;
using TeamODD.ODDB.Runtime.Interfaces;
using TeamODD.ODDB.Runtime.Utils.Converters;

namespace TeamODD.ODDB.Runtime.Mutations
{
    /// <summary>
    /// Engine-agnostic mutations whose ordering and identity rules must be shared by
    /// every ODDB editing surface.
    /// </summary>
    public static class ODDBMutations
    {
        public static bool SetFieldType(IView view, int fieldIndex, string typeKey, string param)
        {
            if (view == null || fieldIndex < 0 || fieldIndex >= view.TotalFields.Count)
                return false;

            var field = view.TotalFields[fieldIndex];
            if (field == null)
                return false;

            if (field.Type == null)
                field.Type = new FieldType();
            field.Type.TypeKey = typeKey ?? string.Empty;
            field.Type.Param = param ?? string.Empty;
            view.NotifyFieldsChanged();
            return true;
        }

        public static bool RekeyRepositoryItem<T>(
            IRepository<T> repository,
            ODDBID oldId,
            ODDBID newId)
            where T : IHasODDBID
        {
            if (repository == null || oldId == null || newId == null)
                return false;
            if (string.Equals(oldId.ToString(), newId.ToString(), StringComparison.Ordinal))
                return false;
            if (repository.Read(newId) != null)
                return false;

            var item = repository.Read(oldId);
            if (item == null)
                return false;

            var originalIndex = IndexOf(repository.GetAll(), item);
            repository.Delete(oldId);
            item.ID = newId;
            repository.Update(newId, item);

            var currentIndex = IndexOf(repository.GetAll(), item);
            if (originalIndex >= 0 && currentIndex >= 0 && currentIndex != originalIndex)
                repository.Move(currentIndex, originalIndex);

            return true;
        }

        public static bool TryResolveViewSiblingMove(
            ODDatabase database,
            string viewId,
            int oldSiblingIndex,
            int newSiblingIndex,
            out IRepository<IView> repository,
            out int oldRepositoryIndex,
            out int newRepositoryIndex)
        {
            repository = null;
            oldRepositoryIndex = -1;
            newRepositoryIndex = -1;

            if (database == null || string.IsNullOrEmpty(viewId))
                return false;

            var view = database.GetView(new ODDBID(viewId));
            if (view == null)
                return false;

            repository = view is Table ? database.Tables : database.Views;
            var all = repository.GetAll();
            var siblings = all.Where(candidate => HasSameParent(candidate, view)).ToList();

            if (oldSiblingIndex < 0 || oldSiblingIndex >= siblings.Count)
                return false;
            if (newSiblingIndex < 0 || newSiblingIndex >= siblings.Count)
                return false;
            if (oldSiblingIndex == newSiblingIndex)
                return false;
            if (!string.Equals(siblings[oldSiblingIndex].ID.ToString(), viewId, StringComparison.Ordinal))
                return false;

            oldRepositoryIndex = IndexOf(all, siblings[oldSiblingIndex]);
            newRepositoryIndex = IndexOf(all, siblings[newSiblingIndex]);
            return oldRepositoryIndex >= 0 && newRepositoryIndex >= 0;
        }

        public static bool MoveViewSibling(
            ODDatabase database,
            string viewId,
            int oldSiblingIndex,
            int newSiblingIndex)
        {
            if (!TryResolveViewSiblingMove(
                    database,
                    viewId,
                    oldSiblingIndex,
                    newSiblingIndex,
                    out var repository,
                    out var oldRepositoryIndex,
                    out var newRepositoryIndex))
                return false;

            repository.Move(oldRepositoryIndex, newRepositoryIndex);
            return true;
        }

        private static bool HasSameParent(IView candidate, IView target)
        {
            var candidateParentId = candidate.ParentView?.ID.ToString();
            var targetParentId = target.ParentView?.ID.ToString();
            return string.Equals(candidateParentId, targetParentId, StringComparison.Ordinal);
        }

        private static int IndexOf<T>(IReadOnlyList<T> items, T target)
        {
            for (var i = 0; i < items.Count; i++)
            {
                if (EqualityComparer<T>.Default.Equals(items[i], target))
                    return i;
            }
            return -1;
        }
    }
}
