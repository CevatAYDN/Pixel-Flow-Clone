using NUnit.Framework;
using Nexus.Core.Services;
using PixelFlow.Data;
using PixelFlow.Models;
using PixelFlow.Services;
using UnityEngine;

using static PixelFlow.Editor.Tests.GameTestContext;

namespace PixelFlow.Editor.Tests
{
    [TestFixture]
    public class GridStateSerializerAndCloudSaveTests
    {
        private GridStateSerializer _serializer;

        [SetUp]
        public void SetUp()
        {
            using var ctx = GameTestContext.CreateGameContext();
            _serializer = ctx.Context.Container.Resolve<GridStateSerializer>();
        }

        [Test]
        public void GridStateSerializer_SaveAndLoad_RestoresGridModelData()
        {
            using var ctx = GameTestContext.CreateGameContext();
            var grid = ctx.GetModel<IGridModel>();
            var session = ctx.GetModel<IGameSessionModel>();
            var level = ctx.GetModel<ILevelModel>();
            var prefs = ctx.GetModel<IPlayerPrefsService>();

            // Setup level & grid
            var testLevel = CreateTestLevel(0);
            level.SetLevel(testLevel);
            grid.Initialize(5, 5);

            // Add path and viaduct
            grid.Grid[1, 1].State = CellState.Path;
            grid.Grid[1, 1].Color = ColorType.Red;
            grid.Grid[2, 2].HasViaduct = true;

            session.StartSession(3);
            session.TryUseViaduct();

            // Save (static method)
            GridStateSerializer.Save(grid, session, level, prefs);
            Assert.IsTrue(_serializer.HasSavedGame(prefs));

            // Load into a fresh grid & session (static method)
            var loadedData = GridStateSerializer.Load(prefs);
            Assert.IsNotNull(loadedData);

            grid.Initialize(5, 5);
            GridStateSerializer.ApplyToGrid(loadedData, grid);

            Assert.AreEqual(5, grid.Width);
            Assert.AreEqual(5, grid.Height);
            Assert.IsTrue(grid.Grid[2, 2].HasViaduct);
            Assert.AreEqual(2, session.AvailableViaducts);
        }

        [Test]
        public void GridStateSerializer_DeleteSaveData_RemovesPrefKey()
        {
            using var ctx = GameTestContext.CreateGameContext();
            var prefs = ctx.GetModel<IPlayerPrefsService>();

            prefs.SetString("NT_PuzzleSave_", "{ \"width\": 5, \"height\": 5 }");
            Assert.IsTrue(_serializer.HasSavedGame(prefs));

            _serializer.ClearSave(prefs);
            Assert.IsFalse(_serializer.HasSavedGame(prefs));
        }

        [Test]
        public void CloudSaveManager_GetOrCreatePlayerId_GeneratesUniqueId()
        {
            using var ctx = GameTestContext.CreateGameContext();
            var prefs = ctx.GetModel<IPlayerPrefsService>();

            string id1 = CloudSaveManager.GetOrCreatePlayerId(prefs);
            Assert.IsFalse(string.IsNullOrEmpty(id1));

            string id2 = CloudSaveManager.GetOrCreatePlayerId(prefs);
            Assert.AreEqual(id1, id2);
        }

        [Test]
        public void CloudSaveManager_ResolveConflict_LastWriteWins()
        {
            var localRecord = new CloudSaveRecord
            {
                LocalSaveJson = "LocalData",
                TimestampUnix = 2000
            };
            var cloudRecord = new CloudSaveRecord
            {
                CloudSaveJson = "CloudData",
                TimestampUnix = 1000
            };

            string resolved = CloudSaveManager.ResolveConflict(localRecord, cloudRecord);
            Assert.AreEqual("LocalData", resolved);

            localRecord.TimestampUnix = 500;
            resolved = CloudSaveManager.ResolveConflict(localRecord, cloudRecord);
            Assert.AreEqual("CloudData", resolved);
        }

        [Test]
        public void CloudSaveManager_SyncToCloud_PersistsRecord()
        {
            using var ctx = GameTestContext.CreateGameContext();
            var prefs = ctx.GetModel<IPlayerPrefsService>();
            var keys = ctx.Context.Container.Resolve<StorageKeysConfigAsset>();

            var manager = new CloudSaveManager
            {
                Adapter = ctx.Context.Container.Resolve<ICloudSaveAdapter>(),
                Prefs = prefs,
                Keys = keys
            };

            manager.SyncToCloudAsync("{\"score\":100}", 1).Wait();

            var record = manager.LoadCloudRecord();
            Assert.AreEqual("{\"score\":100}", record.LocalSaveJson);
            Assert.AreEqual("{\"score\":100}", record.CloudSaveJson);
            Assert.AreEqual(1, record.LocalVersion);
        }
    }
}