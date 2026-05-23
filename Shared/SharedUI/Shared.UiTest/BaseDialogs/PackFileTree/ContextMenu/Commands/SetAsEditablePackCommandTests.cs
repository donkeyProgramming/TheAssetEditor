using Test.TestingUtility.TestUtility;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;

namespace Shared.UiTest.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    [TestFixture]
    internal class SetAsEditablePackCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_ReturnsTrueForRoot()
        {
            AddPackFiles(false, "modfile", "c:\\mymod.pack", ["rootfolder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var root = viewModel.Files.First();

            var command = new SetAsEditablePackCommand(_packFileService, MockScopedLogger.Create());

            Assert.That(command.ShouldAdd(root), Is.True);
        }

        [Test]
        public void IsEnabled_ReturnsTrue()
        {
            AddPackFiles(false, "modfile", "c:\\mymod.pack", ["rootfolder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var root = viewModel.Files.First();

            var command = new SetAsEditablePackCommand(_packFileService, MockScopedLogger.Create());

            Assert.That(command.IsEnabled(root), Is.True);
        }

        [Test]
        public void Execute_SetsEditablePack()
        {
            // Arrange
            var container = AddPackFiles(false, "modfile", "c:\\mymod.pack", ["rootfolder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var root = viewModel.Files.First();

            // Act
            var command = new SetAsEditablePackCommand(_packFileService, MockScopedLogger.Create());
            command.Configure(root);

            command.Execute();

            // Assert
            Assert.That(_packFileService.GetEditablePack(), Is.EqualTo(container));
        }
    }
}
