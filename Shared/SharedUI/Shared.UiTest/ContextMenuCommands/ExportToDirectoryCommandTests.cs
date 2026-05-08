using System.Threading;
using Moq;
using Shared.Core.Services;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;

namespace Shared.UiTest.ContextMenuCommands
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    internal class ExportToDirectoryCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_IsEnabled()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var command = new ExportToDirectoryCommand(new Mock<IStandardDialogs>().Object);

            Assert.That(command.ShouldAdd(root), Is.True);
            Assert.That(command.IsEnabled(root), Is.True);

            // TODO: Execute uses FolderBrowserDialog and writes exported files to disk; skip for filesystem pass two.
            Assert.Ignore("TODO: Execute uses FolderBrowserDialog and writes exported files to disk; skip for filesystem pass two.");
        }

        [Test]
        public void ComputeRelativePath_RepeatedSegmentDoesNotCorruptPath()
        {
            // Root dir is "data", file fullPath is "data\data\unit.mesh" — old Replace would strip both occurrences.
            var result = ExportToDirectoryCommand.ComputeRelativePath("data\\data\\unit.mesh", "data");
            Assert.That(result, Is.EqualTo("\\data\\unit.mesh"));
        }

        [Test]
        public void ComputeRelativePath_NullRootReturnsFullPath()
        {
            var result = ExportToDirectoryCommand.ComputeRelativePath("sub\\file.mesh", null);
            Assert.That(result, Is.EqualTo("\\sub\\file.mesh"));
        }
    }
}
