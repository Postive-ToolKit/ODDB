using System.Collections.Generic;
using System.Linq;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Interfaces;
using TeamODD.ODDB.Runtime.Utils.Converters;

namespace TeamODD.ODDB.Editors.Commands
{
    public static class ViewSiblingOrderResolver
    {
        public static bool TryResolveMove(
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
            var siblings = all
                .Where(candidate => HasSameParent(candidate, view))
                .ToList();

            if (oldSiblingIndex < 0 || oldSiblingIndex >= siblings.Count)
                return false;
            if (newSiblingIndex < 0 || newSiblingIndex >= siblings.Count)
                return false;
            if (oldSiblingIndex == newSiblingIndex)
                return false;
            if (!string.Equals(siblings[oldSiblingIndex].ID.ToString(), viewId))
                return false;

            oldRepositoryIndex = IndexOf(all, siblings[oldSiblingIndex]);
            newRepositoryIndex = IndexOf(all, siblings[newSiblingIndex]);
            return oldRepositoryIndex >= 0 && newRepositoryIndex >= 0;
        }

        private static bool HasSameParent(IView candidate, IView target)
        {
            var candidateParentId = candidate.ParentView?.ID.ToString();
            var targetParentId = target.ParentView?.ID.ToString();
            return string.Equals(candidateParentId, targetParentId);
        }

        private static int IndexOf(IReadOnlyList<IView> views, IView target)
        {
            for (var i = 0; i < views.Count; i++)
            {
                if (views[i].ID == target.ID)
                    return i;
            }
            return -1;
        }
    }
}
