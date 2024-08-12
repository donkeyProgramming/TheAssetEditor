using GameWorld.Core.Rendering.Materials;
using GameWorld.Core.Rendering.Materials.Capabilities;
using GameWorld.Core.Rendering.Materials.Serialization;
using GameWorld.Core.Rendering.Materials.Shaders;
using GameWorld.Core.Test.Utility;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.Types;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Test.Rendering.Materials
{
    internal class MaterialToWsMaterialSerializerTests
    {

        CapabilityMaterialFactory _materialFactory;
        PackFileContainer _outputPack;
        MaterialToWsMaterialSerializer _wsMaterialSerializer;
        PackFileService _pfs;

        [SetUp]
        public void Setup()
        {
            var selectedGame = GameTypeEnum.Warhammer3;
            var appSettings = new ApplicationSettingsService(selectedGame);
            _pfs = new PackFileService(new PackFileDataBase(), appSettings, new GameInformationFactory(), null, null, null);
            _pfs.LoadFolderContainer(PathHelper.Folder("Karl_and_celestialgeneral_Pack"));
            var saveHelper = new PackFileSaveService(_pfs);
            var materialRepo = new WsMaterialRepository(_pfs);
            _outputPack = _pfs.CreateNewPackFileContainer("output", PackFileCAType.MOD, true);
            _materialFactory = new CapabilityMaterialFactory(appSettings, null);
            _wsMaterialSerializer = new MaterialToWsMaterialSerializer(saveHelper, materialRepo, selectedGame);
        }


        [Test]
        public void CreateWsMaterial_Default()
        {
            // Arrange
            var material = _materialFactory.CreateMaterial(CapabilityMaterialsEnum.MetalRoughPbr_Default);
            
            material.GetCapability<MetalRoughCapability>().UseAlpha = false;
            material.GetCapability<MetalRoughCapability>().MaterialMap.TexturePath = $"texturePath/{TextureType.MaterialMap}.dds";
            material.GetCapability<MetalRoughCapability>().BaseColour.TexturePath = $"texturePath/{TextureType.BaseColour}.dds";
            
            // Act
            var pathToCreatedMaterial = _wsMaterialSerializer.ProsessMaterial("variantmeshes/wh_variantmodels/hu1/emp/emp_karl_franz/myCustomModel.rmv2", "mymesh0", UiVertexFormat.Weighted, material);

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
        public void CreateWsMaterial_Default_AddSameTwice()
        {
            // Arrange
            var material = _materialFactory.CreateMaterial(CapabilityMaterialsEnum.MetalRoughPbr_Default);

            material.GetCapability<MetalRoughCapability>().UseAlpha = false;
            material.GetCapability<MetalRoughCapability>().MaterialMap.TexturePath = $"texturePath/{TextureType.MaterialMap}.dds";
            material.GetCapability<MetalRoughCapability>().BaseColour.TexturePath = $"texturePath/{TextureType.BaseColour}.dds";

            // Act
            var pathToCreatedMaterial0 = _wsMaterialSerializer.ProsessMaterial("variantmeshes/wh_variantmodels/hu1/emp/emp_karl_franz/myCustomModel.rmv2", "mymesh0", UiVertexFormat.Weighted, material);
            var pathToCreatedMaterial1 = _wsMaterialSerializer.ProsessMaterial("variantmeshes/wh_variantmodels/hu1/emp/emp_karl_franz/myCustomModel.rmv2", "mymesh1", UiVertexFormat.Weighted, material);
            
            // Assert
            Assert.That(pathToCreatedMaterial0, Is.EqualTo("variantmeshes\\wh_variantmodels\\hu1\\emp\\emp_karl_franz/materials/mymesh0_weighted2_alpha_off.xml.material"));
            Assert.That(pathToCreatedMaterial0, Is.EqualTo(pathToCreatedMaterial1));

            var savedWsMaterialFile = _pfs.FindFile(pathToCreatedMaterial0, _outputPack);
            Assert.That(savedWsMaterialFile, Is.Not.Null);
            Assert.That(_outputPack.FileList.Count, Is.EqualTo(1));
        }
    }
}
