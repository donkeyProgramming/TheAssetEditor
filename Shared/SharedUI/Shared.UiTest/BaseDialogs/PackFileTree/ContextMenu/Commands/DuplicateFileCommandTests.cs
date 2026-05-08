using System.Threading;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;
using Test.TestingUtility.Shared;

namespace Shared.UiTest.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    [TestFixture]
    internal class DuplicateFileCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_ReturnsTrueForFileNode()
        {
            var runner = new AssetEditorTestRunner();
            runner.PackFileService.EnforceGameFilesMustBeLoaded = false;
            var sourcePackFile = runner.CreateEmptyPackFile("SourcePack", false);
            var fileEntry = new NewPackFileEntry("Animation\\Meta", PackFile.CreateFromASCII("testFile.anm", "DummyContent"));
            runner.PackFileService.AddFilesToPack(sourcePackFile, [fileEntry]);
            var fileToCopy = runner.PackFileService.FindFile("Animation\\Meta\\testFile.anm", sourcePackFile)!;
            var node = new TreeNode(fileToCopy.Name, NodeType.File, sourcePackFile, null, fileToCopy);
            var command = runner.CommandFactory.Create<DuplicateFileCommand>();

            Assert.That(command.ShouldAdd(node), Is.True);
        }

        [Test]
        public void IsEnabled_ReturnsTrue()
        {
            var runner = new AssetEditorTestRunner();
            runner.PackFileService.EnforceGameFilesMustBeLoaded = false;
            var sourcePackFile = runner.CreateEmptyPackFile("SourcePack", false);
            var fileEntry = new NewPackFileEntry("Animation\\Meta", PackFile.CreateFromASCII("testFile.anm", "DummyContent"));
            runner.PackFileService.AddFilesToPack(sourcePackFile, [fileEntry]);
            var fileToCopy = runner.PackFileService.FindFile("Animation\\Meta\\testFile.anm", sourcePackFile)!;
            var node = new TreeNode(fileToCopy.Name, NodeType.File, sourcePackFile, null, fileToCopy);
            var command = runner.CommandFactory.Create<DuplicateFileCommand>();

            Assert.That(command.IsEnabled(node), Is.True);
        }

        [Test]
        public void Execute_DuplicatesFileIntoEditablePack()
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

            command.Execute(node);

            var foundFile = runner.PackFileService.FindFile("Animation\\Meta\\testFile_copy.anm", outputPackFile);
            Assert.That(foundFile, Is.Not.Null);
        }

        [Test]
        [TestCase("testFile", "testFile_copy")]            // No extension
        [TestCase("testFile.anm", "testFile_copy.anm")]        // Single extension 
        [TestCase("testFile.anm.meta", "testFile_copy.anm.meta")]   // Double extension 
        public void DuplicateFileCommand(string fileName, string result)
        {
            var runner = new AssetEditorTestRunner();
            runner.PackFileService.EnforceGameFilesMustBeLoaded = false;
            var sourcePackFile = runner.CreateEmptyPackFile("SourcePack", false);
            var outputPackFile = runner.CreateEmptyPackFile("OutputPack", true);

            var fileEntry = new NewPackFileEntry("Animation\\Meta", PackFile.CreateFromASCII(fileName, "DummyContent"));
            runner.PackFileService.AddFilesToPack(sourcePackFile, [fileEntry]);
            var fileToCopy = runner.PackFileService.FindFile("Animation\\Meta\\" + fileName, sourcePackFile);

            runner.CommandFactory.Create<DuplicateFileCommand>().Execute(fileToCopy);
            var foundFile = runner.PackFileService.FindFile("Animation\\Meta\\" + result, outputPackFile);
            Assert.That(foundFile, Is.Not.Null);
        }
    }
}
}
