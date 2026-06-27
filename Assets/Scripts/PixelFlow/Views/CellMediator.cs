using Nexus.Core;
using PixelFlow.Signals;
using UnityEngine;

namespace PixelFlow.Views
{
    public class CellMediator : Mediator<CellView>
    {

        protected override void OnBind()
        {
        }

        protected override void OnUnbind()
        {
        }

        private void HandlePointerDown(Vector2Int pos)
        {
            SignalBus.Fire(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = pos });
        }

        private void HandlePointerDrag(Vector2Int pos)
        {
            SignalBus.Fire(new InputInteractionSignal { Type = InputType.Drag, GridPosition = pos });
        }

        private void HandlePointerUp(Vector2Int pos)
        {
            SignalBus.Fire(new InputInteractionSignal { Type = InputType.PointerUp, GridPosition = pos });
        }
    }
}
