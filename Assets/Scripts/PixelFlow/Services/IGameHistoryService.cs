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
        /// Son snapshot'ı geri yükler. Başarılıysa true döner.
        /// </summary>
        bool Undo(IGridModel grid);

        /// <summary>
        /// Geri alınmış snapshot'ı yeniden uygular. Başarılıysa true döner.
        /// </summary>
        bool Redo(IGridModel grid);

        /// <summary>
        /// Tüm geçmişi temizler (yeni level yüklemesi vb.).
        /// </summary>
        void Clear();
    }
}
