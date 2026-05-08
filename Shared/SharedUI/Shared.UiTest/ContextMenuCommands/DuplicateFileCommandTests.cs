using System.Threading;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;
using Test.TestingUtility.Shared;

namespace Shared.UiTest.ContextMenuCommands
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    internal class DuplicateFileCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_IsEnabled_Execute()
        {
            var runner = new AssetEditorTestRunner();
            runner.PackFileService.EnforceGameFilesMustBeLoaded = false;
            var sourcePackFile = runner.CreateEmptyPackFile("SourcePack", false);
            var outputPackFile = runner.CreateEmptyPackFile("OutputPack", true);

            var fileEntry = new NewPackFileEntry("Animation\\Meta", PackFile.CreateFromASCII("testFile.anm", "DummyContent"));
            runner.PackFileService.AddFilesToPack(sourcePackFile, [fileEntry]);
            var fileToCopy = runner.PackFileService.FindFile("Animation\\Meta\\testFile.anm", sourcePackFile)!;

            var node = new TreeNode(fileToCopy.Name, NodeType.File, sourcePackFile, null, fileToCopy);
            var command = runner.CommandFactory.Create<DuplicateFileCommand>();

            Assert.That(command.ShouldAdd(node), Is.True);
            Assert.That(command.IsEnabled(node), Is.True);

            command.Execute(node);

            var foundFile = runner.PackFileService.FindFile("Animation\\Meta\\testFile_copy.anm", outputPackFile);
            Assert.That(foundFile, Is.Not.Null);
        }
    }
}
