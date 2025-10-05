using System;

namespace TeamODD.ODDB.Runtime.Interfaces
{
    public interface IFieldsHandler
    {
        public event Action OnFieldsChanged;
        
        public event Action<Field> OnFieldAdded;
        
        void AddField(Field field);
        void RemoveField(int index);
        void SwapFields(int indexA, int indexB);
        bool IsScopedField(int index);
        
        public void NotifyFieldsChanged();
        
        public void NotifyFieldAdded(Field field);
    }
}