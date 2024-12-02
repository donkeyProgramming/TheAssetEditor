using GameWorld.Core.Rendering.Materials;
using GameWorld.Core.Rendering.Materials.Capabilities;
using GameWorld.Core.Rendering.Materials.Serialization;
using GameWorld.Core.Test.TestUtility.Material;
using Microsoft.Xna.Framework;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.Types;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Test.Rendering.Shaders.MetalRough
{
    internal class EmissiveMaterialTest
    {
        CapabilityMaterialFactory _abstractMaterialFactory;
        MaterialToWsMaterialSerializer _wsMaterialSerializer;
        IPackFileService _pfs;

        [SetUp]
        public void Setup()
        {
            var selectedGame = GameTypeEnum.Warhammer3;
            var appSettings = new ApplicationSettingsService(selectedGame);
            _pfs = new PackFileService(null);
            _pfs.EnforceGameFilesMustBeLoaded = false;
            var _outputPack = _pfs.CreateNewPackFileContainer("output", PackFileCAType.MOD, true);

            _abstractMaterialFactory = new CapabilityMaterialFactory(appSettings, null);

            var saveHelper = new FileSaveService(_pfs, null);
            var materialRepo = new WsMaterialRepository(_pfs);
            _wsMaterialSerializer = new MaterialToWsMaterialSerializer(saveHelper, materialRepo, selectedGame);
        }

        [Test]
        public void CreateFromWsMaterial()
        {
            // Arrange
            var wsMaterial = WsMaterialHelper.GetEmissiveWsModelFile();

            // Act
            var material = _abstractMaterialFactory.Create(null, wsMaterial);

            // Assert
            var metalRough = material.GetCapability<MetalRoughCapability>();
            Assert.That(metalRough, Is.Not.Null);

            var emissiveCap = material.GetCapability<EmissiveCapability>();
            Assert.That(emissiveCap.Emissive.TexturePath, Is.EqualTo($"texturePath/wsmodel/{TextureType.Emissive}.dds"));
            Assert.That(emissiveCap.EmissiveDistortion.TexturePath, Is.EqualTo($"texturePath/wsmodel/{TextureType.EmissiveDistortion}.dds"));
            Assert.That(emissiveCap.EmissiveDirection, Is.EqualTo(new Vector2(1,2)));
            Assert.That(emissiveCap.EmissiveDistortStrength, Is.EqualTo(2));
            Assert.That(emissiveCap.EmissiveFresnelStrength, Is.EqualTo(3));
            Assert.That(emissiveCap.EmissiveSpeed, Is.EqualTo(4));
            Assert.That(emissiveCap.EmissivePulseSpeed, Is.EqualTo(5));
            Assert.That(emissiveCap.EmissivePulseStrength, Is.EqualTo(6));
            Assert.That(emissiveCap.GradientColours[0], Is.EqualTo(new Vector3(1, 2, 3)));
            Assert.That(emissiveCap.GradientColours[1], Is.EqualTo(new Vector3(4, 5, 6)));
            Assert.That(emissiveCap.GradientColours[2], Is.EqualTo(new Vector3(7, 8, 9)));
            Assert.That(emissiveCap.GradientColours[3], Is.EqualTo(new Vector3(10, 11, 12)));
            Assert.That(emissiveCap.GradientTimes[0], Is.EqualTo(0));
            Assert.That(emissiveCap.GradientTimes[1], Is.EqualTo(1));
            Assert.That(emissiveCap.GradientTimes[2], Is.EqualTo(2));
            Assert.That(emissiveCap.GradientTimes[3], Is.EqualTo(3));
            Assert.That(emissiveCap.EmissiveStrength, Is.EqualTo(7));
            Assert.That(emissiveCap.EmissiveTiling, Is.EqualTo(new Vector2(4, 5)));
            Assert.That(emissiveCap.EmissiveTint, Is.EqualTo(new Vector3(5,6,7)));
        }

        [Test]
        public void GenerateWsMaterial()
        {
            // Arrange
            var wsMaterial = WsMaterialHelper.GetEmissiveWsModelFile();

            // Act
            var material = _abstractMaterialFactory.Create(null, wsMaterial);
            var wsMaterialPath = _wsMaterialSerializer.ProsessMaterial("custompath/materials", "mymesh", UiVertexFormat.Weighted, material);
            var packfile = _pfs.FindFile(wsMaterialPath);
            var generatedMaterial = new WsModelMaterialFile(packfile);

            // Assert
            Assert.That(generatedMaterial.VertexType, Is.EqualTo(UiVertexFormat.Weighted));
            Assert.That(generatedMaterial.Alpha, Is.EqualTo(false));

            Assert.That(generatedMaterial.ShaderPath, Is.EqualTo("shaders/weighted2_character_emissive.xml.shader"));
            Assert.That(generatedMaterial.Name, Is.EqualTo("mymesh_weighted2_alpha_off.xml"));

            WsMaterialHelper.ValidateEmissive(generatedMaterial);
        }
    }
}
