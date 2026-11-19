using GameWorld.Core.Rendering.Materials;
using GameWorld.Core.Rendering.Materials.Capabilities;
using GameWorld.Core.Rendering.Materials.Serialization;
using GameWorld.Core.Rendering.Materials.Shaders;
using GameWorld.Core.Services.SceneSaving.Material;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Settings;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.Types;
using Shared.GameFormats.WsModel;
using Shared.TestUtility;
using Test.TestingUtility.TestUtility;

namespace GameWorld.Core.Test.Services.SceneSaving
{
    // Each GenerateWsModel call here stands for one save in the editor: a fresh
    // serializer (and so a fresh material repository) every time, like the
    // WsModel strategies create.
    internal class WsModelGeneratorServiceTests
    {
        const string ModelFolder = "variantmeshes/wh3_main_mod/shields/";
        const string ElephantModel = ModelFolder + "shield_elephant.rigid_model_v2";
        const string CattleModel = ModelFolder + "shield_cattle.rigid_model_v2";

        readonly GameTypeEnum _game = GameTypeEnum.Warhammer3;
        IPackFileService _pfs;
        IPackFileContainer _vanillaPack;
        IPackFileContainer _editablePack;
        WsModelGeneratorService _generator;
        MaterialToWsMaterialFactory _serializerFactory;
        CapabilityMaterialFactory _materialFactory;

        [SetUp]
        public void Setup()
        {
            _pfs = new PackFileService(null);
            _vanillaPack = _pfs.CreateNewPackFileContainer("vanilla", PackFileVersion.PFH5, PackFileCAType.RELEASE, false);
            _editablePack = _pfs.CreateNewPackFileContainer("output", PackFileVersion.PFH5, PackFileCAType.MOD, true);

            var saveService = new FileSaveService(_pfs, null, MockScopedLogger.Create());
            _generator = new WsModelGeneratorService(_pfs, saveService, MockScopedLogger.Create());
            _serializerFactory = new MaterialToWsMaterialFactory(_pfs, saveService);
            _materialFactory = new CapabilityMaterialFactory(new ApplicationSettingsService(_game), null);
        }

        CapabilityMaterial CreateMaterial(string baseColourTexture)
        {
            var material = _materialFactory.CreateMaterial(CapabilityMaterialsEnum.MetalRoughPbr_Default);
            material.GetCapability<MetalRoughCapability>().UseAlpha = false;
            material.GetCapability<MetalRoughCapability>().BaseColour.TexturePath = baseColourTexture;
            return material;
        }

        void Save(string modelPath, params (string MeshName, CapabilityMaterial Material)[] meshes)
        {
            var input = meshes
                .Select((mesh, index) => new WsModelGeneratorInput(0, index, mesh.MeshName, UiVertexFormat.Weighted, mesh.Material))
                .ToList();
            _generator.GenerateWsModel(_serializerFactory.CreateInstance(_game), modelPath, input);
        }

        string GetMaterialPath(string modelPath, int partIndex)
        {
            var wsModelFile = _pfs.FindFile(Path.ChangeExtension(modelPath, ".wsmodel"));
            Assert.That(wsModelFile, Is.Not.Null);
            var wsModel = new WsModelFile(wsModelFile!);
            return wsModel.MaterialList.Single(x => x.LodIndex == 0 && x.PartIndex == partIndex).MaterialPath;
        }

        string GetBaseColour(string modelPath, int partIndex = 0)
        {
            var materialFile = _pfs.FindFile(GetMaterialPath(modelPath, partIndex));
            Assert.That(materialFile, Is.Not.Null);
            return new WsModelMaterialFile(materialFile!).Textures[TextureType.BaseColour];
        }

        [Test]
        public void SameMeshNameInSameFolder_SavingSecondModelKeepsFirstModelsTextures()
        {
            Save(ElephantModel, ("shield", CreateMaterial("textures/elephant_skin.dds")));
            Save(CattleModel, ("shield", CreateMaterial("textures/cattle_skin.dds")));

            Assert.That(GetBaseColour(ElephantModel), Is.EqualTo("textures/elephant_skin.dds"));
            Assert.That(GetBaseColour(CattleModel), Is.EqualTo("textures/cattle_skin.dds"));
        }

        [Test]
        public void SameMeshNameInSameFolder_ResavingEitherModelKeepsTheOthersTextures()
        {
            Save(ElephantModel, ("shield", CreateMaterial("textures/elephant_skin.dds")));
            Save(CattleModel, ("shield", CreateMaterial("textures/cattle_skin.dds")));
            Save(ElephantModel, ("shield", CreateMaterial("textures/elephant_skin_v2.dds")));
            Save(CattleModel, ("shield", CreateMaterial("textures/cattle_skin_v2.dds")));

            Assert.That(GetBaseColour(ElephantModel), Is.EqualTo("textures/elephant_skin_v2.dds"));
            Assert.That(GetBaseColour(CattleModel), Is.EqualTo("textures/cattle_skin_v2.dds"));
        }

        [Test]
        public void SharedIdenticalMaterial_ChangingOneModelLeavesTheOtherAlone()
        {
            // Both models start out identical, so the second save reuses the first model's material file
            Save(ElephantModel, ("shield", CreateMaterial("textures/hide.dds")));
            Save(CattleModel, ("shield", CreateMaterial("textures/hide.dds")));
            Assert.That(GetMaterialPath(CattleModel, 0), Is.EqualTo(GetMaterialPath(ElephantModel, 0)));

            Save(ElephantModel, ("shield", CreateMaterial("textures/elephant_skin.dds")));

            Assert.That(GetBaseColour(ElephantModel), Is.EqualTo("textures/elephant_skin.dds"));
            Assert.That(GetBaseColour(CattleModel), Is.EqualTo("textures/hide.dds"));
        }

        [Test]
        public void ResavingOnlyUserOfMaterial_OverwritesInPlace()
        {
            Save(ElephantModel, ("shield", CreateMaterial("textures/elephant_skin.dds")));
            var firstPath = GetMaterialPath(ElephantModel, 0);

            Save(ElephantModel, ("shield", CreateMaterial("textures/elephant_skin_v2.dds")));

            Assert.That(GetMaterialPath(ElephantModel, 0), Is.EqualTo(firstPath));
            Assert.That(GetBaseColour(ElephantModel), Is.EqualTo("textures/elephant_skin_v2.dds"));
            Assert.That(_editablePack.GetFileCount(), Is.EqualTo(2)); // wsmodel + one material
        }

        [Test]
        public void SwappingMaterialsBetweenMeshes_EachMeshGetsItsOwnTextures()
        {
            Save(ElephantModel, ("shield", CreateMaterial("textures/a.dds")), ("rim", CreateMaterial("textures/b.dds")));
            Save(ElephantModel, ("shield", CreateMaterial("textures/b2.dds")), ("rim", CreateMaterial("textures/a.dds")));

            Assert.That(GetBaseColour(ElephantModel, 0), Is.EqualTo("textures/b2.dds"));
            Assert.That(GetBaseColour(ElephantModel, 1), Is.EqualTo("textures/a.dds"));
        }

        [Test]
        public void MeshReusesSiblingsOldMaterial_SiblingDoesNotOverwriteIt()
        {
            Save(ElephantModel, ("shield", CreateMaterial("textures/a.dds")), ("rim", CreateMaterial("textures/b.dds")));

            // "shield" now matches the file "rim" used to write, then "rim" changes
            Save(ElephantModel, ("shield", CreateMaterial("textures/b.dds")), ("rim", CreateMaterial("textures/c.dds")));

            Assert.That(GetBaseColour(ElephantModel, 0), Is.EqualTo("textures/b.dds"));
            Assert.That(GetBaseColour(ElephantModel, 1), Is.EqualTo("textures/c.dds"));
        }

        [Test]
        public void MaterialPathTakenByAnotherPack_IsNotOverridden()
        {
            // Vanilla already ships a different material at the path this save would pick
            var vanillaMaterial = CreateMaterial("textures/vanilla.dds");
            var template = new WsMaterialTemplateEditor(vanillaMaterial, _game);
            var vanillaFileName = template.AddTemplateHeader("shield", UiVertexFormat.Weighted, vanillaMaterial);
            foreach (var cap in vanillaMaterial.Capabilities)
                cap.SerializeToWsModel(template);
            _pfs.AddFilesToPack(_vanillaPack, [new NewPackFileEntry(ModelFolder + "materials", PackFile.CreateFromASCII(vanillaFileName, template.GetCompletedMaterialString()))]);
            _vanillaPack.IsReadOnly = true;
            var vanillaPath = ModelFolder + "materials/" + vanillaFileName;

            Save(ElephantModel, ("shield", CreateMaterial("textures/elephant_skin.dds")));

            Assert.That(_pfs.FindFile(vanillaPath, _editablePack), Is.Null);
            Assert.That(GetBaseColour(ElephantModel), Is.EqualTo("textures/elephant_skin.dds"));
        }
    }
}
