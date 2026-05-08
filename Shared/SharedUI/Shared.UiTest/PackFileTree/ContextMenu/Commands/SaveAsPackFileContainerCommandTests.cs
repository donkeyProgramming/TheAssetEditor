using System.Threading;
using Moq;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Core.Settings;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;

namespace Shared.UiTest.PackFileTree.ContextMenu.Commands
{
    [TestFixture]
    internal class SaveAsPackFileContainerCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_ReturnsTrueForRoot()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var command = new SaveAsPackFileContainerCommand(new Mock<IPackFileService>().Object, new ApplicationSettingsService(GameTypeEnum.Warhammer3), new Mock<IStandardDialogs>().Object);

            Assert.That(command.ShouldAdd(root), Is.True);
        }

        [Test]
        public void IsEnabled_ReturnsTrue()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var command = new SaveAsPackFileContainerCommand(new Mock<IPackFileService>().Object, new ApplicationSettingsService(GameTypeEnum.Warhammer3), new Mock<IStandardDialogs>().Object);

            Assert.That(command.IsEnabled(root), Is.True);
        }

        [Test]
        public void Execute_IgnoredUntilFilesystemPassTwo()
        {
            // TODO: Execute uses SaveFileDialog and writes pack file to disk; skip for filesystem pass two.
            Assert.Ignore("TODO: Execute uses SaveFileDialog and writes pack file to disk; skip for filesystem pass two.");
        }
    }
}
