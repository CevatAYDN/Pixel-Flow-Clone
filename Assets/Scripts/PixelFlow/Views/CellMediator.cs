using Nexus.Core;

namespace PixelFlow.Views
{
    // CellMediator artık input üretmiyor; tüm input GridView.Update'ten geliyor.
    // Buradaki tek sorumluluk: View'in yaşam döngüsünü yönetmek.
    // İleride CellView'in model değişikliklerini dinlemesi gerekirse buraya Subscribe<>() eklenebilir.
    public class CellMediator : Mediator<CellView>
    {
        protected override void OnBind()
        {
        }

        protected override void OnUnbind()
        {
        }
    }
}
