using System;

namespace TeamODD.ODDB.Editors.CodeGen
{
    [Serializable]
    internal sealed class PendingRemapEntry
    {
        public string viewId;
        public string className;
        public string queuedAt;

        public PendingRemapEntry() { }

        public PendingRemapEntry(string viewId, string className)
        {
            this.viewId = viewId;
            this.className = className;
            this.queuedAt = DateTime.UtcNow.ToString("o");
        }
    }

    [Serializable]
    internal sealed class PendingRemapPayload
    {
        public PendingRemapEntry[] entries = Array.Empty<PendingRemapEntry>();
    }
}
