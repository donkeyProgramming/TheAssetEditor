using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameWorld.Core.Services.SceneSaving;
using GameWorld.Core.Services.SceneSaving.Geometry;
using GameWorld.Core.Services.SceneSaving.Lod;
using GameWorld.Core.Services.SceneSaving.Material;
using Moq;
using Shared.Core.Settings;
using Shared.GameFormats.RigidModel.LodHeader;

namespace GameWorld.Core.Test.Services.SceneSaving
{
    internal class GeometrySaveSettingsTests
    {
        [TestCase]
        public void Initialize_Wh3()
        {
            // Arrange
            var gameSettings = new ApplicationSettingsService(GameTypeEnum.Warhammer3);

            // Act
            var saveSettings = new GeometrySaveSettings(gameSettings);

            // Assert
            Assert.That(saveSettings.GeometryOutputType, Is.EqualTo(GeometryStrategy.Rmv7));
            Assert.That(saveSettings.MaterialOutputType, Is.EqualTo(MaterialStrategy.WsModel_Warhammer3));
            Assert.That(saveSettings.LodGenerationMethod, Is.EqualTo(LodStrategy.AssetEditor));
        }

        [TestCase]
        public void Initialize_OldGame()
        {
            // Arrange
            var gameSettings = new ApplicationSettingsService(GameTypeEnum.Rome2);

            // Act
            var saveSettings = new GeometrySaveSettings(gameSettings);

            // Assert
            Assert.That(saveSettings.GeometryOutputType, Is.EqualTo(GeometryStrategy.Rmv6));
            Assert.That(saveSettings.MaterialOutputType, Is.EqualTo(MaterialStrategy.None));
            Assert.That(saveSettings.LodGenerationMethod, Is.EqualTo(LodStrategy.AssetEditor));
        }


        [TestCase]
        public void InitializeLodSettings_2_LodsFromHeader()
        {
            // Arrange
            var gameSettings = new ApplicationSettingsService(GameTypeEnum.Rome2);
            RmvLodHeader[] lodHeader = [
                new Mock<RmvLodHeader>().Object,
                new Mock<RmvLodHeader>().Object];

            // Act
            var saveSettings = new GeometrySaveSettings(gameSettings);
            saveSettings.InitializeLodSettings(lodHeader);

            // Assert
            AssertLodHeaderSettings(saveSettings, 2, [false, false]);
        }

        [TestCase]
        public void InitializeLodSettings_4_LodsFromHeader()
        {
            // Arrange
            var gameSettings = new ApplicationSettingsService(GameTypeEnum.Rome2);
            RmvLodHeader[] lodHeader = [
                new Mock<RmvLodHeader>().Object,
                new Mock<RmvLodHeader>().Object,
                new Mock<RmvLodHeader>().Object,
                new Mock<RmvLodHeader>().Object ];

            // Act
            var saveSettings = new GeometrySaveSettings(gameSettings);
            saveSettings.InitializeLodSettings(lodHeader);

            // Assert
            AssertLodHeaderSettings(saveSettings, 4, [false, false, true, true]);
        }


        [TestCase]
        public void InitializeLodSettings_RefreshNumberOfLods_2()
        {
            // Arrange
            var gameSettings = new ApplicationSettingsService(GameTypeEnum.Rome2);
            RmvLodHeader[] lodHeader = [
                new Mock<RmvLodHeader>().Object,
                new Mock<RmvLodHeader>().Object,
                new Mock<RmvLodHeader>().Object,
                new Mock<RmvLodHeader>().Object ];

            // Act
            var saveSettings = new GeometrySaveSettings(gameSettings);
            saveSettings.InitializeLodSettings(lodHeader);
            saveSettings.NumberOfLodsToGenerate = 2;
            saveSettings.RefreshLodSettings();

            // Assert
            AssertLodHeaderSettings(saveSettings, 2, [false, false]);
        }

        [TestCase]
        public void InitializeLodSettings_RefreshNumberOfLods_5()
        {
            // Arrange
            var gameSettings = new ApplicationSettingsService(GameTypeEnum.Rome2);
            RmvLodHeader[] lodHeader = [
                new Mock<RmvLodHeader>().Object,
                new Mock<RmvLodHeader>().Object,
                new Mock<RmvLodHeader>().Object,
                new Mock<RmvLodHeader>().Object ];

            // Act
            var saveSettings = new GeometrySaveSettings(gameSettings);
            saveSettings.InitializeLodSettings(lodHeader);
            saveSettings.NumberOfLodsToGenerate = 5;
            saveSettings.RefreshLodSettings();

            // Assert
            AssertLodHeaderSettings(saveSettings, 5, [false, false, true, true, true]);
        }


        void AssertLodHeaderSettings(GeometrySaveSettings settings, int expectedCount, bool[] shouldBeOptimized)
        {
            Assert.That(settings.LodSettingsPerLod.Count, Is.EqualTo(expectedCount));
            Assert.That(settings.NumberOfLodsToGenerate, Is.EqualTo(expectedCount));
            Assert.That(shouldBeOptimized.Count, Is.EqualTo(expectedCount));

            for (var i = 0; i < expectedCount; i++)
            {
                Assert.That(settings.LodSettingsPerLod[i].OptimizeAlpha, Is.EqualTo(shouldBeOptimized[i]));
                Assert.That(settings.LodSettingsPerLod[i].OptimizeVertex, Is.EqualTo(shouldBeOptimized[i]));
            }
        }
    }
}
