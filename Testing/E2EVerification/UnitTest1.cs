using E2EVerification.Shared;
using GameWorld.Core.Services.SceneSaving;
using KitbasherEditor.ViewModels.UiCommands;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.Events;
using Shared.GameFormats.RigidModel;
using Shared.Ui.Events.UiCommands;

namespace E2EVerification
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        // Save scene dragon - - verify lods
        // Save scenee karl - verify lods, verify wsModel Generatiron
        // Save scenee karl - Lod for all
        // Save scene one lod originally- verify lods

        [Test]
        public void SaveScene()
        {
            var runner = new AssetEditorTestRunner();
            var outputFile = runner.LoadPackFile("C:\\Users\\ole_k\\source\\repos\\TheAssetEditor\\Data\\Karl_and_celestialgeneral.pack", true);

            // Load the a rmv2 and open the kitbash editor
            var file = runner.PackFileService.FindFile(@"variantmeshes\wh_variantmodels\hu1\emp\emp_karl_franz\emp_karl_franz.rigid_model_v2");
            runner.CommandFactory.Create<OpenFileInEditorCommand>().Execute(file);

            // Get the scope of the newly created kitbash editor
            var kitbashScope = runner.ScopeRepository.Scopes.First().Value.ServiceProvider;

            // Edit the save settings and trigger a save
            var saveSettings = kitbashScope.GetRequiredService<GeometrySaveSettings>();
            saveSettings.IsUserInitialized = true;

            var toolCommandFactory = kitbashScope.GetRequiredService<IUiCommandFactory>();
            toolCommandFactory.Create<SaveCommand>().Execute();

            // Verify
            Assert.That(outputFile.FileList.Count, Is.EqualTo(2));

            var rmv2 = ModelFactory.Create().Load(null);

            // Verify Mesh generated
            // Verify Lod generated
            // verify WS model generated
        }


    }
}
