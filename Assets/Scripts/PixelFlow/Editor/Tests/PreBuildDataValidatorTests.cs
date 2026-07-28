using NUnit.Framework;
using PixelFlow.Data;
using PixelFlow.Editor;
using UnityEngine;

namespace PixelFlow.Editor.Tests
{
    [TestFixture]
    public class PreBuildDataValidatorTests
    {
        [Test]
        public void ValidateAllData_RunsWithoutCrashing()
        {
            bool isValid = PreBuildDataValidator.ValidateAllData(out string errorMessage);
            
            // Should either be valid or return a non-null error message explaining what config is missing
            if (!isValid)
            {
                Assert.IsNotEmpty(errorMessage, "Geçersizse hata mesajı hangi config'in eksik olduğunu açıklamalı");
            }
            else
            {
                Assert.IsTrue(string.IsNullOrEmpty(errorMessage),
                    "Geçerliyse hata mesajı boş olmalı (yanıltıcı hata metni bırakılmamalı)");
            }
        }

        [Test]
        public void GameConfig_EditorFlags_DefaultToStrictValidation()
        {
            var config = ScriptableObject.CreateInstance<GameConfig>();
            Assert.IsFalse(config.AllowFallbackReleaseText);
            Assert.IsFalse(config.AllowLocalizationFallbackDictionary);
            Assert.IsFalse(config.AllowNotificationFallbackText);
            Assert.IsFalse(config.AllowOfflineCloudSave);
            Assert.IsTrue(config.StrictGlobalReleaseValidation);
        }
    }
}
