namespace TeamODD.ODDB.Runtime.Interfaces
{
    /// <summary>
    /// Interface to represent an object that has a parent view.
    /// </summary>
    public interface IHasParentView
    {
        IView ParentView { get; set; }

        public bool IsChildOf(string viewId);
    }
}