using System;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Interfaces;

namespace TeamODD.ODDB.Editors.Commands
{
    public abstract class BaseCommand : ICommand
    {
        public abstract string Name { get; }
        public DateTime ExecutionTime { get; set; }
        public abstract void Execute();
        public abstract void Undo();
    }

    public class SetViewNameCommand : BaseCommand
    {
        private readonly IView _view;
        private readonly string _oldName;
        private readonly string _newName;
        private readonly Action<string> _notifyChanged;

        public override string Name => "Set View Name";

        public SetViewNameCommand(IView view, string newName, Action<string> notifyChanged)
        {
            _view = view;
            _newName = newName;
            _oldName = view.Name;
            _notifyChanged = notifyChanged;
        }

        public override void Execute()
        {
            _view.Name = _newName;
            _notifyChanged?.Invoke(_view.ID);
        }

        public override void Undo()
        {
            _view.Name = _oldName;
            _notifyChanged?.Invoke(_view.ID);
        }
    }

    public class AddRowCommand : BaseCommand
    {
        private readonly Table _table;
        private readonly Action<string> _notifyChanged;
        private Row _createdRow;

        public override string Name => "Add Row";

        public AddRowCommand(Table table, Action<string> notifyChanged)
        {
            _table = table;
            _notifyChanged = notifyChanged;
        }

        public override void Execute()
        {
            if (_createdRow == null)
            {
                _createdRow = _table.AddRow();
            }
            else
            {
                _table.RestoreRow(_createdRow);
            }
            _notifyChanged?.Invoke(_table.ID);
        }

        public override void Undo()
        {
            if (_createdRow != null)
            {
                _table.RemoveRow(_createdRow.ID);
                _notifyChanged?.Invoke(_table.ID);
            }
        }
    }

    public class RemoveRowCommand : BaseCommand
    {
        private readonly Table _table;
        private readonly string _rowId;
        private readonly Action<string> _notifyChanged;
        private Row _removedRow;

        public override string Name => "Remove Row";

        public RemoveRowCommand(Table table, string rowId, Action<string> notifyChanged)
        {
            _table = table;
            _rowId = rowId;
            _notifyChanged = notifyChanged;
        }

        public override void Execute()
        {
            _removedRow = _table.GetRow(_rowId);
            _table.RemoveRow(_rowId);
            _notifyChanged?.Invoke(_table.ID);
        }

        public override void Undo()
        {
            if (_removedRow != null)
            {
                _table.RestoreRow(_removedRow);
                _notifyChanged?.Invoke(_table.ID);
            }
        }
    }

    public class AddFieldCommand : BaseCommand
    {
        private readonly IView _view;
        private readonly Field _field;
        private readonly Action<string> _notifyChanged;

        public override string Name => "Add Field";

        public AddFieldCommand(IView view, Field field, Action<string> notifyChanged)
        {
            _view = view;
            _field = field;
            _notifyChanged = notifyChanged;
        }

        public override void Execute()
        {
            _view.AddField(_field);
            _notifyChanged?.Invoke(_view.ID);
        }

        public override void Undo()
        {
            int scopedIndex = _view.ScopedFields.IndexOf(_field);
            if (scopedIndex != -1)
            {
                int globalIndex = scopedIndex;
                if (_view.ParentView != null)
                    globalIndex += _view.ParentView.TotalFields.Count;
                
                _view.RemoveField(globalIndex);
                _notifyChanged?.Invoke(_view.ID);
            }
        }
    }

    public class RemoveFieldCommand : BaseCommand
    {
        private readonly IView _view;
        private readonly int _globalIndex;
        private readonly Action<string> _notifyChanged;
        private Field _removedField;

        public override string Name => "Remove Field";

        public RemoveFieldCommand(IView view, int globalIndex, Action<string> notifyChanged)
        {
            _view = view;
            _globalIndex = globalIndex;
            _notifyChanged = notifyChanged;
        }

        public override void Execute()
        {
            if (_globalIndex >= 0 && _globalIndex < _view.TotalFields.Count)
            {
                _removedField = _view.TotalFields[_globalIndex];
                _view.RemoveField(_globalIndex);
                _notifyChanged?.Invoke(_view.ID);
            }
        }

        public override void Undo()
        {
            if (_removedField != null)
            {
                _view.InsertField(_globalIndex, _removedField);
                _notifyChanged?.Invoke(_view.ID);
            }
        }
    }

    public class SetBindTypeCommand : BaseCommand
    {
        private readonly IView _view;
        private readonly Type _oldType;
        private readonly Type _newType;
        private readonly Action<string> _notifyChanged;

        public override string Name => "Set Bind Type";

        public SetBindTypeCommand(IView view, Type newType, Action<string> notifyChanged)
        {
            _view = view;
            _newType = newType;
            _oldType = view.BindType;
            _notifyChanged = notifyChanged;
        }

        public override void Execute()
        {
            _view.BindType = _newType;
            _notifyChanged?.Invoke(_view.ID);
        }

        public override void Undo()
        {
            _view.BindType = _oldType;
            _notifyChanged?.Invoke(_view.ID);
        }
    }

    public class SetParentCommand : BaseCommand
    {
        private readonly IView _view;
        private readonly IView _oldParent;
        private readonly IView _newParent;
        private readonly Action<string> _notifyChanged;

        public override string Name => "Set Parent View";

        public SetParentCommand(IView view, IView newParent, Action<string> notifyChanged)
        {
            _view = view;
            _newParent = newParent;
            _oldParent = view.ParentView;
            _notifyChanged = notifyChanged;
        }

        public override void Execute()
        {
            _view.ParentView = _newParent;
            _notifyChanged?.Invoke(_view.ID);
        }

        public override void Undo()
        {
            _view.ParentView = _oldParent;
            _notifyChanged?.Invoke(_view.ID);
        }
    }
}