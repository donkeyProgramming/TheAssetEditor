using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Editors.KitbasherEditor.UiCommands;
using GameWorld.Core.Services.SceneSaving;
using GameWorld.Core.Services.SceneSaving.Lod;
using Shared.Core.Events;
using Shared.GameFormats.RigidModel;
using Shared.Ui.Events.UiCommands;
using Test.TestingUtility.Shared;

namespace Test.KitbashEditor.LoadAndSave
{
    internal class LoadAndSave_Geometry
    {
        // No geo
        // Only visible
        // rmv8
        // Rmv7
        // Rmv6
        // Validate pivot and matrix shit




        [Test]
        public void Warhammer3_SaveKarl_Default()
        {
            var runner = new AssetEditorTestRunner();
            runner.CreateCaContainer();
            var outputPackFile = runner.LoadPackFile(TestFiles.KarlPackFile, true);

            // Load the a rmv2 and open the kitbash editor
            var originalRmv2File = runner.PackFileService.FindFile(TestFiles.RmvFilePathKarl);
            runner.CommandFactory.Create<OpenEditorCommand>().Execute(originalRmv2File);

            // Edit the save settings and trigger a save
            var saveSettings = runner.GetRequiredServiceInCurrentEditorScope<GeometrySaveSettings>();
            saveSettings.IsUserInitialized = true;

            var toolCommandFactory = runner.GetRequiredServiceInCurrentEditorScope<IUiCommandFactory>();
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
            var rmv2File = runner.PackFileService.FindFile(TestFiles.RmvFilePathKarl, outputPackFile);
            var rmv = RmvHelper.AssertFile(rmv2File, RmvVersionEnum.RMV2_V7, 4, "humanoid01");
            RmvHelper.AssertGeometryFile(rmv, 4, [4, 4, 2, 2], vertexCount);
            RmvHelper.AssertMaterial(rmv, 4, expectedVertexType, alpha, ModelMaterialEnum.weighted);

            // Verify wsmodel
            var wsModelFile = runner.PackFileService.FindFile(TestFiles.WsFilePathKarl, outputPackFile);
            WsModelHelper.AssertFile(wsModelFile);
        }

        [Test]
        public void Warhammer3_SaveKarl_Lod0ForAll()
        {
            var runner = new AssetEditorTestRunner();
            runner.CreateCaContainer();
            var outputPackFile = runner.LoadPackFile(TestFiles.KarlPackFile, true);

            // Load the a rmv2 and open the kitbash editor
            var originalRmv2File = runner.PackFileService.FindFile(TestFiles.RmvFilePathKarl);
            runner.CommandFactory.Create<OpenEditorCommand>().Execute(originalRmv2File);

            // Edit the save settings and trigger a save
            var saveSettings = runner.GetRequiredServiceInCurrentEditorScope<GeometrySaveSettings>();
            saveSettings.IsUserInitialized = true;
            saveSettings.LodGenerationMethod = LodStrategy.Lod0ForAll;

            var toolCommandFactory = runner.GetRequiredServiceInCurrentEditorScope<IUiCommandFactory>();
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
            var rmv2File = runner.PackFileService.FindFile(TestFiles.RmvFilePathKarl, outputPackFile);
            var rmv = RmvHelper.AssertFile(rmv2File, RmvVersionEnum.RMV2_V7, 4, "humanoid01");
            RmvHelper.AssertGeometryFile(rmv, 4, [4, 4, 4, 4], vertexCount);
            RmvHelper.AssertMaterial(rmv, 4, expectedVertexType, alpha, ModelMaterialEnum.weighted);

            // Verify wsmodel
            var wsModelFile = runner.PackFileService.FindFile(TestFiles.WsFilePathKarl, outputPackFile);
            WsModelHelper.AssertFile(wsModelFile);
        }
    }
}
