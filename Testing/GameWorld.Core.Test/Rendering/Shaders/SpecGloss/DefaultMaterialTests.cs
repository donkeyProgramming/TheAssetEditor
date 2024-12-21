using GameWorld.Core.Rendering.Materials;
using GameWorld.Core.Rendering.Materials.Capabilities;
using GameWorld.Core.Rendering.Materials.Serialization;
using GameWorld.Core.Rendering.Materials.Shaders;
using GameWorld.Core.Test.TestUtility;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.GameFormats.RigidModel.Types;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Test.Rendering.Shaders.SpecGloss
{
    internal class DefaultMaterialTests
    {
        IPackFileService _pfs;

        [SetUp]
        public void Setup()
        {
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

        WsModelMaterialFile GetWsModelFile()
        {
            var wsMaterial = new WsModelMaterialFile()
            {
                Alpha = false,
                Name = "cth_celestial_general_body_01_weighted4_alpha.xml",
                ShaderPath = "shaders/weighted4_character_alpha.xml.shader",
                Textures = new()
                {
                    {TextureType.Specular, $"texturePath/wsmodel/{TextureType.Specular}.dds"},
                    {TextureType.Gloss, $"texturePath/wsmodel/{TextureType.Gloss}.dds"},
                    {TextureType.Diffuse, $"texturePath/wsmodel/{TextureType.Diffuse}.dds"},
                    {TextureType.Normal, $"texturePath/wsmodel/{TextureType.Normal}.dds"},
                    {TextureType.Mask, $"texturePath/wsmodel/{TextureType.Mask}.dds"},
                    {TextureType.Blood, $"texturePath/wsmodel/{TextureType.Blood}.dds"},
                },
                Parameters =
                [

                ]
            };
            return wsMaterial;
        }

        IRmvMaterial GetRmvMaterial()
        {
            var rmvMaterial = RmvMaterialHelper
                .Create(ModelMaterialEnum.weighted)
                .SetAlpha(true)
                .AssignMaterials([TextureType.Diffuse, TextureType.Specular, TextureType.Gloss, TextureType.Mask, TextureType.Normal]);

            return rmvMaterial;
        }

        [Test]
        public void CreateFromWsMaterial()
        {
            // Arrange
            var wsMaterial = GetWsModelFile();

            // Act
            var material = GetMaterialFactory(GameTypeEnum.Warhammer2).Create(null, wsMaterial);

            // Assert
            var specGlossCap = material.GetCapability<SpecGlossCapability>();
            Assert.That(specGlossCap.UseAlpha, Is.EqualTo(false));
            Assert.That(specGlossCap.SpecularMap.TexturePath, Is.EqualTo($"texturePath/wsmodel/{specGlossCap.SpecularMap.Type}.dds"));
            Assert.That(specGlossCap.GlossMap.TexturePath, Is.EqualTo($"texturePath/wsmodel/{specGlossCap.GlossMap.Type}.dds"));
            Assert.That(specGlossCap.DiffuseMap.TexturePath, Is.EqualTo($"texturePath/wsmodel/{specGlossCap.DiffuseMap.Type}.dds"));
            Assert.That(specGlossCap.NormalMap.TexturePath, Is.EqualTo($"texturePath/wsmodel/{specGlossCap.NormalMap.Type}.dds"));
            Assert.That(specGlossCap.Mask.TexturePath, Is.EqualTo($"texturePath/wsmodel/{specGlossCap.Mask.Type}.dds"));
        }

        [Test]
        [TestCase(GameTypeEnum.Troy)]
        [TestCase(GameTypeEnum.Warhammer2)]
        public void GenerateWsMaterial(GameTypeEnum gameType)
        {
            // Arrange
            var wsMaterial = GetWsModelFile();

            // Act
            var material = GetMaterialFactory(gameType).Create(null, wsMaterial);
            var serializer = CreateWsMaterialSerializer(gameType);
            var wsMaterialPath = serializer.ProsessMaterial("custompath/materials", "mymesh", UiVertexFormat.Cinematic, material);
            var packfile = _pfs.FindFile(wsMaterialPath);
            var generatedMaterial = new WsModelMaterialFile(packfile);

            // Assert
            Assert.That(generatedMaterial.VertexType, Is.EqualTo(UiVertexFormat.Cinematic));
            Assert.That(generatedMaterial.Alpha, Is.EqualTo(false));

            Assert.That(generatedMaterial.ShaderPath, Is.EqualTo("shaders/weighted4_character.xml.shader"));
            Assert.That(generatedMaterial.Name, Is.EqualTo("mymesh_weighted4_alpha_off.xml"));

            Assert.That(generatedMaterial.Textures[TextureType.Specular], Is.EqualTo($"texturePath/wsmodel/{TextureType.Specular}.dds"));
            if(gameType != GameTypeEnum.Pharaoh)
                Assert.That(generatedMaterial.Textures[TextureType.Gloss], Is.EqualTo($"texturePath/wsmodel/{TextureType.Gloss}.dds"));
            Assert.That(generatedMaterial.Textures[TextureType.Diffuse], Is.EqualTo($"texturePath/wsmodel/{TextureType.Diffuse}.dds"));
            Assert.That(generatedMaterial.Textures[TextureType.Normal], Is.EqualTo($"texturePath/wsmodel/{TextureType.Normal}.dds"));
            Assert.That(generatedMaterial.Textures[TextureType.Mask], Is.EqualTo($"texturePath/wsmodel/{TextureType.Mask}.dds"));
        }
        [Test]
        [TestCase(GameTypeEnum.Pharaoh)]
        public void GenerateWsMaterialPharaoh(GameTypeEnum gameType)
        {
            //Arrange
            var wsMaterial = GetWsModelFile();

            //Act
            var material = GetMaterialFactory(gameType).Create(null, wsMaterial);
            var serializer = CreateWsMaterialSerializer(gameType);
            var wsMaterialPath = serializer.ProsessMaterial("custompath/materials", "mymesh", UiVertexFormat.Cinematic, material);
            var packfile = _pfs.FindFile(wsMaterialPath);
            var generatedMaterial = new WsModelMaterialFile(packfile);

            // Assert
            Assert.That(generatedMaterial.VertexType, Is.EqualTo(UiVertexFormat.Cinematic));
            Assert.That(generatedMaterial.Alpha, Is.EqualTo(false));

            Assert.That(generatedMaterial.ShaderPath, Is.EqualTo("shaders/system/weighted_standard_4.xml.shader"));
            Assert.That(generatedMaterial.Name, Is.EqualTo("mymesh_weighted_standard_4.xml"));

            Assert.That(generatedMaterial.Textures[TextureType.Specular], Is.EqualTo($"texturePath/wsmodel/{TextureType.Specular}.dds"));
            if (gameType != GameTypeEnum.Pharaoh)
                Assert.That(generatedMaterial.Textures[TextureType.Gloss], Is.EqualTo($"texturePath/wsmodel/{TextureType.Gloss}.dds"));
            Assert.That(generatedMaterial.Textures[TextureType.Diffuse], Is.EqualTo($"texturePath/wsmodel/{TextureType.Diffuse}.dds"));
            Assert.That(generatedMaterial.Textures[TextureType.Normal], Is.EqualTo($"texturePath/wsmodel/{TextureType.Normal}.dds"));
            Assert.That(generatedMaterial.Textures[TextureType.Mask], Is.EqualTo($"texturePath/wsmodel/{TextureType.Mask}.dds"));

        }

        [Test]
        public void CreateFromRmvMaterial()
        {
            // Arrange
            var rmvMaterial = GetRmvMaterial();

            // Act
            var material = GetMaterialFactory(GameTypeEnum.Troy).Create(rmvMaterial, null);

            // Assert
            var specGlossCap = material.GetCapability<SpecGlossCapability>();
            Assert.That(specGlossCap.UseAlpha, Is.EqualTo(true));
            Assert.That(specGlossCap.SpecularMap.TexturePath, Is.EqualTo($"texturePath/{specGlossCap.SpecularMap.Type}.dds"));
            Assert.That(specGlossCap.GlossMap.TexturePath, Is.EqualTo($"texturePath/{specGlossCap.GlossMap.Type}.dds"));
            Assert.That(specGlossCap.DiffuseMap.TexturePath, Is.EqualTo($"texturePath/{specGlossCap.DiffuseMap.Type}.dds"));
            Assert.That(specGlossCap.NormalMap.TexturePath, Is.EqualTo($"texturePath/{specGlossCap.NormalMap.Type}.dds"));
            Assert.That(specGlossCap.Mask.TexturePath, Is.EqualTo($"texturePath/{specGlossCap.Mask.Type}.dds"));
        }

        [Test]
        public void GenerateRmvMaterial()
        {
            // Arrange
            var rmvMaterial = GetRmvMaterial();

            // Act
            var material = GetMaterialFactory(GameTypeEnum.Pharaoh).Create(rmvMaterial, null);
            var serializer = new MaterialToRmvSerializer();
            var createdRmvMaterial = serializer.CreateMaterialFromCapabilityMaterial(material) as WeightedMaterial;

            // Assert
            Assert.That(createdRmvMaterial.IntParams.Get(WeightedParamterIds.IntParams_Alpha_index), Is.EqualTo(1));
            Assert.That(createdRmvMaterial.MaterialId, Is.EqualTo(ModelMaterialEnum.weighted));

            Assert.That(createdRmvMaterial.GetTexture(TextureType.Specular).Value.Path, Is.EqualTo($"texturePath/{TextureType.Specular}.dds"));
            Assert.That(createdRmvMaterial.GetTexture(TextureType.Gloss).Value.Path, Is.EqualTo($"texturePath/{TextureType.Gloss}.dds"));
            Assert.That(createdRmvMaterial.GetTexture(TextureType.Diffuse).Value.Path, Is.EqualTo($"texturePath/{TextureType.Diffuse}.dds"));
            Assert.That(createdRmvMaterial.GetTexture(TextureType.Normal).Value.Path, Is.EqualTo($"texturePath/{TextureType.Normal}.dds"));
            Assert.That(createdRmvMaterial.GetTexture(TextureType.Mask).Value.Path, Is.EqualTo($"texturePath/{TextureType.Mask}.dds"));
        }

        [Test]
        public void EqualTest_Same()
        {
            // Arrange
            var materialA = GetMaterialFactory(GameTypeEnum.Pharaoh).CreateMaterial(CapabilityMaterialsEnum.SpecGlossPbr_Default);
            var materialB = GetMaterialFactory(GameTypeEnum.Pharaoh).CreateMaterial(CapabilityMaterialsEnum.SpecGlossPbr_Default);

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
            var materialA = GetMaterialFactory(GameTypeEnum.Pharaoh).CreateMaterial(CapabilityMaterialsEnum.SpecGlossPbr_Default);
            materialA.GetCapability<SpecGlossCapability>().UseAlpha = false;
            var materialB = GetMaterialFactory(GameTypeEnum.Pharaoh).CreateMaterial(CapabilityMaterialsEnum.SpecGlossPbr_Default);
            materialB.GetCapability<SpecGlossCapability>().UseAlpha = true;

            // Act
            var (result, message) = materialA.AreEqual(materialB);

            // Assert
            // Assert
            Assert.That(result, Is.False);
            Assert.That(message.Length, Is.Not.EqualTo(0));
        }

        [Test]
        public void EqualTest_DiffDiffuseColor()
        {
            // Arrange
            var materialA = GetMaterialFactory(GameTypeEnum.Pharaoh).CreateMaterial(CapabilityMaterialsEnum.SpecGlossPbr_Default);
            materialA.GetCapability<SpecGlossCapability>().DiffuseMap.TexturePath = "Custom path";
            var materialB = GetMaterialFactory(GameTypeEnum.Pharaoh).CreateMaterial(CapabilityMaterialsEnum.SpecGlossPbr_Default);

            // Act
            var (result, message) = materialA.AreEqual(materialB);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(message.Length, Is.Not.EqualTo(0));
        }
    }
}
