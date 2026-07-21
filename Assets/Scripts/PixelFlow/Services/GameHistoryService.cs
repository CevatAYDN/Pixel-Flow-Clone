using System.Collections.Generic;
using Nexus.Core;
using PixelFlow.Models;
using System.Threading;
using System.Threading.Tasks;

namespace PixelFlow.Services
{
    /// <summary>
    /// Undo/Redo yığınları. MaxDepth aşılınca en eski snapshot atılır.
    /// Test edilebilirlik için interface üzerinden bağlanır.
    /// Grid state + GameSessionModel state'ini birlikte yönetir.
    /// </summary>
    public sealed class GameHistoryService : IGameHistoryService, INexusService
    {
        private const int DefaultMaxDepth = 200;

        private readonly int _maxDepth;
        private readonly LinkedList<GridSnapshot> _undoStack = new LinkedList<GridSnapshot>();
        private readonly LinkedList<GridSnapshot> _redoStack = new LinkedList<GridSnapshot>();

        public int UndoCount => _undoStack.Count;
        public int RedoCount => _redoStack.Count;
        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        [Inject]
        public GameHistoryService() : this(DefaultMaxDepth) { }

        // DI dışı manuel oluşturma (testler vb.) için internal bırakıldı.
        internal GameHistoryService(int maxDepth)
        {
            _maxDepth = maxDepth > 0 ? maxDepth : DefaultMaxDepth;
        }

        public void Record(IGridModel grid)
        {
            Record(grid, null);
        }

        public void Record(IGridModel grid, IGameSessionModel session)
        {
            var snapshot = GridSnapshot.Capture(grid, session);
            _undoStack.AddLast(snapshot);
            _redoStack.Clear();

            // Kapasite aşımı: en eski snapshot'ı at
            if (_undoStack.Count > _maxDepth)
            {
                _undoStack.RemoveFirst();
            }
        }

        public bool Undo(IGridModel grid)
        {
            return Undo(grid, null);
        }

        public bool Undo(IGridModel grid, IGameSessionModel session)
        {
            if (_undoStack.Count == 0) return false;

            // Mevcut state'i redo'ya taşı
            var currentSnapshot = GridSnapshot.Capture(grid, session);
            _redoStack.AddLast(currentSnapshot);

            // Son snapshot'ı geri yükle
            var restore = _undoStack.Last.Value;
            _undoStack.RemoveLast();
            restore.ApplyTo(grid);
            if (session != null)
            {
                restore.ApplySessionTo(session);
            }

            return true;
        }

        public bool Redo(IGridModel grid)
        {
            return Redo(grid, null);
        }

        public bool Redo(IGridModel grid, IGameSessionModel session)
        {
            if (_redoStack.Count == 0) return false;

            // Mevcut state'i undo'ya taşı
            var currentSnapshot = GridSnapshot.Capture(grid, session);
            _undoStack.AddLast(currentSnapshot);

            // Son redo snapshot'ını geri yükle
            var restore = _redoStack.Last.Value;
            _redoStack.RemoveLast();
            restore.ApplyTo(grid);
            if (session != null)
            {
                restore.ApplySessionTo(session);
            }

            return true;
        }

        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
        }

        public ValueTask InitializeAsync(CancellationToken ct) => default;
        public void OnDispose() { Clear(); }
    }
}
