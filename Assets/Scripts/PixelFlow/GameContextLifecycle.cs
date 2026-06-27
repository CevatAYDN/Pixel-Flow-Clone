using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using PixelFlow.Models;

using UnityEngine;

namespace PixelFlow
{
    public class GameContextLifecycle : MonoBehaviour, IContextLifecycle
    {
        public void OnConfigure(IContextBuilder builder)
        {
            builder.BindModel<IGridModel, GridModel>();
            builder.BindModel<ILevelModel, LevelModel>();
            builder.BindModel<IProgressModel, ProgressModel>();
            builder.BindModel<IGameStateModel, GameStateModel>();
            builder.BindModel<IHintModel, HintModel>();
            builder.BindModel<ISettingsModel, SettingsModel>();
            builder.BindModel<ISoundModel, SoundModel>();

            builder.BindSignal<PixelFlow.Signals.InputInteractionSignal>().To<PixelFlow.Commands.ProcessInputCommand>();
            builder.BindSignal<PixelFlow.Signals.CheckWinConditionSignal>().To<PixelFlow.Commands.CheckWinConditionCommand>();
            builder.BindSignal<PixelFlow.Signals.LoadLevelSignal>().To<PixelFlow.Commands.LoadLevelCommand>();
            builder.BindSignal<PixelFlow.Signals.RequestHintSignal>().To<PixelFlow.Commands.UseHintCommand>();
            builder.BindSignal<PixelFlow.Commands.ChangeThemeSignal>().To<PixelFlow.Commands.ChangeThemeCommand>();
        }

        public ValueTask OnInitializeAsync(CancellationToken ct) => default;
        public ValueTask OnStartAsync(CancellationToken ct) => default;
        public void OnDispose() { }
    }
}
