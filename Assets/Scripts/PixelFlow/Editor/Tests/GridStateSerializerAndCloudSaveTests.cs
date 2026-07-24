using NUnit.Framework;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Data;
using PixelFlow.Models;
using PixelFlow.Services;
using System.Collections.Generic;
using UnityEngine;

namespace PixelFlow.Editor.Tests
{
    [TestFixture]
    public class GridStateSerializerAndCloudSaveTests
    {
        [Test]
        public void GridStateSerializer_SaveAndLoad_RestoresGridModelData()
        {
            using var ctx = GameTestContext.CreateGameContext();
            var grid = ctx.GetModel<IGridModel>();
            var session = ctx.GetModel<IGameSessionModel>();
            var level = ctx.GetModel<ILevelModel>();
            var prefs = ctx.GetModel<IPlayerPrefsService>();

            // Setup level & grid
            var testLevel = GameTestContext.CreateTestLevel(0);
            level.SetLevel(testLevel);
            grid.Initialize(5, 5);

            // Add path and viaduct
            grid.Grid[1, 1].State = CellState.Path;
            grid.Grid[1, 1].Color = ColorType.Red;
            grid.Grid[2, 2].HasViaduct = true;

            session.StartSession(3);
            session.TryUseViaduct();

            // Save
            GridStateSerializer.Save(grid, session, level, prefs);
            Assert.IsTrue(GridStateSerializer.HasSavedGame(prefs));

            // Load into a fresh grid & session
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
            Assert.IsTrue(GridStateSerializer.HasSavedGame(prefs));

            GridStateSerializer.ClearSave(prefs);
            Assert.IsFalse(GridStateSerializer.HasSavedGame(prefs));
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

            CloudSaveManager.SyncToCloud(prefs, "{\"score\":100}", 1);

            var record = CloudSaveManager.LoadCloudRecord(prefs);
            Assert.AreEqual("{\"score\":100}", record.LocalSaveJson);
            Assert.AreEqual("{\"score\":100}", record.CloudSaveJson);
            Assert.AreEqual(1, record.LocalVersion);
        }
    }
}
