using System;

namespace TeamODD.ODDB.Editors.Commands
{
    public interface ICommand
    {
        void Execute();
        void Undo();
        string Name { get; }
        DateTime ExecutionTime { get; set; }
    }
}