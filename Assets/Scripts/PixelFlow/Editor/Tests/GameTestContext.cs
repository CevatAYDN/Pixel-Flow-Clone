using System.Collections.Generic;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Commands;
using PixelFlow.Data;
using PixelFlow.Models;
using PixelFlow.Services;
using PixelFlow.Signals;
using UnityEngine;

namespace PixelFlow.Editor.Tests
{
    /// <summary>
    /// Shared test factory for all PixelFlow EditMode tests.
    /// Every focused test class should reuse CreateGameContext() and CreateTestLevel()
    /// instead of duplicating setup logic.
    /// </summary>
    public static class GameTestContext
    {
        /// <summary>
        /// Builds a Nexus test context with all PixelFlow game bindings registered.
        /// Uses InMemoryPlayerPrefsService so models can be constructed in EditMode.
        /// Every test must Dispose() the returned context in teardown.
        /// </summary>
        public static NexusTestContext CreateGameContext()
        {
            return NexusTestHarness.CreateContext(builder =>
            {
                builder.Bind<IPlayerPrefsService, InMemoryPlayerPrefsService>();
                builder.BindService<IPathService, PathService>();
                builder.BindService<IGameHistoryService, GameHistoryService>();
                builder.Bind<IPathSolver, RuntimePathSolver>();
                builder.Bind<IHintService, HintService>();
                builder.BindService<IVehicleSimulator, VehicleSimulator>();
                builder.BindService<ITaxCollectionService, TaxCollectionService>();
                builder.BindService<ISaveThrottler, SaveThrottler>();
                builder.BindService<IHapticService, HapticService>();
                builder.BindService<INexusService, LoggerService>();
                builder.Bind<ILoggerService, LoggerService>();
                builder.BindService<ICrisisAdService, CrisisAdService>();
                builder.BindService<IObstacleService, ObstacleService>();
                builder.BindService<IOverclockService, OverclockService>();
                builder.BindService<ITutorialDriver, TutorialDriver>();
                builder.BindService<IAudioService, AudioService>();

                builder.BindReactiveModel<IGridModel, GridModel>();
                builder.BindReactiveModel<ILevelModel, LevelModel>();
                builder.BindReactiveModel<IProgressModel, ProgressModel>();
                builder.BindReactiveModel<IGameStateModel, GameStateModel>();
                builder.BindReactiveModel<IGameSessionModel, GameSessionModel>();
                builder.BindReactiveModel<IHintModel, HintModel>();
                builder.BindReactiveModel<ISettingsModel, SettingsModel>();
                builder.BindReactiveModel<ISoundModel, SoundModel>();
                builder.BindReactiveModel<ICityEconomyModel, CityEconomyModel>();
                builder.BindReactiveModel<ITutorialModel, TutorialModel>();
                builder.Bind<ILevelProgressionService, LevelProgressionService>();

                builder.BindInstance<IRecoveryStrategy>(new DefaultRecoveryStrategy(maxRetries: 3));

                builder.BindSignal<InputInteractionSignal>().To<ProcessInputCommand>();
                builder.BindSignal<CheckWinConditionSignal>().To<CheckWinConditionCommand>();
                builder.BindSignal<LoadLevelSignal>().To<LoadLevelCommand>();
                builder.BindSignal<RequestHintSignal>().To<UseHintCommand>();
                builder.BindSignal<ChangeThemeSignal>().To<ChangeThemeCommand>();
                builder.BindSignal<LevelCompletedSignal>().To<SaveProgressCommand>();
                builder.BindSignal<UndoSignal>().To<UndoCommand>();
                builder.BindSignal<RedoSignal>().To<RedoCommand>();
            });
        }

        /// <summary>
        /// Creates a 5x5 test level with Red, Blue, Green nodes, one bridge at (2,2),
        /// and known solution paths.
        /// </summary>
        public static LevelData CreateTestLevel(int index = 0)
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.levelIndex = index;
            level.width = 5;
            level.height = 5;

            level.initialNodes = new List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(4, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(0, 4), color = ColorType.Blue },
                new GridNode { position = new Vector2Int(4, 4), color = ColorType.Blue },
                new GridNode { position = new Vector2Int(2, 0), color = ColorType.Green },
                new GridNode { position = new Vector2Int(2, 4), color = ColorType.Green },
            };

            level.bridgePositions = new List<Vector2Int> { new Vector2Int(2, 2) };

            level.solutions = new List<PathSolution>
            {
                new PathSolution
                {
                    color = ColorType.Red,
                    pathPositions = new List<Vector2Int>
                    {
                        new Vector2Int(0, 0), new Vector2Int(1, 0),
                        new Vector2Int(2, 0), new Vector2Int(3, 0), new Vector2Int(4, 0)
                    }
                },
                new PathSolution
                {
                    color = ColorType.Blue,
                    pathPositions = new List<Vector2Int>
                    {
                        new Vector2Int(0, 4), new Vector2Int(0, 3),
                        new Vector2Int(0, 2), new Vector2Int(0, 1), new Vector2Int(0, 0)
                    }
                },
                new PathSolution
                {
                    color = ColorType.Green,
                    pathPositions = new List<Vector2Int>
                    {
                        new Vector2Int(2, 0), new Vector2Int(2, 1),
                        new Vector2Int(2, 2), new Vector2Int(2, 3), new Vector2Int(2, 4)
                    }
                }
            };

            return level;
        }

        /// <summary>
        /// Creates an empty level with no initial nodes, bridges, or solutions.
        /// Useful for testing edge cases and manual grid setup.
        /// </summary>
        public static LevelData CreateEmptyLevel(int width, int height, int index = 0)
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.levelIndex = index;
            level.width = width;
            level.height = height;
            level.initialNodes = new List<GridNode>();
            level.bridgePositions = new List<Vector2Int>();
            level.solutions = new List<PathSolution>();
            return level;
        }
    }
}
