using System.Text;
using System.Text.RegularExpressions;
using Editors.KitbasherEditor.ChildEditors.SaveDialog;
using Editors.KitbasherEditor.ViewModels;
using GameWorld.Core.Rendering.Materials.Capabilities;
using GameWorld.Core.Services.SceneSaving;
using Shared.Core.Commands;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.Types;
using Shared.GameFormats.WsModel;
using Test.TestingUtility.Shared;

namespace Test.KitbashEditor.LoadAndSave
{
    internal class LoadAndSave_WsModel : LoadAndSaveBase
    {
        //WsOld
        // WS warhammer 3
        // No ws

        const string KarlBodyMaterial = "VariantMeshes/wh_variantmodels/hu1/emp/emp_karl_franz/materials/emp_karl_franz_body_01_weighted4_alpha_off.xml.material";

        [Test]
        public void Warhammer3_SaveKarl_Default()
        {
            var runner = new AssetEditorTestRunner();
            runner.CreateCaContainer();
            var outputPackFile = runner.LoadPackFile(TestFiles.KarlPackFile, true, loadAsGamePack: true);

            // Load the a rmv2 and open the kitbash editor
            var originalRmv2File = runner.PackFileService.FindFile(TestFiles.RmvFilePathKarl);
            runner.CommandFactory.Create<OpenEditorCommand>().Execute(originalRmv2File);

            // Edit the save settings and trigger a save
            var saveSettings = runner.GetRequiredServiceInCurrentEditorScope<GeometrySaveSettings>();
            saveSettings.IsUserInitialized = true;
            saveSettings.DisplayDialogOnNextSave = false;

            var toolCommandFactory = runner.GetRequiredServiceInCurrentEditorScope<IUiCommandFactory>();
            toolCommandFactory.Create<SaveCommand>().Execute();

            // Verify output files
            Assert.That(outputPackFile!.GetFileCount(), Is.EqualTo(2));

            // Verify the generated RMV2 file
            uint[] expectedMeshCountPerLod = [4, 4, 2, 2];
            uint[] vertexCount = [36150, 23496, 10848, 3612];
            VertexFormat[][] expectedVertexType = [
                [VertexFormat.Cinematic, VertexFormat.Cinematic, VertexFormat.Cinematic, VertexFormat.Weighted],
                [VertexFormat.Cinematic, VertexFormat.Cinematic, VertexFormat.Cinematic, VertexFormat.Weighted],
                [VertexFormat.Weighted, VertexFormat.Weighted ],
                [VertexFormat.Weighted, VertexFormat.Weighted ]];
            bool[][] alpha = [
                [false, true, false, true],
                [false, true, false, true],
                [false, false],
                [false, false]];

            // Assert
            var rmv2File = runner.PackFileService.FindFile(TestFiles.RmvFilePathKarl, outputPackFile);
            var rmv = RmvHelper.AssertFile(rmv2File, RmvVersionEnum.RMV2_V7, 4, "humanoid01");
            RmvHelper.AssertGeometryFile(rmv, 4, [4, 4, 2, 2], vertexCount);
            RmvHelper.AssertMaterial(rmv, 4, expectedVertexType, alpha, ModelMaterialEnum.weighted);

            // Verify wsmodel
            var wsModelFile = runner.PackFileService.FindFile(TestFiles.WsFilePathKarl, outputPackFile);
            WsModelHelper.AssertFile(wsModelFile);
        }

        [Test]
        public void Warhammer3_SaveKarl_HandTunedMaterialIsKept()
        {
            var (runner, outputPackFile, _, tunedContent) = OpenKarlWithHandTunedBody();

            Save(runner);

            // LOD 1 is generated from LOD 0, so it shares the file
            Assert.That(GetMaterialPath(runner, outputPackFile, 0, 0), Is.EqualTo(KarlBodyMaterial));
            Assert.That(GetMaterialPath(runner, outputPackFile, 1, 0), Is.EqualTo(KarlBodyMaterial));
            Assert.That(ReadFile(runner, KarlBodyMaterial, outputPackFile), Is.EqualTo(tunedContent));
            Assert.That(outputPackFile.GetFileCount(), Is.EqualTo(3)); // rmv2, wsmodel and the tuned material
        }

        [Test]
        public void Warhammer3_SaveKarl_EditedHandTunedMaterialIsRegeneratedAndTunedFileKept()
        {
            var (runner, outputPackFile, editor, tunedContent) = OpenKarlWithHandTunedBody();
            var body = GetMainNode(editor).GetMeshesInLod(0, false)[0];
            body.Material.GetCapability<MetalRoughCapability>().BaseColour.TexturePath = "textures/karl_repainted.dds";

            Save(runner);

            var bodyMaterialPath = GetMaterialPath(runner, outputPackFile, 0, 0);
            Assert.That(bodyMaterialPath, Is.Not.EqualTo(KarlBodyMaterial));
            var bodyMaterial = new WsModelMaterialFile(runner.PackFileService.FindFile(bodyMaterialPath, outputPackFile)!);
            Assert.That(bodyMaterial.Textures[TextureType.BaseColour], Is.EqualTo("textures/karl_repainted.dds"));
            Assert.That(ReadFile(runner, KarlBodyMaterial, outputPackFile), Is.EqualTo(tunedContent));
        }

        // Karl with his body material tuned by hand in the output pack: a shader and a param the template has no place for
        (AssetEditorTestRunner Runner, IPackFileContainer Output, KitbasherViewModel Editor, string TunedContent) OpenKarlWithHandTunedBody()
        {
            var runner = new AssetEditorTestRunner();
            runner.CreateCaContainer();
            var outputPackFile = runner.LoadPackFile(TestFiles.KarlPackFile, true, loadAsGamePack: true)!;

            var vanillaContent = ReadFile(runner, KarlBodyMaterial);
            var tunedContent = Regex.Replace(vanillaContent, @"(<shader>[^<]*)\.xml\.shader</shader>", "$1_hair.xml.shader</shader>");
            tunedContent = tunedContent.Replace("</params>", "<param><name>hand_tuned_sheen</name><type>float</type><value>0.35</value></param></params>");
            Assert.That(tunedContent, Does.Contain("_hair.xml.shader"));
            Assert.That(tunedContent, Does.Contain("hand_tuned_sheen"));

            var directory = KarlBodyMaterial[..KarlBodyMaterial.LastIndexOf('/')];
            var fileName = KarlBodyMaterial[(KarlBodyMaterial.LastIndexOf('/') + 1)..];
            runner.PackFileService.AddFilesToPack(outputPackFile, [new NewPackFileEntry(directory, PackFile.CreateFromASCII(fileName, tunedContent))]);
            Assert.That(ReadFile(runner, KarlBodyMaterial), Is.EqualTo(tunedContent));

            var rmv2File = runner.PackFileService.FindFile(TestFiles.RmvFilePathKarl)!;
            var editor = (KitbasherViewModel)runner.CommandFactory.Create<OpenEditorCommand>().Execute(rmv2File);
            return (runner, outputPackFile, editor, tunedContent);
        }

        static void Save(AssetEditorTestRunner runner)
        {
            var saveSettings = runner.GetRequiredServiceInCurrentEditorScope<GeometrySaveSettings>();
            saveSettings.IsUserInitialized = true;
            saveSettings.DisplayDialogOnNextSave = false;
            runner.GetRequiredServiceInCurrentEditorScope<IUiCommandFactory>().Create<SaveCommand>().Execute();
        }

        static string ReadFile(AssetEditorTestRunner runner, string path, IPackFileContainer? pack = null)
        {
            var file = runner.PackFileService.FindFile(path, pack);
            Assert.That(file, Is.Not.Null, path);
            return Encoding.UTF8.GetString(file!.DataSource.ReadData());
        }

        static string GetMaterialPath(AssetEditorTestRunner runner, IPackFileContainer outputPackFile, int lodIndex, int partIndex)
        {
            var wsModelFile = runner.PackFileService.FindFile(TestFiles.WsFilePathKarl, outputPackFile);
            Assert.That(wsModelFile, Is.Not.Null);
            var wsModel = new WsModelFile(wsModelFile!);
            return wsModel.MaterialList.Single(x => x.LodIndex == lodIndex && x.PartIndex == partIndex).MaterialPath;
        }
    }
}
