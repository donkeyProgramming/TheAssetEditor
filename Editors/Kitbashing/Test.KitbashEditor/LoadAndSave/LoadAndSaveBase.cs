using System;
using Editors.KitbasherEditor.UiCommands;
using Editors.KitbasherEditor.ViewModels;
using GameWorld.Core.Rendering.Materials.Capabilities;
using GameWorld.Core.Rendering.Materials.Shaders.SpecGloss;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Services.SceneSaving;
using Shared.Core.Events;
using Shared.Core.Settings;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.GameFormats.RigidModel.Types;
using Shared.Ui.Events.UiCommands;
using Test.TestingUtility.Shared;
using Test.TestingUtility.TestUtility;

namespace Test.KitbashEditor.LoadAndSave
{

    public static class TestFiles
    {
        public static readonly string KarlPackFile = PathHelper.GetDataFile("Karl_and_celestialgeneral.pack");

        public static readonly string RomePackFileFolder = PathHelper.GetDataFolder("Data\\Rome_Man_And_Shield_Pack");
        public static readonly string RomePack_MeshDecal = "variantmeshes/variantmeshes/_variantmodels/man/weapons/att_cha_pennant_spear_qua_circ.rigid_model_v2";
        public static readonly string RomePack_MeshDecalDirt = "variantmeshes/variantmeshes/_variantmodels/man/shield/att_cha_umayyad_shield1.rigid_model_v2";
        public static readonly string RomePack_MeshDirt = "variantmeshes/variantmeshes/_variantmodels/man/bosses/att_celts_large_curved_flat_boss_for_medium_shield.rigid_model_v2";
        public static readonly string RomePack_MeshSkin = "variantmeshes/variantmeshes/_variantmodels/man/skin/att_bel_vandal_cine_cut_2.rigid_model_v2";
        public static readonly string RomePack_MeshSkinDirt = "variantmeshes/variantmeshes/_variantmodels/man/skin/att_celts_germanic_01_skin02_woad_cut_3.rigid_model_v2";
        public static readonly string RomePack_MeshHelmet = "variantmeshes/_variantmodels/man/helmets/carthaginian_pylos.rigid_model_v2";

        public static readonly string RmvFilePathKarl = @"variantmeshes\wh_variantmodels\hu1\emp\emp_karl_franz\emp_karl_franz.rigid_model_v2";
        public static readonly string WsFilePathKarl = @"variantmeshes\wh_variantmodels\hu1\emp\emp_karl_franz\emp_karl_franz.wsmodel";
    }

    internal class LoadAndSaveBase
    {
        public (AssetEditorTestRunner Runner, KitbasherViewModel Editor) CreateKitbashTool(string meshPath)
        {
            var runner = new AssetEditorTestRunner(GameTypeEnum.Rome2);
            runner.CreateCaContainer();
            runner.LoadFolderPackFile(TestFiles.RomePackFileFolder);
            var outputPackFile = runner.CreateOutputPack();

            // Load the a rmv2 and open the kitbash editor
            var editorHandle = runner.CommandFactory.Create<OpenEditorCommand>().Execute(meshPath);
            var editor = editorHandle as KitbasherViewModel;

            return (runner, editor!);
        }

        public (SpecGlossCapability Main, AdvancedMaterialCapability Adv, UiVertexFormat Vert) GetMaterials(KitbasherViewModel editor, int index = 0)
        {
            // Get the first mesh and find the needed data structures 
            var sceneManager = editor!.SceneExplorer.SceneManager;
            var meshNode = SceneNodeHelper.GetChildrenOfType<Rmv2MeshNode>(sceneManager.RootNode)[index];

            var meshMaterial = (meshNode.Material as AdvancedRmvMaterial)!;
            var mainCap = meshMaterial.GetCapability<SpecGlossCapability>();
            var advCap = meshMaterial.GetCapability<AdvancedMaterialCapability>();

            return (mainCap, advCap, meshNode.Geometry.VertexFormat);
        }

        public MainEditableNode GetMainNode(KitbasherViewModel editor)
        {
            var sceneManager = editor!.SceneExplorer.SceneManager;
            var mainNode = SceneNodeHelper.GetChildrenOfType<MainEditableNode>(sceneManager.RootNode).First();
            return mainNode;
        }

        public WeightedMaterial SaveAndGetMaterial(AssetEditorTestRunner runner)
        {
            // Edit the save settings and trigger a save
            var saveSettings = runner.GetRequiredServiceInCurrentEditorScope<GeometrySaveSettings>();
            saveSettings.IsUserInitialized = true;

            var toolCommandFactory = runner.GetRequiredServiceInCurrentEditorScope<IUiCommandFactory>();
            var saveResult = toolCommandFactory.Create<SaveCommand>().ExecuteWithResult();

            Assert.That(saveResult, Is.Not.Null);
            Assert.That(saveResult!.GeneratedMesh, Is.Not.Null);

            var mesh = saveResult!.GeneratedMesh!.ModelList[0][0];
            var meshMaterial = mesh.Material as WeightedMaterial;
            return meshMaterial!;
        }

        public void AssertTexture(WeightedMaterial material, TextureType textureType, bool expected)
        {
            var foundAndNotEmpty = false;
            foreach (var texture in material.TexturesParams)
            {
                if (texture.TexureType == textureType)
                {
                    if (texture.Path.Length != 0)
                        foundAndNotEmpty = true;
                }
            }

            if (expected && foundAndNotEmpty == false)
                Assert.Fail($"Texture {textureType} not found ");
        }

        public void AssertParameterList<T>(ParamList<T> paramList, params T[] expected)
        {
            var paramValues = paramList.Values
                .OrderBy(x => x.Index).Select(x => x.Value)
                .ToArray();

            Assert.That(paramValues.Length, Is.EqualTo(expected.Length));

            for (var i = 0; i < expected.Length; i++)
                Assert.That(paramValues[i], Is.EqualTo(expected[i]));
        }
    }
}
