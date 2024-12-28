using AssetEditor.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Test.TestingUtility.Shared;
using Test.TestingUtility.TestUtility;

namespace Test.E2EVerification
{
    internal class PackFileBrowser
    {
        private readonly string _inputPackFileKarl = PathHelper.GetDataFolder("Data\\Karl_and_celestialgeneral_Pack");

        [Test]
        public void DragAndDrop()
        {
            // Arrange
            var runner = new AssetEditorTestRunner();
            runner.CreateCaContainer();
            var outputPackFile = runner.LoadFolderPackFile(_inputPackFileKarl);

            var mainApplicationView = runner.ServiceProvider.GetRequiredService<MainViewModel>();
            var treeView = mainApplicationView.FileTree;
            var packRootNode = treeView.Files[1];

            // Act
            var fileToMove = treeView.GetFromPath(packRootNode, @"animations\battle\humanoid01\2handed_hammer\stand\hu1_2hh_stand_idle_01.anim");
            var destinationNode = treeView.GetFromPath(packRootNode, @"animations");
            treeView.Drop(fileToMove, destinationNode);

            // Assert
            // Get file in packfileservice
            var movedFile = runner.PackFileService.FindFile(@"animations\hu1_2hh_stand_idle_01.anim");
            Assert.That(movedFile, Is.Not.Null);

            // Get file node from 
            var movedNode = treeView.GetFromPath(packRootNode, @"animations\hu1_2hh_stand_idle_01.anim");
            Assert.That(movedNode, Is.Not.Null);
            Assert.That(movedNode.UnsavedChanged, Is.True);
            Assert.That(movedNode.Parent.UnsavedChanged, Is.True);
        }
    }
}
