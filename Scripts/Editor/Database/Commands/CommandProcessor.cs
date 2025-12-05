using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TeamODD.ODDB.Editors.Commands
{
    /// <summary>
    /// Manages the execution, undo, and redo of commands in the ODDB Editor.
    /// Maintains undo/redo stacks and handles history navigation.
    /// </summary>
    public class CommandProcessor
    {
        // LinkedList is used for UndoStack to efficiently remove the oldest item when the limit is reached.
        private readonly LinkedList<ICommand> _undoStack = new LinkedList<ICommand>();
        private readonly Stack<ICommand> _redoStack = new Stack<ICommand>();
        
        /// <summary>
        /// Maximum number of history items to keep in the undo stack.
        /// </summary>
        public int MaxHistoryCount { get; set; } = 50;

        /// <summary>
        /// Event triggered when the command history changes (execute, undo, redo, clear).
        /// </summary>
        public event Action OnHistoryChanged;

        /// <summary>
        /// Executes a command and pushes it to the undo stack.
        /// Clears the redo stack.
        /// </summary>
        public void Execute(ICommand command)
        {
            command.ExecutionTime = DateTime.Now;
            command.Execute();
            
            _undoStack.AddFirst(command); // Push to top
            _redoStack.Clear();
            
            // Enforce history limit
            while (_undoStack.Count > MaxHistoryCount)
            {
                _undoStack.RemoveLast(); // Remove oldest
            }
            
            OnHistoryChanged?.Invoke();
        }

        /// <summary>
        /// Undoes the last executed command.
        /// </summary>
        public void Undo()
        {
            if (_undoStack.Count == 0) return;

            var command = _undoStack.First.Value; // Peek
            _undoStack.RemoveFirst(); // Pop
            
            command.Undo();
            _redoStack.Push(command);
            
            OnHistoryChanged?.Invoke();
            Debug.Log($"[ODDB] Undo: {command.Name}");
        }

        /// <summary>
        /// Redoes the last undone command.
        /// </summary>
        public void Redo()
        {
            if (_redoStack.Count == 0) return;

            var command = _redoStack.Pop();
            command.Execute();
            _undoStack.AddFirst(command);
            
            OnHistoryChanged?.Invoke();
            Debug.Log($"[ODDB] Redo: {command.Name}");
        }
        
        /// <summary>
        /// Clears all history stacks.
        /// </summary>
        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            OnHistoryChanged?.Invoke();
        }

        /// <summary>
        /// Jumps to the state AFTER the execution of the target command.
        /// If target is null, jump to initial state (Undo All).
        /// </summary>
        /// <param name="target">The command to jump to. It must be in either stack.</param>
        public void JumpTo(ICommand target)
        {
            if (target == null) // Undo All (Initial State)
            {
                while (_undoStack.Count > 0)
                    Undo();
                return;
            }

            // If target is in Undo stack (Current or Past state)
            if (_undoStack.Contains(target))
            {
                // Undo until target is at the top of Undo stack (meaning it's the last executed command)
                while (_undoStack.Count > 0 && _undoStack.First.Value != target)
                {
                    Undo();
                }
            }
            // If target is in Redo stack (Future state)
            else if (_redoStack.Contains(target))
            {
                // Redo until target is moved to Undo stack
                while (_redoStack.Count > 0)
                {
                    var cmd = _redoStack.Peek();
                    Redo();
                    if (cmd == target) break;
                }
            }
        }

        public IEnumerable<ICommand> GetUndoList() => _undoStack;
        public IEnumerable<ICommand> GetRedoList() => _redoStack;
    }
}