using System;

namespace TeamODD.ODDB.Runtime.Interfaces
{
    public interface IFieldsHandler
    {
        public event Action OnFieldsChanged;
        
        public event Action<Field> OnFieldAdded;

        public event Action<int, int> OnFieldMoved;
        
        void AddField(Field field);
        void RemoveField(int index);
        void InsertField(int index, Field field);
        void MoveField(int oldIndex, int newIndex);
        bool IsScopedField(int index);
        
        public void NotifyFieldsChanged();
        
        public void NotifyFieldAdded(Field field);
    }
}