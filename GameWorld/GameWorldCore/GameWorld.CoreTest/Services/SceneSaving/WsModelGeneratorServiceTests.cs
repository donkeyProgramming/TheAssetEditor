using System.Text;
using System.Text.RegularExpressions;
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
    // Each Save() is one editor save: a fresh serializer and repository, as the WsModel strategies create
    internal class WsModelGeneratorServiceTests
    {
        const string ModelFolder = "variantmeshes/wh3_main_mod/shields/";
        const string ElephantModel = ModelFolder + "shield_elephant.rigid_model_v2";
        const string CattleModel = ModelFolder + "shield_cattle.rigid_model_v2";

        const string VanillaMaterialFolder = "variantmeshes/wh_variantmodels/hu1/emp/emp_karl_franz/materials";

        readonly GameTypeEnum _game = GameTypeEnum.Warhammer3;
        IPackFileService _pfs;
        IPackFileContainer _vanillaPack;
        IPackFileContainer _otherPack;
        IPackFileContainer _warhammer2Pack;
        IPackFileContainer _editablePack;
        FileSaveService _saveService;
        WsModelGeneratorService _generator;
        MaterialToWsMaterialFactory _serializerFactory;
        CapabilityMaterialFactory _materialFactory;

        record Mesh(string Name, CapabilityMaterial Material, UiVertexFormat VertexFormat = UiVertexFormat.Weighted);

        [SetUp]
        public void Setup()
        {
            _pfs = new PackFileService(null);
            _vanillaPack = _pfs.CreateNewPackFileContainer("vanilla", PackFileVersion.PFH5, PackFileCAType.RELEASE, false);
            _vanillaPack.IsCaPackFile = true;
            _otherPack = _pfs.CreateNewPackFileContainer("other_mod", PackFileVersion.PFH5, PackFileCAType.MOD, false);
            _otherPack.IsReadOnly = true;
            _vanillaPack.PackFileSettings.GameVersion = _game;
            _warhammer2Pack = _pfs.CreateNewPackFileContainer("warhammer2", PackFileVersion.PFH5, PackFileCAType.RELEASE, false);
            _warhammer2Pack.IsCaPackFile = true;
            _warhammer2Pack.PackFileSettings.GameVersion = GameTypeEnum.Warhammer2;
            _editablePack = _pfs.CreateNewPackFileContainer("output", PackFileVersion.PFH5, PackFileCAType.MOD, true);

            _saveService = new FileSaveService(_pfs, null, MockScopedLogger.Create());
            _generator = new WsModelGeneratorService(_pfs, _saveService, MockScopedLogger.Create());
            _serializerFactory = new MaterialToWsMaterialFactory(_pfs, _saveService);
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
            => SaveMeshes(modelPath, meshes.Select(x => new Mesh(x.MeshName, x.Material)).ToArray());

        void SaveMeshes(string modelPath, params Mesh[] meshes) => SaveMeshes(modelPath, false, meshes);

        void SaveMeshes(string modelPath, bool regenerateMaterials, params Mesh[] meshes)
        {
            var input = meshes
                .Select((mesh, index) => new WsModelGeneratorInput(0, index, mesh.Name, mesh.VertexFormat, mesh.Material))
                .ToList();
            _generator.GenerateWsModel(_serializerFactory.CreateInstance(_game), modelPath, input, regenerateMaterials);
        }

        (string FileName, string Content) GenerateMaterialFile(string meshName, UiVertexFormat vertexFormat, CapabilityMaterial material)
        {
            var template = new WsMaterialTemplateEditor(material, _game);
            var fileName = template.AddTemplateHeader(meshName, vertexFormat, material);
            foreach (var cap in material.Capabilities)
                cap.SerializeToWsModel(template);
            return (fileName, template.GetCompletedMaterialString());
        }

        // What a modder tunes by hand: a shader and a param the template has no place for
        static string HandTune(string materialContent)
        {
            var tuned = Regex.Replace(materialContent, @"(<shader>[^<]*)\.xml\.shader</shader>", "$1_hair.xml.shader</shader>");
            tuned = tuned.Replace("</params>", "<param><name>hand_tuned_sheen</name><type>float</type><value>0.35</value></param></params>");
            Assert.That(tuned, Is.Not.EqualTo(materialContent));
            return tuned;
        }

        // Adds a file to a pack loaded read-only (the game's, or one opened for reference)
        void AddToReadOnlyPack(IPackFileContainer pack, string folder, PackFile file)
        {
            var isCaPackFile = pack.IsCaPackFile;
            pack.IsCaPackFile = false;
            pack.IsReadOnly = false;
            _pfs.AddFilesToPack(pack, [new NewPackFileEntry(folder, file)]);
            pack.IsReadOnly = true;
            pack.IsCaPackFile = isCaPackFile;
        }

        // A material with a shader the template would not write, in the game's packs or another read-only pack
        string AddVanillaMaterial(string meshName, string baseColourTexture, UiVertexFormat vertexFormat = UiVertexFormat.Weighted, IPackFileContainer? pack = null)
        {
            var (fileName, content) = GenerateMaterialFile(meshName, vertexFormat, CreateMaterial(baseColourTexture));
            AddToReadOnlyPack(pack ?? _vanillaPack, VanillaMaterialFolder, PackFile.CreateFromASCII(fileName, HandTune(content)));
            return VanillaMaterialFolder + "/" + fileName;
        }

        // The model saved by AE once, then its material tuned by hand in the pack
        string SaveThenHandTune(string modelPath, string meshName, string baseColourTexture)
        {
            Save(modelPath, (meshName, CreateMaterial(baseColourTexture)));
            var materialPath = GetMaterialPath(modelPath, 0);
            _saveService.Save(materialPath, Encoding.UTF8.GetBytes(HandTune(ReadFile(materialPath))), false);
            return materialPath;
        }

        // As the scene loader does for a mesh whose wsmodel material loaded
        CapabilityMaterial LoadMaterial(string materialPath, UiVertexFormat vertexFormat = UiVertexFormat.Weighted)
        {
            var file = _pfs.FindFile(materialPath);
            Assert.That(file, Is.Not.Null);
            var material = _materialFactory.Create(null!, new WsModelMaterialFile(file!));
            material.RememberLoadedFrom(materialPath, vertexFormat);
            return material;
        }

        string ReadFile(string path)
        {
            var file = _pfs.FindFile(path);
            Assert.That(file, Is.Not.Null, path);
            return Encoding.UTF8.GetString(file!.DataSource.ReadData());
        }

        string GetShader(string modelPath, int partIndex = 0) => new WsModelMaterialFile(ReadFile(GetMaterialPath(modelPath, partIndex))).ShaderPath;

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
            AddToReadOnlyPack(_vanillaPack, ModelFolder + "materials", PackFile.CreateFromASCII(vanillaFileName, template.GetCompletedMaterialString()));
            var vanillaPath = ModelFolder + "materials/" + vanillaFileName;

            Save(ElephantModel, ("shield", CreateMaterial("textures/elephant_skin.dds")));

            Assert.That(_pfs.FindFile(vanillaPath, _editablePack), Is.Null);
            Assert.That(GetBaseColour(ElephantModel), Is.EqualTo("textures/elephant_skin.dds"));
        }

        [Test]
        public void IdenticalMaterialInAnotherPack_IsNotReused()
        {
            // Another mod ships exactly what this save would write, at its own path
            var material = CreateMaterial("textures/elephant_skin.dds");
            var (fileName, content) = GenerateMaterialFile("other_shield", UiVertexFormat.Weighted, material);
            AddToReadOnlyPack(_otherPack, "variantmeshes/other_mod/materials", PackFile.CreateFromASCII(fileName, content));

            Save(ElephantModel, ("shield", material));

            var materialPath = GetMaterialPath(ElephantModel, 0);
            Assert.That(materialPath.Replace('\\', '/'), Does.StartWith(ModelFolder));
            Assert.That(_pfs.FindFile(materialPath, _editablePack), Is.Not.Null);
        }

        [Test]
        public void IdenticalVanillaMaterial_IsReused()
        {
            var material = CreateMaterial("textures/elephant_skin.dds");
            var (fileName, content) = GenerateMaterialFile("karl_shield", UiVertexFormat.Weighted, material);
            AddToReadOnlyPack(_vanillaPack, VanillaMaterialFolder, PackFile.CreateFromASCII(fileName, content));

            Save(ElephantModel, ("shield", material));

            Assert.That(GetMaterialPath(ElephantModel, 0).Replace('\\', '/'), Is.EqualTo(VanillaMaterialFolder + "/" + fileName));
            Assert.That(_editablePack.GetFileCount(), Is.EqualTo(1)); // only the wsmodel
        }

        // Materials loaded from a file, and left alone, keep that file

        [Test]
        public void HandTunedMaterial_ResavedUnchanged_IsKeptAsIs()
        {
            var tunedPath = SaveThenHandTune(ElephantModel, "shield", "textures/elephant_skin.dds");
            var tunedContent = ReadFile(tunedPath);

            Save(ElephantModel, ("shield", LoadMaterial(tunedPath)));
            Save(ElephantModel, ("shield", LoadMaterial(tunedPath)));

            Assert.That(GetMaterialPath(ElephantModel, 0), Is.EqualTo(tunedPath));
            Assert.That(ReadFile(tunedPath), Is.EqualTo(tunedContent));
            Assert.That(_editablePack.GetFileCount(), Is.EqualTo(2)); // wsmodel + the tuned material
        }

        [Test]
        public void VanillaMaterial_Unchanged_IsReferencedNotCopied()
        {
            var vanillaPath = AddVanillaMaterial("karl_body", "textures/karl_body.dds");

            Save(ElephantModel, ("shield", LoadMaterial(vanillaPath)));

            Assert.That(GetMaterialPath(ElephantModel, 0), Is.EqualTo(vanillaPath));
            Assert.That(GetShader(ElephantModel), Is.EqualTo("shaders/weighted2_character_hair.xml.shader"));
            Assert.That(_editablePack.GetFileCount(), Is.EqualTo(1)); // only the wsmodel
        }

        [Test]
        public void OtherPacksMaterial_Unchanged_IsGeneratedIntoTheMod()
        {
            // Another mod, or another game's pack opened to port from: the file may not be there when this mod is used
            var otherPath = AddVanillaMaterial("karl_body", "textures/karl_body.dds", pack: _otherPack);
            var otherContent = ReadFile(otherPath);

            Save(ElephantModel, ("shield", LoadMaterial(otherPath)));

            var materialPath = GetMaterialPath(ElephantModel, 0);
            Assert.That(materialPath, Is.Not.EqualTo(otherPath));
            Assert.That(_pfs.FindFile(materialPath, _editablePack), Is.Not.Null);
            Assert.That(GetShader(ElephantModel), Is.EqualTo("shaders/weighted2_character.xml.shader"));
            Assert.That(GetBaseColour(ElephantModel), Is.EqualTo("textures/karl_body.dds"));
            Assert.That(ReadFile(otherPath), Is.EqualTo(otherContent));
        }

        [Test]
        public void OtherGamesMaterial_Unchanged_IsGeneratedIntoTheMod()
        {
            // File > Open Warhammer2 packs, to port a WH2 model to WH3
            var warhammer2Path = AddVanillaMaterial("karl_body", "textures/karl_body.dds", pack: _warhammer2Pack);

            Save(ElephantModel, ("shield", LoadMaterial(warhammer2Path)));

            Assert.That(GetMaterialPath(ElephantModel, 0), Is.Not.EqualTo(warhammer2Path));
            Assert.That(GetShader(ElephantModel), Is.EqualTo("shaders/weighted2_character.xml.shader"));
            Assert.That(GetBaseColour(ElephantModel), Is.EqualTo("textures/karl_body.dds"));
        }

        [Test]
        public void IdenticalMaterialInOtherGamesPacks_IsNotReused()
        {
            var material = CreateMaterial("textures/elephant_skin.dds");
            var (fileName, content) = GenerateMaterialFile("karl_shield", UiVertexFormat.Weighted, material);
            AddToReadOnlyPack(_warhammer2Pack, VanillaMaterialFolder, PackFile.CreateFromASCII(fileName, content));

            Save(ElephantModel, ("shield", material));

            Assert.That(_pfs.FindFile(GetMaterialPath(ElephantModel, 0), _editablePack), Is.Not.Null);
        }

        [Test]
        public void RenamedMesh_KeepsItsFile()
        {
            var vanillaPath = AddVanillaMaterial("karl_body", "textures/karl_body.dds");

            Save(ElephantModel, ("renamed_in_the_editor", LoadMaterial(vanillaPath)));

            Assert.That(GetMaterialPath(ElephantModel, 0), Is.EqualTo(vanillaPath));
        }

        [Test]
        public void CopiedMaterial_BothMeshesUseTheLoadedFile()
        {
            var vanillaPath = AddVanillaMaterial("karl_body", "textures/karl_body.dds");
            var material = LoadMaterial(vanillaPath);

            Save(ElephantModel, ("shield", material), ("shield_copy", material.Clone()));

            Assert.That(GetMaterialPath(ElephantModel, 0), Is.EqualTo(vanillaPath));
            Assert.That(GetMaterialPath(ElephantModel, 1), Is.EqualTo(vanillaPath));
            Assert.That(_editablePack.GetFileCount(), Is.EqualTo(1));
        }

        [Test]
        public void MaterialTypeChangedAndBack_KeepsTheLoadedFile()
        {
            var vanillaPath = AddVanillaMaterial("karl_body", "textures/karl_body.dds");
            var emissive = _materialFactory.ChangeMaterial(LoadMaterial(vanillaPath), CapabilityMaterialsEnum.MetalRoughPbr_Emissive);
            var backToDefault = _materialFactory.ChangeMaterial(emissive, CapabilityMaterialsEnum.MetalRoughPbr_Default);

            Save(ElephantModel, ("shield", backToDefault));

            Assert.That(GetMaterialPath(ElephantModel, 0), Is.EqualTo(vanillaPath));
        }

        // Materials edited in AE are written by AE

        [Test]
        public void VanillaMaterial_Edited_IsRegeneratedAndVanillaFileUntouched()
        {
            var vanillaPath = AddVanillaMaterial("karl_body", "textures/karl_body.dds");
            var vanillaContent = ReadFile(vanillaPath);
            var material = LoadMaterial(vanillaPath);
            material.GetCapability<MetalRoughCapability>().BaseColour.TexturePath = "textures/repainted.dds";

            Save(ElephantModel, ("shield", material));

            Assert.That(GetMaterialPath(ElephantModel, 0), Is.Not.EqualTo(vanillaPath));
            Assert.That(GetBaseColour(ElephantModel), Is.EqualTo("textures/repainted.dds"));
            Assert.That(GetShader(ElephantModel), Is.EqualTo("shaders/weighted2_character.xml.shader"));
            Assert.That(ReadFile(vanillaPath), Is.EqualTo(vanillaContent));
        }

        [Test]
        public void AeWrittenMaterial_Edited_IsOverwrittenInPlace()
        {
            Save(ElephantModel, ("shield", CreateMaterial("textures/elephant_skin.dds")));
            var materialPath = GetMaterialPath(ElephantModel, 0);
            var material = LoadMaterial(materialPath);
            material.GetCapability<MetalRoughCapability>().BaseColour.TexturePath = "textures/elephant_skin_v2.dds";

            Save(ElephantModel, ("shield", material));

            Assert.That(GetMaterialPath(ElephantModel, 0), Is.EqualTo(materialPath));
            Assert.That(GetBaseColour(ElephantModel), Is.EqualTo("textures/elephant_skin_v2.dds"));
            Assert.That(_editablePack.GetFileCount(), Is.EqualTo(2));
        }

        [Test]
        public void HandTunedMaterial_Edited_GoesToANewFileAndTheTunedFileIsKept()
        {
            var tunedPath = SaveThenHandTune(ElephantModel, "shield", "textures/elephant_skin.dds");
            var tunedContent = ReadFile(tunedPath);
            var material = LoadMaterial(tunedPath);
            material.GetCapability<MetalRoughCapability>().BaseColour.TexturePath = "textures/elephant_skin_v2.dds";

            Save(ElephantModel, ("shield", material));

            Assert.That(GetMaterialPath(ElephantModel, 0), Is.Not.EqualTo(tunedPath));
            Assert.That(GetBaseColour(ElephantModel), Is.EqualTo("textures/elephant_skin_v2.dds"));
            Assert.That(ReadFile(tunedPath), Is.EqualTo(tunedContent));
        }

        [Test]
        public void AlphaChanged_IsRegenerated()
        {
            var vanillaPath = AddVanillaMaterial("karl_body", "textures/karl_body.dds");
            var material = LoadMaterial(vanillaPath);
            material.GetCapability<MetalRoughCapability>().UseAlpha = true;

            Save(ElephantModel, ("shield", material));

            Assert.That(GetMaterialPath(ElephantModel, 0), Is.Not.EqualTo(vanillaPath));
            Assert.That(GetShader(ElephantModel), Is.EqualTo("shaders/weighted2_character_alpha.xml.shader"));
        }

        [Test]
        public void VertexFormatChanged_IsRegeneratedForTheNewFormat()
        {
            var vanillaPath = AddVanillaMaterial("karl_body", "textures/karl_body.dds", UiVertexFormat.Cinematic);

            SaveMeshes(ElephantModel, new Mesh("shield", LoadMaterial(vanillaPath, UiVertexFormat.Cinematic), UiVertexFormat.Weighted));

            Assert.That(GetMaterialPath(ElephantModel, 0), Is.Not.EqualTo(vanillaPath));
            Assert.That(GetShader(ElephantModel), Is.EqualTo("shaders/weighted2_character.xml.shader"));
            Assert.That(GetBaseColour(ElephantModel), Is.EqualTo("textures/karl_body.dds"));
        }

        [Test]
        public void CopiedMaterialOnAMeshWithAnotherVertexFormat_IsRegeneratedForThatMeshOnly()
        {
            var vanillaPath = AddVanillaMaterial("karl_body", "textures/karl_body.dds");
            var material = LoadMaterial(vanillaPath);

            SaveMeshes(ElephantModel, new Mesh("shield", material), new Mesh("buckle", material.Clone(), UiVertexFormat.Static));

            Assert.That(GetMaterialPath(ElephantModel, 0), Is.EqualTo(vanillaPath));
            Assert.That(GetMaterialPath(ElephantModel, 1), Is.Not.EqualTo(vanillaPath));
            Assert.That(GetShader(ElephantModel, 1), Is.EqualTo("shaders/rigid_character.xml.shader"));
        }

        [Test]
        public void MaterialTypeChanged_IsRegenerated()
        {
            var vanillaPath = AddVanillaMaterial("karl_body", "textures/karl_body.dds");

            Save(ElephantModel, ("shield", _materialFactory.ChangeMaterial(LoadMaterial(vanillaPath), CapabilityMaterialsEnum.MetalRoughPbr_Emissive)));

            Assert.That(GetMaterialPath(ElephantModel, 0), Is.Not.EqualTo(vanillaPath));
            Assert.That(GetShader(ElephantModel), Is.EqualTo("shaders/weighted2_character_emissive.xml.shader"));
        }

        [Test]
        public void LoadedFileDeletedBeforeSave_IsRegenerated()
        {
            var tunedPath = SaveThenHandTune(ElephantModel, "shield", "textures/elephant_skin.dds");
            var material = LoadMaterial(tunedPath);
            _pfs.DeleteFile(_editablePack, _pfs.FindFile(tunedPath, _editablePack)!);

            Save(ElephantModel, ("shield", material));

            Assert.That(_pfs.FindFile(GetMaterialPath(ElephantModel, 0)), Is.Not.Null);
            Assert.That(GetBaseColour(ElephantModel), Is.EqualTo("textures/elephant_skin.dds"));
        }

        [Test]
        public void LoadedMaterialWithNoTemplate_IsGeneratedAsBefore()
        {
            var vanillaPath = AddVanillaMaterial("karl_body", "textures/karl_body.dds");
            var material = LoadMaterial(vanillaPath, UiVertexFormat.Unknown); // The template has no shader for this

            Save(ElephantModel, ("shield", material));

            Assert.That(GetMaterialPath(ElephantModel, 0), Is.Not.EqualTo(vanillaPath));
            Assert.That(GetBaseColour(ElephantModel), Is.EqualTo("textures/karl_body.dds"));
        }

        // A kept file is never overwritten by another mesh in the same save, whatever the mesh order

        [Test]
        public void SharedLoadedMaterial_EditingTheFirstMeshLeavesTheSecondsFileAlone()
        {
            // Identical meshes share one generated file
            Save(ElephantModel, ("shield", CreateMaterial("textures/hide.dds")), ("shield_back", CreateMaterial("textures/hide.dds")));
            var sharedPath = GetMaterialPath(ElephantModel, 0);
            Assert.That(GetMaterialPath(ElephantModel, 1), Is.EqualTo(sharedPath));

            var edited = LoadMaterial(sharedPath);
            edited.GetCapability<MetalRoughCapability>().BaseColour.TexturePath = "textures/painted.dds";
            Save(ElephantModel, ("shield", edited), ("shield_back", LoadMaterial(sharedPath)));

            Assert.That(GetMaterialPath(ElephantModel, 1), Is.EqualTo(sharedPath));
            Assert.That(GetBaseColour(ElephantModel, 1), Is.EqualTo("textures/hide.dds"));
            Assert.That(GetBaseColour(ElephantModel, 0), Is.EqualTo("textures/painted.dds"));
        }

        [Test]
        public void NewMeshNamedLikeAKeptFile_DoesNotOverwriteIt()
        {
            Save(ElephantModel, ("rim", CreateMaterial("textures/rim.dds")));
            var rimPath = GetMaterialPath(ElephantModel, 0);

            // The old rim renamed, and a new mesh given its name, so its generated file name is the kept one
            Save(ElephantModel, ("rim", CreateMaterial("textures/new_rim.dds")), ("old_rim", LoadMaterial(rimPath)));

            Assert.That(GetMaterialPath(ElephantModel, 1), Is.EqualTo(rimPath));
            Assert.That(GetBaseColour(ElephantModel, 1), Is.EqualTo("textures/rim.dds"));
            Assert.That(GetBaseColour(ElephantModel, 0), Is.EqualTo("textures/new_rim.dds"));
        }

        // Regenerate all materials

        [Test]
        public void Regenerate_HandTunedMaterialAtGeneratedName_IsOverwrittenFromTheTemplate()
        {
            var tunedPath = SaveThenHandTune(ElephantModel, "shield", "textures/elephant_skin.dds");

            SaveMeshes(ElephantModel, true, new Mesh("shield", LoadMaterial(tunedPath)));

            Assert.That(GetMaterialPath(ElephantModel, 0), Is.EqualTo(tunedPath));
            Assert.That(GetShader(ElephantModel), Is.EqualTo("shaders/weighted2_character.xml.shader"));
            Assert.That(ReadFile(tunedPath), Does.Not.Contain("hand_tuned_sheen"));
            Assert.That(GetBaseColour(ElephantModel), Is.EqualTo("textures/elephant_skin.dds"));
        }

        [Test]
        public void Regenerate_VanillaMaterial_IsGeneratedIntoTheMod()
        {
            var vanillaPath = AddVanillaMaterial("karl_body", "textures/karl_body.dds");
            var vanillaContent = ReadFile(vanillaPath);

            SaveMeshes(ElephantModel, true, new Mesh("shield", LoadMaterial(vanillaPath)));

            Assert.That(GetMaterialPath(ElephantModel, 0), Is.Not.EqualTo(vanillaPath));
            Assert.That(GetShader(ElephantModel), Is.EqualTo("shaders/weighted2_character.xml.shader"));
            Assert.That(ReadFile(vanillaPath), Is.EqualTo(vanillaContent));
        }

        [Test]
        public void SecondModelUsingAKeptFile_IsNotOverwrittenByTheFirstModelsEdit()
        {
            // Cattle reuses the elephant's file through dedup, then elephant edits its copy
            Save(ElephantModel, ("shield", CreateMaterial("textures/hide.dds")));
            var sharedPath = GetMaterialPath(ElephantModel, 0);
            Save(CattleModel, ("shield", LoadMaterial(sharedPath)));
            Assert.That(GetMaterialPath(CattleModel, 0), Is.EqualTo(sharedPath));

            var edited = LoadMaterial(sharedPath);
            edited.GetCapability<MetalRoughCapability>().BaseColour.TexturePath = "textures/elephant_skin.dds";
            Save(ElephantModel, ("shield", edited));

            Assert.That(GetBaseColour(ElephantModel), Is.EqualTo("textures/elephant_skin.dds"));
            Assert.That(GetBaseColour(CattleModel), Is.EqualTo("textures/hide.dds"));
        }
    }
}
