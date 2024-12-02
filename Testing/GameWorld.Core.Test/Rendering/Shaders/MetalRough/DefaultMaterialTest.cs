using GameWorld.Core.Rendering.Materials;
using GameWorld.Core.Rendering.Materials.Capabilities;
using GameWorld.Core.Rendering.Materials.Serialization;
using GameWorld.Core.Rendering.Materials.Shaders;
using GameWorld.Core.Test.TestUtility;
using GameWorld.Core.Test.TestUtility.Material;
using Microsoft.Xna.Framework;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.GameFormats.RigidModel.Types;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Test.Rendering.Shaders.MetalRough
{
    internal class DefaultMaterialTest
    {
        IPackFileService _pfs;

        [SetUp]
        public void Setup()
        {
            var selectedGame = GameTypeEnum.Warhammer3;
            var appSettings = new ApplicationSettingsService(selectedGame);
            _pfs = new PackFileService(null);
            _pfs.EnforceGameFilesMustBeLoaded = false;
            var _ = _pfs.CreateNewPackFileContainer("output", PackFileCAType.MOD, true);
        }

        CapabilityMaterialFactory GetMaterialFactory(GameTypeEnum gameTypeEnum)
        {
            var appSettings = new ApplicationSettingsService(gameTypeEnum);
            return new CapabilityMaterialFactory(appSettings, null);
        }

        MaterialToWsMaterialSerializer CreateWsMaterialSerializer(GameTypeEnum gameTypeEnum)
        {
            var saveHelper = new FileSaveService(_pfs, null);
            var materialRepo = new WsMaterialRepository(_pfs);
            return new MaterialToWsMaterialSerializer(saveHelper, materialRepo, gameTypeEnum);
        }

        IRmvMaterial GetRmvMaterial()
        {
            var rmvMaterial = RmvMaterialHelper
                .Create(ModelMaterialEnum.weighted)
                .SetAlpha(true)
                .AssignMaterials([TextureType.BaseColour, TextureType.MaterialMap, TextureType.Normal, TextureType.Mask]);

            return rmvMaterial;
        }

        [Test]
        public void CreateFromWsMaterial()
        {
            // Arrange
            var wsMaterial = WsMaterialHelper.GetDefaultMetalRoughWsModelFile();

            // Act
            var material = GetMaterialFactory(GameTypeEnum.Warhammer3).Create(null, wsMaterial);

            // Assert
            var metalRoughCap = material.GetCapability<MetalRoughCapability>();
            Assert.That(metalRoughCap.UseAlpha, Is.EqualTo(true));
            Assert.That(metalRoughCap.BaseColour.TexturePath, Is.EqualTo($"texturePath/wsmodel/{TextureType.BaseColour}.dds"));
            Assert.That(metalRoughCap.MaterialMap.TexturePath, Is.EqualTo($"texturePath/wsmodel/{TextureType.MaterialMap}.dds"));
            Assert.That(metalRoughCap.NormalMap.TexturePath, Is.EqualTo($"texturePath/wsmodel/{TextureType.Normal}.dds"));
            Assert.That(metalRoughCap.Mask.TexturePath, Is.EqualTo($"texturePath/wsmodel/{TextureType.Mask}.dds"));
            Assert.That(metalRoughCap.Distortion.TexturePath, Is.EqualTo($"texturePath/wsmodel/{TextureType.Distortion}.dds"));
            Assert.That(metalRoughCap.DistortionNoise.TexturePath, Is.EqualTo($"texturePath/wsmodel/{TextureType.DistortionNoise}.dds"));

            var bloodCap = material.GetCapability<BloodCapability>();
            Assert.That(bloodCap.BloodMask.TexturePath, Is.EqualTo($"texturePath/wsmodel/{TextureType.Blood}.dds"));
            Assert.That(bloodCap.UseBlood, Is.EqualTo(true));
            Assert.That(bloodCap.UvScale, Is.EqualTo(new Vector2(1,2)));
        }

        [Test]
        [TestCase(GameTypeEnum.Warhammer3)]
        [TestCase(GameTypeEnum.ThreeKingdoms)]
        public void GenerateWsMaterial(GameTypeEnum gameType)
        {
            // Arrange
            var wsMaterial = WsMaterialHelper.GetDefaultMetalRoughWsModelFile();

            // Act
            var material = GetMaterialFactory(gameType).Create(null, wsMaterial);
            var serializer = CreateWsMaterialSerializer(gameType);
            var wsMaterialPath = serializer.ProsessMaterial("custompath/materials", "mymesh", UiVertexFormat.Cinematic, material);
            var packfile = _pfs.FindFile(wsMaterialPath);
            var generatedMaterial = new WsModelMaterialFile(packfile);

            // Assert
            Assert.That(generatedMaterial.VertexType, Is.EqualTo(UiVertexFormat.Cinematic));
            Assert.That(generatedMaterial.Alpha, Is.EqualTo(true));
           
            Assert.That(generatedMaterial.ShaderPath, Is.EqualTo("shaders/weighted4_character_alpha.xml.shader"));
            Assert.That(generatedMaterial.Name, Is.EqualTo("mymesh_weighted4_alpha_on.xml"));

            WsMaterialHelper.ValidateMetalRough(generatedMaterial);
        }

        [Test]
        public void CreateFromRmvMaterial()
        {
            // Arrange
            var rmvMaterial = GetRmvMaterial();

            // Act
            var material = GetMaterialFactory(GameTypeEnum.Warhammer3).Create(rmvMaterial, null);

            // Assert
            var metalRoughCap = material.GetCapability<MetalRoughCapability>();
            Assert.That(metalRoughCap.UseAlpha, Is.EqualTo(true));
            Assert.That(metalRoughCap.BaseColour.TexturePath, Is.EqualTo($"texturePath/{TextureType.BaseColour}.dds"));
            Assert.That(metalRoughCap.MaterialMap.TexturePath, Is.EqualTo($"texturePath/{TextureType.MaterialMap}.dds"));
            Assert.That(metalRoughCap.NormalMap.TexturePath, Is.EqualTo($"texturePath/{TextureType.Normal}.dds"));
            Assert.That(metalRoughCap.Mask.TexturePath, Is.EqualTo($"texturePath/{TextureType.Mask}.dds"));
            Assert.That(metalRoughCap.Distortion.TexturePath, Is.EqualTo("commontextures/winds_of_magic_specular.dds"));
            Assert.That(metalRoughCap.DistortionNoise.TexturePath, Is.EqualTo($"commontextures/winds_of_magic_noise.dds"));

            var bloodCap = material.GetCapability<BloodCapability>();
            Assert.That(bloodCap.BloodMask.TexturePath, Is.EqualTo("commontextures/bloodmap.dds"));
            Assert.That(bloodCap.UseBlood, Is.EqualTo(false));
            Assert.That(bloodCap.UvScale, Is.EqualTo(new Vector2(1, 1)));
        }

        [Test]
        public void GenerateRmvMaterial()
        {
            // Arrange
            var rmvMaterial = GetRmvMaterial();

            // Act
            var material = GetMaterialFactory(GameTypeEnum.ThreeKingdoms).Create(rmvMaterial, null);
            var serializer = new MaterialToRmvSerializer();
            var createdRmvMaterial = serializer.CreateMaterialFromCapabilityMaterial(material) as WeightedMaterial;

            // Assert
            var hasAlphaValue = createdRmvMaterial.IntParams.TryGet(WeightedParamterIds.IntParams_Alpha_index, out var alphaValue);
            Assert.That(hasAlphaValue, Is.True);
            Assert.That(alphaValue, Is.EqualTo(1));
            Assert.That(createdRmvMaterial.MaterialId, Is.EqualTo(ModelMaterialEnum.weighted));

            Assert.That(createdRmvMaterial.GetTexture(TextureType.BaseColour).Value.Path, Is.EqualTo($"texturePath/{TextureType.BaseColour}.dds"));
            Assert.That(createdRmvMaterial.GetTexture(TextureType.MaterialMap).Value.Path, Is.EqualTo($"texturePath/{TextureType.MaterialMap}.dds"));
            Assert.That(createdRmvMaterial.GetTexture(TextureType.Normal).Value.Path, Is.EqualTo($"texturePath/{TextureType.Normal}.dds"));
            Assert.That(createdRmvMaterial.GetTexture(TextureType.Mask).Value.Path, Is.EqualTo($"texturePath/{TextureType.Mask}.dds"));
        }

        [Test]
        public void EqualTest_Same()
        {
            // Arrange
            var materialA = GetMaterialFactory(GameTypeEnum.Warhammer3).CreateMaterial(CapabilityMaterialsEnum.MetalRoughPbr_Default);
            var materialB = GetMaterialFactory(GameTypeEnum.Warhammer3).CreateMaterial(CapabilityMaterialsEnum.MetalRoughPbr_Default);
            
            // Act
            var (result, message) = materialA.AreEqual(materialB);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(message.Length, Is.EqualTo(0));
        }

        [Test]
        public void EqualTest_DiffAlpha()
        {
            // Arrange
            var materialA = GetMaterialFactory(GameTypeEnum.Warhammer3).CreateMaterial(CapabilityMaterialsEnum.MetalRoughPbr_Default);
            materialA.GetCapability<MetalRoughCapability>().UseAlpha = false;
            var materialB = GetMaterialFactory(GameTypeEnum.Warhammer3).CreateMaterial(CapabilityMaterialsEnum.MetalRoughPbr_Default);
            materialB.GetCapability<MetalRoughCapability>().UseAlpha = true;

            // Act
            var (result, message) = materialA.AreEqual(materialB);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(message.Length, Is.Not.EqualTo(0));
        }

        [Test]
        public void EqualTest_DiffBaseColor()
        {
            // Arrange
            var materialA = GetMaterialFactory(GameTypeEnum.Warhammer3).CreateMaterial(CapabilityMaterialsEnum.MetalRoughPbr_Default);
            materialA.GetCapability<MetalRoughCapability>().BaseColour.TexturePath = "Custom path";
            var materialB = GetMaterialFactory(GameTypeEnum.Warhammer3).CreateMaterial(CapabilityMaterialsEnum.MetalRoughPbr_Default);

            // Act
            var (result, message) = materialA.AreEqual(materialB);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(message.Length, Is.Not.EqualTo(0));
        }
    }
}
