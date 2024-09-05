using E2EVerification.Shared;
using Editors.KitbasherEditor.UiCommands;
using GameWorld.Core.Services.SceneSaving;
using GameWorld.Core.Services.SceneSaving.Lod;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.Events;
using Shared.GameFormats.RigidModel;
using Shared.Ui.Events.UiCommands;

namespace E2EVerification
{
    public class KitbashEditor_SaveTests
    {
        private readonly string _inputPackFileKarl = PathHelper.FileInDataFolder("Karl_and_celestialgeneral.pack");
        private readonly string _rmvFilePathKarl = @"variantmeshes\wh_variantmodels\hu1\emp\emp_karl_franz\emp_karl_franz.rigid_model_v2";
        private readonly string _wsFilePathKarl = @"variantmeshes\wh_variantmodels\hu1\emp\emp_karl_franz\emp_karl_franz.wsmodel";

        [SetUp]
        public void Setup()
        {
        }

        // Save scene one lod originally- verify lods
        // Save scene dragon - - verify lod

        [Test]
        public void SaveKarl_Default()
        {
            var runner = new AssetEditorTestRunner();
            var outputPackFile = runner.LoadPackFile(_inputPackFileKarl, true);

            // Load the a rmv2 and open the kitbash editor
            var originalRmv2File = runner.PackFileService.FindFile(_rmvFilePathKarl);
            runner.CommandFactory.Create<OpenFileInEditorCommand>().Execute(originalRmv2File);

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
            uint[] expectedMeshCountPerLod = [4,4,2,2];
            uint[] vertexCount = [36150, 20646, 13734, 6867];
            VertexFormat[][] expectedVertexType = [
                [VertexFormat.Cinematic, VertexFormat.Cinematic, VertexFormat.Cinematic, VertexFormat.Weighted],
                [VertexFormat.Cinematic, VertexFormat.Cinematic, VertexFormat.Cinematic, VertexFormat.Weighted],
                [VertexFormat.Weighted, VertexFormat.Weighted ],
                [VertexFormat.Weighted, VertexFormat.Weighted ]];
            AlphaMode[][] alpha = [
                [AlphaMode.Opaque, AlphaMode.Transparent, AlphaMode.Opaque, AlphaMode.Transparent],
                [AlphaMode.Opaque, AlphaMode.Transparent, AlphaMode.Opaque, AlphaMode.Transparent],
                [AlphaMode.Opaque, AlphaMode.Opaque],
                [AlphaMode.Opaque, AlphaMode.Opaque]];

            var rmv2File = runner.PackFileService.FindFile(_rmvFilePathKarl, outputPackFile);
            RmvHelper.AssertFile(rmv2File, expectedMeshCountPerLod, vertexCount, expectedVertexType, alpha);

            // Verify wsmodel
            var wsModelFile = runner.PackFileService.FindFile(_wsFilePathKarl, outputPackFile);
            WsModelHelper.AssertFile(wsModelFile);
        }

        [Test]
        public void SaveKarl_Lod0ForAll()
        {
            var runner = new AssetEditorTestRunner();
            var outputPackFile = runner.LoadPackFile(_inputPackFileKarl, true);

            // Load the a rmv2 and open the kitbash editor
            var originalRmv2File = runner.PackFileService.FindFile(_rmvFilePathKarl);
            runner.CommandFactory.Create<OpenFileInEditorCommand>().Execute(originalRmv2File);

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
            AlphaMode[][] alpha = [
                [AlphaMode.Opaque, AlphaMode.Transparent, AlphaMode.Opaque, AlphaMode.Transparent],
                [AlphaMode.Opaque, AlphaMode.Transparent, AlphaMode.Opaque, AlphaMode.Transparent],
                [AlphaMode.Opaque, AlphaMode.Transparent, AlphaMode.Opaque, AlphaMode.Transparent],
                [AlphaMode.Opaque, AlphaMode.Transparent, AlphaMode.Opaque, AlphaMode.Transparent]];

            var rmv2File = runner.PackFileService.FindFile(_rmvFilePathKarl, outputPackFile);
            RmvHelper.AssertFile(rmv2File, expectedMeshCountPerLod, vertexCount, expectedVertexType, alpha);

            // Verify wsmodel
            var wsModelFile = runner.PackFileService.FindFile(_wsFilePathKarl, outputPackFile);
            WsModelHelper.AssertFile(wsModelFile);       
        }

        public void LoadAndSaveDecalAndDirtShield()
        { 
        
        }


    }
}
