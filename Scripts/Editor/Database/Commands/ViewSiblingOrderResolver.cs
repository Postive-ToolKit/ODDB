using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Interfaces;
using TeamODD.ODDB.Runtime.Mutations;

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
            return ODDBMutations.TryResolveViewSiblingMove(
                database,
                viewId,
                oldSiblingIndex,
                newSiblingIndex,
                out repository,
                out oldRepositoryIndex,
                out newRepositoryIndex);
        }
    }
}
