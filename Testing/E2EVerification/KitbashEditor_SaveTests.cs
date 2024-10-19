using E2EVerification.Shared;
using Editors.KitbasherEditor.UiCommands;
using GameWorld.Core.Services.SceneSaving;
using GameWorld.Core.Services.SceneSaving.Lod;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.Events;
using Shared.Core.Services;
using Shared.GameFormats.RigidModel;
using Shared.TestUtility;
using Shared.Ui.Events.UiCommands;

namespace E2EVerification
{
    public class KitbashEditor_SaveTests
    {
        private readonly string _inputPackFileKarl = PathHelper.File("Karl_and_celestialgeneral.pack");
        private readonly string _rmvFilePathKarl = @"variantmeshes\wh_variantmodels\hu1\emp\emp_karl_franz\emp_karl_franz.rigid_model_v2";
        private readonly string _wsFilePathKarl = @"variantmeshes\wh_variantmodels\hu1\emp\emp_karl_franz\emp_karl_franz.wsmodel";

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Warhammer3_SaveKarl_Default()
        {
            var runner = new AssetEditorTestRunner();
            var outputPackFile = runner.LoadPackFile(_inputPackFileKarl, true);

            // Load the a rmv2 and open the kitbash editor
            var originalRmv2File = runner.PackFileService.FindFile(_rmvFilePathKarl);
            runner.CommandFactory.Create<OpenEditorCommand>().Execute(originalRmv2File);

            // Get the scope of the newly created kitbash editor
            var kitbashScope = runner.ScopeRepository.Scopes.First().Value.ServiceProvider;

            // Edit the save settings and trigger a save
            var saveSettings = kitbashScope.GetRequiredService<GeometrySaveSettings>();
            saveSettings.IsUserInitialized = true;

            var toolCommandFactory = kitbashScope.GetRequiredService<IUiCommandFactory>();
            toolCommandFactory.Create<SaveCommand>().Execute();

            // Verify output files
            Assert.That(outputPackFile!.FileList.Count, Is.EqualTo(2));

            // Verify the generated RMV2 file
            uint[] expectedMeshCountPerLod = [4, 4, 2, 2];
            uint[] vertexCount = [36150, 20646, 13734, 6867];
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
            var rmv2File = runner.PackFileService.FindFile(_rmvFilePathKarl, outputPackFile);
            var rmv = RmvHelper.AssertFile(rmv2File, RmvVersionEnum.RMV2_V7, 4, "humanoid01");
            RmvHelper.AssertGeometryFile(rmv, 4, [4, 4, 2, 2], vertexCount);
            RmvHelper.AssertMaterial(rmv, 4, expectedVertexType, alpha, ModelMaterialEnum.weighted);

            // Verify wsmodel
            var wsModelFile = runner.PackFileService.FindFile(_wsFilePathKarl, outputPackFile);
            WsModelHelper.AssertFile(wsModelFile);
        }

        [Test]
        public void Warhammer3_SaveKarl_Lod0ForAll()
        {
            var runner = new AssetEditorTestRunner();
            var outputPackFile = runner.LoadPackFile(_inputPackFileKarl, true);

            // Load the a rmv2 and open the kitbash editor
            var originalRmv2File = runner.PackFileService.FindFile(_rmvFilePathKarl);
            runner.CommandFactory.Create<OpenEditorCommand>().Execute(originalRmv2File);

            // Get the scope of the newly created kitbash editor
            var kitbashScope = runner.ScopeRepository.Scopes.First().Value.ServiceProvider;

            // Edit the save settings and trigger a save
            var saveSettings = kitbashScope.GetRequiredService<GeometrySaveSettings>();
            saveSettings.IsUserInitialized = true;
            saveSettings.LodGenerationMethod = LodStrategy.Lod0ForAll;

            var toolCommandFactory = kitbashScope.GetRequiredService<IUiCommandFactory>();
            toolCommandFactory.Create<SaveCommand>().Execute();

            // Verify output files
            Assert.That(outputPackFile!.FileList.Count, Is.EqualTo(2));

            // Verify the generated RMV2 file
            uint[] expectedMeshCountPerLod = [4, 4, 4, 4];
            uint[] vertexCount = [36150, 36150, 36150, 36150];
            VertexFormat[][] expectedVertexType = [
                [VertexFormat.Cinematic, VertexFormat.Cinematic, VertexFormat.Cinematic, VertexFormat.Weighted],
                [VertexFormat.Cinematic, VertexFormat.Cinematic, VertexFormat.Cinematic, VertexFormat.Weighted],
                [VertexFormat.Cinematic, VertexFormat.Cinematic, VertexFormat.Cinematic, VertexFormat.Weighted],
                [VertexFormat.Cinematic, VertexFormat.Cinematic, VertexFormat.Cinematic, VertexFormat.Weighted]];
            bool[][] alpha = [
                [false, true, false, true],
                [false, true, false, true],
                [false, true, false, true],
                [false, true, false, true]];

            // Assert
            var rmv2File = runner.PackFileService.FindFile(_rmvFilePathKarl, outputPackFile);
            var rmv = RmvHelper.AssertFile(rmv2File, RmvVersionEnum.RMV2_V7, 4, "humanoid01");
            RmvHelper.AssertGeometryFile(rmv, 4, [4, 4, 4, 4], vertexCount);
            RmvHelper.AssertMaterial(rmv, 4, expectedVertexType, alpha, ModelMaterialEnum.weighted);

            // Verify wsmodel
            var wsModelFile = runner.PackFileService.FindFile(_wsFilePathKarl, outputPackFile);
            WsModelHelper.AssertFile(wsModelFile);
        }

        [Test]
        public void Rome2_LoadAndSaveDirtHelmet()
        {
            var runner = new AssetEditorTestRunner(GameTypeEnum.Rome_2);
            var outputPackFile = runner.LoadFolderPackFile(PathHelper.Folder("Rome_Man_And_Shield_Pack"), true);

            // Load the a rmv2 and open the kitbash editor
            var meshPath = "variantmeshes/_variantmodels/man/helmets/carthaginian_pylos.rigid_model_v2";
            var originalRmv2File = runner.PackFileService.FindFile(meshPath);
            runner.CommandFactory.Create<OpenEditorCommand>().Execute(originalRmv2File);

            // Get the scope of the newly created kitbash editor
            var kitbashScope = runner.ScopeRepository.Scopes.First().Value.ServiceProvider;

            // Edit the save settings and trigger a save
            var saveSettings = kitbashScope.GetRequiredService<GeometrySaveSettings>();
            saveSettings.IsUserInitialized = true;

            var toolCommandFactory = kitbashScope.GetRequiredService<IUiCommandFactory>();
            toolCommandFactory.Create<SaveCommand>().Execute();

            // Verify output files
            Assert.That(outputPackFile!.FileList.Count, Is.EqualTo(1));

            // Verify the generated RMV2 file
            VertexFormat[][] expectedVertexType = [ [VertexFormat.Weighted],[VertexFormat.Weighted],[VertexFormat.Weighted],[VertexFormat.Weighted]];
            bool[][] alpha = [ [true],[true], [false], [false]];

            var rmv2File = runner.PackFileService.FindFile(meshPath, outputPackFile);
            
            var rmv = RmvHelper.AssertFile(rmv2File, RmvVersionEnum.RMV2_V6, 4, "rome_man_game");
            RmvHelper.AssertMaterial(rmv, 4, expectedVertexType, alpha, ModelMaterialEnum.weighted_dirtmap);
        }

        [Test]
        public void Rome2_LoadAndSaveDirtAndDecalShield()
        {
            var runner = new AssetEditorTestRunner(GameTypeEnum.Rome_2);
            var outputPackFile = runner.LoadFolderPackFile(PathHelper.Folder("Rome_Man_And_Shield_Pack"), true);

            // Load the a rmv2 and open the kitbash editor
            var meshPath = "variantmeshes/_variantmodels/man/shield/celtic_oval_shield_a.rigid_model_v2";
            var originalRmv2File = runner.PackFileService.FindFile(meshPath);
            runner.CommandFactory.Create<OpenEditorCommand>().Execute(originalRmv2File);

            // Get the scope of the newly created kitbash editor
            var kitbashScope = runner.ScopeRepository.Scopes.First().Value.ServiceProvider;

            // Edit the save settings and trigger a save
            var saveSettings = kitbashScope.GetRequiredService<GeometrySaveSettings>();
            saveSettings.IsUserInitialized = true;

            var toolCommandFactory = kitbashScope.GetRequiredService<IUiCommandFactory>();
            toolCommandFactory.Create<SaveCommand>().Execute();

            // Verify output files
            Assert.That(outputPackFile!.FileList.Count, Is.EqualTo(1));

            // Verify the generated RMV2 file
            VertexFormat[][] expectedVertexType = [[VertexFormat.Static], [VertexFormat.Static], [VertexFormat.Static]];
            bool[][] alpha = [[false], [false], [false]];

            var rmv2File = runner.PackFileService.FindFile(meshPath, outputPackFile);

            var rmv = RmvHelper.AssertFile(rmv2File, RmvVersionEnum.RMV2_V6, 3, "");
            RmvHelper.AssertMaterial(rmv, 3, expectedVertexType, alpha, ModelMaterialEnum.decal_dirtmap);
        }
    }
}
