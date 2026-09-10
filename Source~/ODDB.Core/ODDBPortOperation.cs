using System;
using System.Collections.Generic;
using System.Threading;

namespace TeamODD.ODDB.Runtime
{
    /// <summary>
    /// Incremental entity materialization operation.
    /// Core conversion and row hydration remain synchronous, but callers can
    /// bound the work per step and return control to an engine main loop.
    /// </summary>
    public sealed class ODDBPortOperation : IDisposable
    {
        private readonly ODDatabase _database;
        private readonly List<ODDatabase.PortWorkItem> _workItems;
        private int _workIndex;
        private int _rowIndex;
        private bool _isCompleted;
        private bool _isDisposed;

        internal ODDBPortOperation(ODDatabase database, List<ODDatabase.PortWorkItem> workItems, int totalRows)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _workItems = workItems ?? throw new ArgumentNullException(nameof(workItems));
            TotalRows = totalRows;
        }

        internal static ODDBPortOperation Completed(ODDatabase database)
        {
            var operation = new ODDBPortOperation(database, new List<ODDatabase.PortWorkItem>(), 0);
            operation._isCompleted = true;
            return operation;
        }

        public ODDatabase Database => _database;
        public int TotalRows { get; }
        public int ProcessedRows { get; private set; }
        public bool IsCompleted => _isCompleted;
        public float Progress => TotalRows == 0
            ? (_isCompleted ? 1f : 0f)
            : Math.Min(1f, (float)ProcessedRows / TotalRows);

        /// <summary>
        /// Materializes at most <paramref name="maxEntities"/> rows.
        /// Cancellation or a materialization failure discards partial caches.
        /// </summary>
        public int Step(int maxEntities, CancellationToken cancellationToken = default)
        {
            EnsureUsable();
            if (_isCompleted) return 0;
            if (maxEntities <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxEntities), "Step budget must be positive.");

            var materialized = 0;
            try
            {
                while (_workIndex < _workItems.Count && materialized < maxEntities)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var work = _workItems[_workIndex];
                    if (_rowIndex >= work.Rows.Count)
                    {
                        _workIndex++;
                        _rowIndex = 0;
                        continue;
                    }

                    var row = work.Rows[_rowIndex++];
                    _database.MaterializePortRow(work.TargetType, work.Fields, row);
                    ProcessedRows++;
                    materialized++;
                }

                cancellationToken.ThrowIfCancellationRequested();
                if (_workIndex >= _workItems.Count)
                {
                    _database.CompletePortData(this);
                    _isCompleted = true;
                }

                return materialized;
            }
            catch
            {
                _database.AbortPortData(this);
                _isCompleted = true;
                throw;
            }
        }

        /// <summary>
        /// Completes the operation synchronously using the largest practical step.
        /// This preserves the legacy ODDatabase.PortData behavior.
        /// </summary>
        public void Complete(CancellationToken cancellationToken = default)
        {
            while (!_isCompleted)
                Step(int.MaxValue, cancellationToken);
        }

        /// <summary>
        /// Aborts the operation and removes all partially materialized state.
        /// </summary>
        public void Cancel()
        {
            if (_isCompleted) return;
            _database.AbortPortData(this);
            _isCompleted = true;
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            Cancel();
        }

        private void EnsureUsable()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(ODDBPortOperation));
            if (_database.IsPorted && !_isCompleted)
                throw new InvalidOperationException("The database was ported by another operation.");
        }
    }
}
