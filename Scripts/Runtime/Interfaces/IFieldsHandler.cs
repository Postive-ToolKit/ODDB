using System;

namespace TeamODD.ODDB.Runtime.Interfaces
{
    public interface IFieldsHandler
    {
        public event Action OnFieldsChanged;
        void AddField(Field field);
        void RemoveField(int index);
        void SwapFields(int indexA, int indexB);
        bool IsScopedField(int index);
        
        public void NotifyFieldsChanged();
    }
}