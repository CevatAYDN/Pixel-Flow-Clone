using NUnit.Framework;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Data;
using PixelFlow.Models;
using UnityEngine;

namespace PixelFlow.Editor.Tests
{
    [TestFixture]
    public class GameSessionAndModelsTests
    {
        [Test]
        public void GameSessionModel_StartSession_InitializesProperties()
        {
            var session = new GameSessionModel();
            Assert.IsFalse(session.IsSessionActive);

            session.StartSession(maxViaducts: 3, targetFlowScore: 10);

            Assert.IsTrue(session.IsSessionActive);
            Assert.AreEqual(3, session.MaxViaducts);
            Assert.AreEqual(3, session.AvailableViaducts);
            Assert.AreEqual(10, session.TargetFlowScore);
            Assert.AreEqual(0, session.Score);
            Assert.AreEqual(0f, session.ElapsedTime);
        }

        [Test]
        public void GameSessionModel_TryUseViaduct_DecrementsAvailableCount()
        {
            var session = new GameSessionModel();
            session.StartSession(maxViaducts: 2);

            bool success = session.TryUseViaduct();
            Assert.IsTrue(success);
            Assert.AreEqual(1, session.AvailableViaducts);

            success = session.TryUseViaduct();
            Assert.IsTrue(success);
            Assert.AreEqual(0, session.AvailableViaducts);

            success = session.TryUseViaduct();
            Assert.IsFalse(success);
        }

        [Test]
        public void GameSessionModel_UpdateTime_AccumulatesElapsedSeconds()
        {
            var session = new GameSessionModel();
            session.StartSession(maxViaducts: 1);

            session.UpdateTime(1.5f);
            session.UpdateTime(2.0f);

            Assert.AreEqual(3.5f, session.ElapsedTime, 0.01f);
        }

        [Test]
        public void LevelModel_SetLevel_UpdatesCurrentLevel()
        {
            var levelModel = new LevelModel();
            Assert.IsNull(levelModel.CurrentLevel);

            var levelData = ScriptableObject.CreateInstance<LevelData>();
            levelData.levelIndex = 42;

            levelModel.SetLevel(levelData);
            Assert.AreEqual(42, levelModel.CurrentLevel.levelIndex);
        }

        [Test]
        public void ProgressModel_UnlockAndStars_PersistsHigherValues()
        {
            using var ctx = GameTestContext.CreateGameContext();
            var progress = ctx.GetModel<IProgressModel>();

            progress.RecordStars(1, 2);
            Assert.AreEqual(2, progress.GetStars(1));

            // Lower star value should not overwrite higher value
            progress.RecordStars(1, 1);
            Assert.AreEqual(2, progress.GetStars(1));

            // Higher star value updates
            progress.RecordStars(1, 3);
            Assert.AreEqual(3, progress.GetStars(1));
        }

        [Test]
        public void SoundModel_ToggleMute_UpdatesState()
        {
            using var ctx = GameTestContext.CreateGameContext();
            var prefs = ctx.GetModel<IPlayerPrefsService>();
            var keys = ScriptableObject.CreateInstance<StorageKeysConfigAsset>();
            keys.KeySoundMuted = "SoundMuted";
            var sound = new SoundModel(prefs, keys);

            bool initialMute = sound.IsMuted;
            sound.ToggleMute();
            Assert.AreNotEqual(initialMute, sound.IsMuted);

            sound.ToggleMute();
            Assert.AreEqual(initialMute, sound.IsMuted);
        }

        [Test]
        public void TutorialModel_StepLifecycle_UpdatesFlags()
        {
            using var ctx = GameTestContext.CreateGameContext();
            var prefs = ctx.GetModel<IPlayerPrefsService>();
            var keys = ScriptableObject.CreateInstance<StorageKeysConfigAsset>();
            keys.KeyTutorialStep = "TutorialStep";
            var tutorial = new TutorialModel(prefs, keys);

            Assert.IsFalse(tutorial.IsActive);

            tutorial.StartStep(TutorialStep.TouchAndDrag);
            Assert.IsTrue(tutorial.IsActive);
            Assert.AreEqual(TutorialStep.TouchAndDrag, tutorial.CurrentStep);

            tutorial.CompleteStep(TutorialStep.TouchAndDrag);
            Assert.IsFalse(tutorial.IsActive);
            Assert.IsTrue(tutorial.IsCompleted(TutorialStep.TouchAndDrag));
        }
    }
}
