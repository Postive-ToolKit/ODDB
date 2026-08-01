using System;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Interfaces;
using TeamODD.ODDB.Runtime.Mutations;

namespace TeamODD.ODDB.Editors.Commands
{
    /// <summary>
    /// Changes a field's type key and param without touching cell data so existing
    /// rows keep their serialized strings — handy when recovering from a corrupted
    /// schema where every field's _typeKey was reset to "string".
    /// </summary>
    public class SetFieldTypeCommand : BaseCommand
    {
        private readonly IView _view;
        private readonly int _fieldIndex;
        private readonly string _newTypeKey;
        private readonly string _newParam;
        private readonly Action<string> _notifyChanged;

        private string _oldTypeKey;
        private string _oldParam;
        private bool _captured;

        public override string Name => "Set Field Type";

        public SetFieldTypeCommand(IView view, int fieldIndex, string newTypeKey, string newParam, Action<string> notifyChanged)
        {
            _view = view;
            _fieldIndex = fieldIndex;
            _newTypeKey = newTypeKey ?? string.Empty;
            _newParam = newParam ?? string.Empty;
            _notifyChanged = notifyChanged;
        }

        public override void Execute()
        {
            var field = ResolveField();
            if (field == null) return;
            if (field.Type == null) field.Type = new FieldType();

            if (!_captured)
            {
                _oldTypeKey = field.Type.TypeKey;
                _oldParam = field.Type.Param;
                _captured = true;
            }

            if (ODDBMutations.SetFieldType(_view, _fieldIndex, _newTypeKey, _newParam))
                _notifyChanged?.Invoke(_view.ID);
        }

        public override void Undo()
        {
            if (!_captured) return;
            var field = ResolveField();
            if (field == null) return;
            if (ODDBMutations.SetFieldType(_view, _fieldIndex, _oldTypeKey, _oldParam))
                _notifyChanged?.Invoke(_view.ID);
        }

        private Field ResolveField()
        {
            if (_view == null) return null;
            if (_fieldIndex < 0 || _fieldIndex >= _view.TotalFields.Count) return null;
            return _view.TotalFields[_fieldIndex];
        }
    }
}
