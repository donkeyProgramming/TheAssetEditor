using Editors.KitbasherEditor.UiCommands;
using GameWorld.Core.Services.SceneSaving;
using GameWorld.Core.Services.SceneSaving.Lod;
using Shared.Core.Events;
using Shared.Core.Settings;
using Shared.GameFormats.RigidModel;
using Shared.Ui.Events.UiCommands;
using Test.TestingUtility.Shared;
using Test.TestingUtility.TestUtility;

namespace Test.KitbashEditor.LoadAndSave
{
    public class KitbashEditor_SaveTests
    {

        [SetUp]
        public void Setup()
        {
        }


     

       

       /* [Test]
        public void Rome2_LoadAndSaveDirtHelmet()
        {
            var runner = new AssetEditorTestRunner(GameTypeEnum.Rome2);
            runner.CreateCaContainer();
            runner.LoadFolderPackFile(PathHelper.GetDataFolder("Data\\Rome_Man_And_Shield_Pack"));
            var outputPackFile = runner.CreateOutputPack();

            // Load the a rmv2 and open the kitbash editor
            var meshPath = "variantmeshes/_variantmodels/man/helmets/carthaginian_pylos.rigid_model_v2";
            var originalRmv2File = runner.PackFileService.FindFile(meshPath);
            runner.CommandFactory.Create<OpenEditorCommand>().Execute(originalRmv2File);

            // Edit the save settings and trigger a save
            var saveSettings = runner.GetRequiredServiceInCurrentEditorScope<GeometrySaveSettings>();
            saveSettings.IsUserInitialized = true;

            var toolCommandFactory = runner.GetRequiredServiceInCurrentEditorScope<IUiCommandFactory>();
            toolCommandFactory.Create<SaveCommand>().Execute();

            // Verify output files
            Assert.That(outputPackFile!.FileList.Count, Is.EqualTo(1));

            // Verify the generated RMV2 file
            VertexFormat[][] expectedVertexType = [[VertexFormat.Weighted], [VertexFormat.Weighted], [VertexFormat.Weighted], [VertexFormat.Weighted]];
            bool[][] alpha = [[true], [true], [false], [false]];

            var rmv2File = runner.PackFileService.FindFile(meshPath, outputPackFile);

            var rmv = RmvHelper.AssertFile(rmv2File, RmvVersionEnum.RMV2_V6, 4, "rome_man_game");
            RmvHelper.AssertMaterial(rmv, 4, expectedVertexType, alpha, ModelMaterialEnum.weighted_dirtmap);
        }

        [Test]
        public void Rome2_LoadAndSaveDirtAndDecalShield()
        {
            var runner = new AssetEditorTestRunner(GameTypeEnum.Rome2);
            runner.CreateCaContainer();
            runner.LoadFolderPackFile(PathHelper.GetDataFolder("Data\\Rome_Man_And_Shield_Pack"));
            var outputPackFile = runner.CreateOutputPack();

            // Load the a rmv2 and open the kitbash editor
            var meshPath = "variantmeshes/_variantmodels/man/shield/celtic_oval_shield_a.rigid_model_v2";
            var originalRmv2File = runner.PackFileService.FindFile(meshPath);
            runner.CommandFactory.Create<OpenEditorCommand>().Execute(originalRmv2File);

            // Edit the save settings and trigger a save
            var saveSettings = runner.GetRequiredServiceInCurrentEditorScope<GeometrySaveSettings>();
            saveSettings.IsUserInitialized = true;

            var toolCommandFactory = runner.GetRequiredServiceInCurrentEditorScope<IUiCommandFactory>();
            toolCommandFactory.Create<SaveCommand>().Execute();

            // Verify output files
            Assert.That(outputPackFile!.FileList.Count, Is.EqualTo(1));

            // Verify the generated RMV2 file
            VertexFormat[][] expectedVertexType = [[VertexFormat.Static], [VertexFormat.Static], [VertexFormat.Static]];
            bool[][] alpha = [[false], [false], [false]];

            var rmv2File = runner.PackFileService.FindFile(meshPath, outputPackFile);

            var rmv = RmvHelper.AssertFile(rmv2File, RmvVersionEnum.RMV2_V6, 3, "");
            RmvHelper.AssertMaterial(rmv, 3, expectedVertexType, alpha, ModelMaterialEnum.decal_dirtmap);
        }*/


    }
}
