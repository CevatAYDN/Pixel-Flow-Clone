// NOT: CellMediator kaldırıldı. CellView özel mediator mantığı gerektirmiyor.
// Bu dosya, eski sahne/prefab referanslarındaki "missing script" uyarılarını
// engellemek için tutuluyordu. Tüm referanslar temizlendikten sonra
// güvenle silinebilir.
//
// Silme komutu: rm Assets/Scripts/PixelFlow/Views/CellMediator.cs
//               rm Assets/Scripts/PixelFlow/Views/CellMediator.cs.meta
using Nexus.Core;

namespace PixelFlow.Views
{
    /// <summary>
    /// Boş stub — sadece Unity uyarılarını engellemek için.
    /// Tüm referanslar temizlenince bu dosyayı silin.
    /// </summary>
    public class CellMediator : Mediator<CellView>
    {
        protected override void OnBind() { }
        protected override void OnUnbind() { }
    }
}
