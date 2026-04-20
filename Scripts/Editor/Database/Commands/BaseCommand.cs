using System;

namespace TeamODD.ODDB.Editors.Commands
{
    public abstract class BaseCommand : ICommand
    {
        public abstract string Name { get; }
        public DateTime ExecutionTime { get; set; }
        public abstract void Execute();
        public abstract void Undo();
    }
}
