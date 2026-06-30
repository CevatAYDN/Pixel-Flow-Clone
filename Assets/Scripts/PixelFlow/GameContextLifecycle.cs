using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using PixelFlow.Models;
using PixelFlow.Services;

using UnityEngine;

namespace PixelFlow
{
    public class GameContextLifecycle : MonoBehaviour, IContextLifecycle
    {
        public void OnConfigure(IContextBuilder builder)
        {
            // PlayerPrefs servisini singleton olarak bağla; kalıcı state kullanan tüm
            // modeller bunu constructor injection ile alır (test edilebilir).
            builder.Bind<IPlayerPrefsService, UnityPlayerPrefsService>();
            builder.Bind<IPathService, PathService>();
            builder.Bind<IGameHistoryService, GameHistoryService>();
            builder.Bind<IPathSolver, RuntimePathSolver>();
            builder.Bind<IHintService, HintService>();
            builder.Bind<ILevelProgressionService, LevelProgressionService>();

            builder.BindModel<IGridModel, GridModel>();
            builder.BindModel<ILevelModel, LevelModel>();
            builder.BindModel<IProgressModel, ProgressModel>();
            builder.BindModel<IGameStateModel, GameStateModel>();
            builder.BindModel<IGameSessionModel, GameSessionModel>();
            builder.BindModel<IHintModel, HintModel>();
            builder.BindModel<ISettingsModel, SettingsModel>();
            builder.BindModel<ISoundModel, SoundModel>();

            builder.BindSignal<PixelFlow.Signals.InputInteractionSignal>().To<PixelFlow.Commands.ProcessInputCommand>();
            builder.BindSignal<PixelFlow.Signals.CheckWinConditionSignal>().To<PixelFlow.Commands.CheckWinConditionCommand>();
            builder.BindSignal<PixelFlow.Signals.LoadLevelSignal>().To<PixelFlow.Commands.LoadLevelCommand>();
            builder.BindSignal<PixelFlow.Signals.RequestHintSignal>().To<PixelFlow.Commands.UseHintCommand>();
            builder.BindSignal<PixelFlow.Commands.ChangeThemeSignal>().To<PixelFlow.Commands.ChangeThemeCommand>();
            builder.BindCommand<PixelFlow.Signals.LevelCompletedSignal, PixelFlow.Commands.SaveProgressCommand>(ExecutionMode.Exclusive, priority: 0);
            builder.BindSignal<PixelFlow.Signals.UndoSignal>().To<PixelFlow.Commands.UndoCommand>();
            builder.BindSignal<PixelFlow.Signals.RedoSignal>().To<PixelFlow.Commands.RedoCommand>();
            builder.BindSignal<PixelFlow.Signals.TimerTickSignal>().To<PixelFlow.Commands.TimerCommand>();
        }

        public ValueTask OnInitializeAsync(CancellationToken ct) => default;
        public ValueTask OnStartAsync(CancellationToken ct) => default;
        public void OnDispose() { }
    }
}
