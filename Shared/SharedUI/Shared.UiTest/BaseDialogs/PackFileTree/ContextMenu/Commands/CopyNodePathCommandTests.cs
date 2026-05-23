using Test.TestingUtility.TestUtility;
using System.Threading;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;
using Shared.Ui.BaseDialogs.PackFileTree.Utility;

namespace Shared.UiTest.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    [TestFixture]
    internal class CopyNodePathCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_ReturnsTrueForFile()
        {
            var container = AddPackFiles(false, "modfile", "c:\\mymod.pack", ["folder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var file = TreeNodeHelper.FindNode(viewModel, container, "folder\\file.txt");

            var command = new CopyNodePathCommand(MockScopedLogger.Create());

            Assert.That(command.ShouldAdd(file), Is.True);
        }

        [Test]
        public void IsEnabled_ReturnsTrue()
        {
            var container = AddPackFiles(false, "modfile", "c:\\mymod.pack", ["folder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var file = TreeNodeHelper.FindNode(viewModel, container, "folder\\file.txt");

            var command = new CopyNodePathCommand(MockScopedLogger.Create());

            Assert.That(command.IsEnabled(file), Is.True);
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Execute_CopiesNodePathToClipboard()
        {
            var container = AddPackFiles(false, "modfile", "c:\\mymod.pack", ["folder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var file = TreeNodeHelper.FindNode(viewModel, container, "folder\\file.txt");

            var command = new CopyNodePathCommand(MockScopedLogger.Create());
            command.Configure(file);

            command.Execute();

            Assert.That(System.Windows.Clipboard.GetText(), Is.EqualTo("folder\\file.txt"));
        }
    }
}
