// CellMediator removed -- CellView uses no custom mediator logic.
// This file retained as a stub to prevent Unity "missing script" warnings
// on scene/prefab references that were set before the mediator was removed.
// Can be fully deleted once all scene/prefab references are cleaned up.
using Nexus.Core;

namespace PixelFlow.Views
{
    public class CellMediator : Mediator<CellView>
    {
        protected override void OnBind() { }
    }
}
