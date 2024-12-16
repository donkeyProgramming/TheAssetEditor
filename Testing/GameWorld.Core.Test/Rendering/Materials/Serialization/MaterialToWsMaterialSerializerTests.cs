using GameWorld.Core.Rendering.Materials;
using GameWorld.Core.Rendering.Materials.Capabilities;
using GameWorld.Core.Rendering.Materials.Serialization;
using GameWorld.Core.Rendering.Materials.Shaders;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.Types;
using Shared.GameFormats.WsModel;
using Shared.TestUtility;

namespace GameWorld.Core.Test.Rendering.Materials.Serialization
{
    internal class MaterialToWsMaterialSerializerTests
    {
        PackFileContainer _outputPack;
        IPackFileService _pfs;

        MaterialToWsMaterialSerializer _wsMaterialSerializer;
        CapabilityMaterial _testMaterial;

        [SetUp]
        public void Setup()
        {
            var selectedGame = GameTypeEnum.Warhammer3;
            var appSettings = new ApplicationSettingsService(selectedGame);
            _pfs = PackFileSerivceTestHelper.CreateFromFolder(selectedGame, "Data\\Karl_and_celestialgeneral_Pack");

            var saveHelper = new FileSaveService(_pfs, null);
            var materialRepo = new WsMaterialRepository(_pfs);
            _outputPack = _pfs.CreateNewPackFileContainer("output", PackFileCAType.MOD, true);
            var materialFactory = new CapabilityMaterialFactory(appSettings, null);
            _wsMaterialSerializer = new MaterialToWsMaterialSerializer(saveHelper, materialRepo, selectedGame);

            // Create a material and give it some textures
            _testMaterial = materialFactory.CreateMaterial(CapabilityMaterialsEnum.MetalRoughPbr_Default);
            _testMaterial.GetCapability<MetalRoughCapability>().UseAlpha = false;
            _testMaterial.GetCapability<MetalRoughCapability>().MaterialMap.TexturePath = $"texturePath/{TextureType.MaterialMap}.dds";
            _testMaterial.GetCapability<MetalRoughCapability>().BaseColour.TexturePath = $"texturePath/{TextureType.BaseColour}.dds";
        }


        [Test]
        public void ProsessMaterial()
        {
            // Act
            var pathToCreatedMaterial = _wsMaterialSerializer.ProsessMaterial("variantmeshes/wh_variantmodels/hu1/emp/emp_karl_franz/myCustomModel.rmv2", "mymesh0", UiVertexFormat.Weighted, _testMaterial);

            // Assert
            Assert.That(pathToCreatedMaterial, Is.EqualTo("variantmeshes\\wh_variantmodels\\hu1\\emp\\emp_karl_franz/materials/mymesh0_weighted2_alpha_off.xml.material"));

            var savedWsMaterialFile = _pfs.FindFile(pathToCreatedMaterial, _outputPack);
            Assert.That(savedWsMaterialFile, Is.Not.Null);

            var createdWsMaterial = new WsModelMaterialFile(savedWsMaterialFile);
            Assert.That(createdWsMaterial.Alpha, Is.False);
            Assert.That(createdWsMaterial.VertexType, Is.EqualTo(UiVertexFormat.Weighted));
            Assert.That(createdWsMaterial.Textures[TextureType.MaterialMap], Is.EqualTo($"texturePath/{TextureType.MaterialMap}.dds"));
            Assert.That(createdWsMaterial.Textures[TextureType.BaseColour], Is.EqualTo($"texturePath/{TextureType.BaseColour}.dds"));
        }

        [Test]
        public void ProsessMaterial_AddSameTwice()
        {
            // Act
            var pathToCreatedMaterial0 = _wsMaterialSerializer.ProsessMaterial("variantmeshes/wh_variantmodels/hu1/emp/emp_karl_franz/myCustomModel.rmv2", "mymesh0", UiVertexFormat.Weighted, _testMaterial);
            var pathToCreatedMaterial1 = _wsMaterialSerializer.ProsessMaterial("variantmeshes/wh_variantmodels/hu1/emp/emp_karl_franz/myCustomModel.rmv2", "mymesh1", UiVertexFormat.Weighted, _testMaterial);

            // Assert
            Assert.That(pathToCreatedMaterial0, Is.EqualTo("variantmeshes\\wh_variantmodels\\hu1\\emp\\emp_karl_franz/materials/mymesh0_weighted2_alpha_off.xml.material"));
            Assert.That(pathToCreatedMaterial0, Is.EqualTo(pathToCreatedMaterial1));

            var savedWsMaterialFile = _pfs.FindFile(pathToCreatedMaterial0, _outputPack);
            Assert.That(savedWsMaterialFile, Is.Not.Null);
            Assert.That(_outputPack.FileList.Count, Is.EqualTo(1));
        }
    }
}
