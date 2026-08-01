using System;
using System.Collections.Generic;
using TeamODD.ODDB.Runtime.DTO;
using TeamODD.ODDB.Runtime.DTO.Builders;
using TeamODD.ODDB.Runtime.Interfaces;
using TeamODD.ODDB.Runtime.Mutations;
using TeamODD.ODDB.Runtime.Utils.Converters;

namespace TeamODD.ODDB.Runtime
{
    public class View : IView
    {
        public event Action OnFieldsChanged;

        public event Action<Field> OnFieldAdded;

        public event Action<int, int> OnFieldMoved;

        private const string DEFAULT_NAME = "Default Name";
        public ODDBID ID { get; set; }
        public string Name { get; set; }
        public Type BindType { get; set; }

        public IView ParentView
        {
            get => _parentView;
            set
            {
                if (value == this)
                    throw new InvalidOperationException("Cannot set parent view to itself.");
                // Cycle defense: walk `value`'s ancestor chain. If `this` appears in it,
                // the assignment would form a cycle (this → value → … → this) and any
                // future `TotalFields` / `IsChildOf` traversal would stack-overflow.
                // Also catch pre-existing cycles in value's chain — refuse rather than
                // propagate the bad state into this view. Refusal is logged but not
                // thrown so that database load can continue with the cycle isolated.
                if (value != null && DetectsCycleIfAssigned(value))
                {
                    ODDB.Logger.Warn(
                        $"[ODDB] Refusing cyclic ParentView assignment on view '{Name}' ({ID}) → target '{value.Name}' ({value.ID}). " +
                        "Parent link left unset to keep the database loadable.");
                    return;
                }
                if (_parentView != null)
                {
                    _parentView.OnFieldsChanged -= NotifyFieldsChanged;
                    _parentView.OnFieldAdded -= NotifyFieldAdded;
                    _parentView.OnFieldMoved -= NotifyFieldMoved;
                }
                _parentView = value;
                NotifyFieldsChanged();
                if (_parentView == null)
                    return;
                _parentView.OnFieldsChanged += NotifyFieldsChanged;
                _parentView.OnFieldAdded += NotifyFieldAdded;
                _parentView.OnFieldMoved += NotifyFieldMoved;

                if (BindType == null)
                {
                    BindType = _parentView.BindType;
                    return;
                }
                if (_parentView.BindType == null)
                    return;
                if (_parentView.BindType == BindType || BindType.IsSubclassOf(_parentView.BindType))
                    return;
                BindType = _parentView.BindType;
            }
        }

        protected ODDBID _parentViewKey;

        private IView _parentView;

        public List<Field> TotalFields
        {
            get
            {
                // Iterative walk with cycle detection. The pre-v2.0.15 form was a
                // recursive `ParentView.TotalFields` call that stack-overflowed on
                // cyclic parent chains. Now we accumulate the ancestor chain top-down
                // into a stack, stop on revisit, then flatten root-to-leaf.
                if (_parentView == null)
                    return _fields;

                var chain = new List<IHasFields>();
                var visited = new HashSet<ODDBID>();
                IView cursor = this;
                while (cursor != null && visited.Add(cursor.ID))
                {
                    chain.Add(cursor);
                    cursor = cursor.ParentView;
                }
                // If we exited via a revisit, the chain holds the loop-free prefix —
                // safe to return its concatenation without recursion.
                var result = new List<Field>();
                for (int i = chain.Count - 1; i >= 0; i--)
                    result.AddRange(chain[i].ScopedFields);
                return result;
            }
        }
        public List<Field> ScopedFields => _fields;
        private readonly List<Field> _fields = new();

        public View()
        {
            ID = new ODDBID();
            Name = DEFAULT_NAME;
        }
        public View(IEnumerable<Field> tableMetas = null)
        {
            if (tableMetas == null)
                return;
            _fields.AddRange(tableMetas);
        }

        public void AddField(Field field)
        {
            _fields.Add(field);
            OnAddField(field);
            OnFieldAdded?.Invoke(field);
            OnFieldsChanged?.Invoke();
        }

        public void InsertField(int index, Field field)
        {
            int myStartIndex = 0;
            if (ParentView != null)
                myStartIndex = ParentView.TotalFields.Count;

            int scopedIndex = index - myStartIndex;

            if (scopedIndex < 0 || scopedIndex > _fields.Count)
            {
                ODDB.Logger.Error($"Index {index} is out of range for insertion in this view.");
                return;
            }

            _fields.Insert(scopedIndex, field);
            OnAddField(field);
            OnFieldAdded?.Invoke(field);
            OnFieldsChanged?.Invoke();
        }

        public void RemoveField(int index)
        {
            if (!IsScopedField(index))
            {
                ODDB.Logger.Error($"Index {index} is out of range for this view.");
                return;
            }
            _fields.RemoveAt(ConvertToScopedIndex(index));
            OnRemoveField(index);
            OnFieldsChanged?.Invoke();
        }

        public void MoveField(int oldIndex, int newIndex)
        {
            if (!IsScopedField(oldIndex) || !IsScopedField(newIndex))
            {
                ODDB.Logger.Error($"Index {oldIndex} or {newIndex} is out of range for this view.");
                return;
            }
            var scopedOldIndex = ConvertToScopedIndex(oldIndex);
            var scopedNewIndex = ConvertToScopedIndex(newIndex);

            var item = _fields[scopedOldIndex];
            _fields.RemoveAt(scopedOldIndex);
            _fields.Insert(scopedNewIndex, item);

            NotifyFieldMoved(oldIndex, newIndex);
            NotifyFieldsChanged();
        }

        public bool SetFieldType(int fieldIndex, string typeKey, string param)
        {
            return ODDBMutations.SetFieldType(this, fieldIndex, typeKey, param);
        }

        public bool IsScopedField(int index)
        {
            if (ParentView == null)
            {
                return index >= 0 && index < _fields.Count;
            }
            var parentTableMetas = ParentView.TotalFields;
            return index >= parentTableMetas.Count && index < parentTableMetas.Count + _fields.Count;
        }

        private int ConvertToScopedIndex(int index)
        {
            if (ParentView == null)
                return index;
            var parentTableMetas = ParentView.TotalFields;
            return index - parentTableMetas.Count;
        }

        #region Virtual Event Methods
        protected virtual void OnAddField(Field field) { }
        protected virtual void OnRemoveField(int index) { }
        protected virtual void OnMoveField(int oldIndex, int newIndex) { }

        public virtual void OnDatabaseInitialize(ODDatabase database)
        {
            ParentView = database.Views.Read(_parentViewKey);
            database.OnDataChanged += OnDatabaseDataChanged;
            database.OnDataRemoved += OnDatabaseDataRemoved;
        }

        protected virtual void OnDatabaseDataChanged(ODDBID id)
        {
            if (ParentView != null && id == ParentView.ID)
                ParentView = ParentView;
        }

        protected virtual void OnDatabaseDataRemoved(ODDBID id) { }
        #endregion

        public bool IsChildOf(string viewId)
        {
            // Iterative walk with cycle detection (replaces recursive form).
            if (string.IsNullOrEmpty(viewId)) return false;
            var visited = new HashSet<ODDBID>();
            var cursor = ParentView;
            while (cursor != null && visited.Add(cursor.ID))
            {
                if (cursor.ID == viewId) return true;
                cursor = cursor.ParentView;
            }
            return false;
        }

        /// <summary>
        /// Returns true if assigning <paramref name="candidate"/> as ParentView would
        /// form a cycle (this view would become its own ancestor) or if
        /// <paramref name="candidate"/>'s existing ancestor chain already contains a cycle.
        /// </summary>
        private bool DetectsCycleIfAssigned(IView candidate)
        {
            var visited = new HashSet<ODDBID>();
            var cursor = candidate;
            while (cursor != null)
            {
                if (cursor.ID == ID) return true;
                if (!visited.Add(cursor.ID)) return true;
                cursor = cursor.ParentView;
            }
            return false;
        }

        public virtual ViewDTO ToDTO()
        {
            var dtoBuilder = new ViewDTOBuilder();
            var viewDto = dtoBuilder
                .SetName(this)
                .SetID(this)
                .SetTableMeta(this)
                .SetBindType(this)
                .SetParentView(this)
                .Build();

            return viewDto;
        }

        public virtual void FromDTO(ViewDTO dto)
        {
            if (dto == null)
                return;

            ID = new ODDBID(dto.ID);
            Name = dto.Name;
            BindType = ODDBTypeUtility.TryConvertBindType(dto.BindType, out var bindType) ? bindType : null;

            _parentViewKey = new ODDBID(dto.ParentView);
            ScopedFields.Clear();
            ScopedFields.AddRange(dto.TableMetas);
            ODDBConverter.OnDatabaseCreated.Add(new DataBaseCreateEvent
            {
                Priority = DataCreateProcess.ViewFieldInfo,
                OnEvent = OnDatabaseInitialize
            });
        }

        public void NotifyFieldsChanged()
        {
            OnFieldsChanged?.Invoke();
        }

        public void NotifyFieldAdded(Field field)
        {
            OnFieldAdded?.Invoke(field);
        }

        public void NotifyFieldMoved(int oldIndex, int newIndex)
        {
            OnMoveField(oldIndex, newIndex);
            OnFieldMoved?.Invoke(oldIndex, newIndex);
        }
    }
}
