using PixelFlow.Models;

namespace PixelFlow.Services
{
    /// <summary>
    /// Undo/Redo yığınlarını yönetir. Her mutasyon öncesi snapshot alınır.
    /// </summary>
    public interface IGameHistoryService
    {
        bool CanUndo { get; }
        bool CanRedo { get; }
        int UndoCount { get; }
        int RedoCount { get; }

        /// <summary>
        /// Mevcut grid state'ini snapshot'layıp undo stack'ine iter.
        /// Redo stack'i temizlenir (yeni mutasyon = yeni branching point).
        /// </summary>
        void Record(IGridModel grid);

        /// <summary>
        /// Grid + Session state'ini snapshot'layıp undo stack'ine iter.
        /// </summary>
        void Record(IGridModel grid, IGameSessionModel session);

        /// <summary>
        /// Son snapshot'ı geri yükler. Başarılıysa true döner.
        /// Grid + Session state'ini birlikte geri yükler.
        /// </summary>
        bool Undo(IGridModel grid);

        /// <summary>
        /// Son snapshot'ı geri yükler. Grid + Session state'ini birlikte geri yükler.
        /// </summary>
        bool Undo(IGridModel grid, IGameSessionModel session);

        /// <summary>
        /// Geri alınmış snapshot'ı yeniden uygular. Başarılıysa true döner.
        /// </summary>
        bool Redo(IGridModel grid);

        /// <summary>
        /// Geri alınmış snapshot'ı yeniden uygular. Grid + Session state'ini birlikte geri yükler.
        /// </summary>
        bool Redo(IGridModel grid, IGameSessionModel session);

        /// <summary>
        /// Tüm geçmişi temizler (yeni level yüklemesi vb.).
        /// </summary>
        void Clear();
    }
}
