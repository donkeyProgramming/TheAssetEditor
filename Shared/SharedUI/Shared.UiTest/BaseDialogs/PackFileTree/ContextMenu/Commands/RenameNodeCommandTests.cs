using Test.TestingUtility.TestUtility;
using System.Threading;
using Moq;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;
using Shared.Ui.BaseDialogs.PackFileTree.Utility;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace Shared.UiTest.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    [TestFixture]
    internal class RenameNodeCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_ReturnsTrueForFile()
        {
            var container = AddPackFiles(false, "modfile", "c:\\mymod.pack", ["rootfolder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var node = TreeNodeHelper.FindNode(viewModel, container, "rootfolder\\file.txt");

            var command = new RenameNodeCommand(_packFileService, new Mock<IStandardDialogs>().Object, MockScopedLogger.Create());

            Assert.That(command.ShouldAdd(node), Is.True);
        }

        [Test]
        public void IsEnabled_ReturnsTrue()
        {
            var container = AddPackFiles(false, "modfile", "c:\\mymod.pack", ["rootfolder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var node = TreeNodeHelper.FindNode(viewModel, container, "rootfolder\\file.txt");

            var command = new RenameNodeCommand(_packFileService, new Mock<IStandardDialogs>().Object, MockScopedLogger.Create());

            Assert.That(command.IsEnabled(node), Is.True);
        }



        [Test]
        public void Execute_RenamesFile()
        {
            // Arrange
            AddPackFiles(true, "gamefile", "root", []);
            var container = AddPackFiles(false, "modfile", "c:\\mymod.pack", ["rootfolder\\file0.txt", "rootfolder\\file1.txt"]);

            var viewModel = PackFileBrowser();
            var node = TreeNodeHelper.FindNode(viewModel, container, "rootfolder\\file0.txt");
           
            var dialogs = new Mock<IStandardDialogs>();
            dialogs.Setup(x => x.ShowTextInputDialog("Rename file", node.Name)).Returns(new TextInputDialogResult(true, "renamed.txt"));

            // Act
            var command = new RenameNodeCommand(_packFileService, dialogs.Object, MockScopedLogger.Create());
            command.Configure(node);

            command.Execute();

            // Assert
            Assert.That(node.Name, Is.EqualTo("renamed.txt"));
            var packfile = container.FindFile("rootfolder\\renamed.txt");
            Assert.That(packfile, Is.Not.Null);
            Assert.That(node.UnsavedChanged, Is.EqualTo(true));
        }

        [Test]
        public void Execute_RenamesDirectory()
        {
            // Arrange
            var container = AddPackFiles(false, "modfile", "c:\\mymod.pack", ["myfolder\\file0.txt", "myfolder\\file1.txt"]);

            var viewModel = PackFileBrowser();
            var root = viewModel.Files.First();
            var dirNode = root.Children.First(x => x.NodeType == NodeType.Directory);

            var dialogs = new Mock<IStandardDialogs>();
            dialogs.Setup(x => x.ShowTextInputDialog("Create folder", dirNode.Name)).Returns(new TextInputDialogResult(true, "renamed_folder"));

            // Act
            var command = new RenameNodeCommand(_packFileService, dialogs.Object, MockScopedLogger.Create());
            command.Configure(dirNode);

            command.Execute();

            // Assert
            Assert.That(dirNode.Name, Is.EqualTo("renamed_folder"));
            var file0 = container.FindFile("renamed_folder\\file0.txt");
            var file1 = container.FindFile("renamed_folder\\file1.txt");
            Assert.That(file0, Is.Not.Null);
            Assert.That(file1, Is.Not.Null);
        }
    }
}
