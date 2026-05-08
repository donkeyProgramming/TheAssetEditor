using System.Threading;
using Moq;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Core.Settings;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;

namespace Shared.UiTest.ContextMenuCommands
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    internal class SaveAsPackFileContainerCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_IsEnabled()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var command = new SaveAsPackFileContainerCommand(new Mock<IPackFileService>().Object, new ApplicationSettingsService(GameTypeEnum.Warhammer3), new Mock<IStandardDialogs>().Object);

            Assert.That(command.ShouldAdd(root), Is.True);
            Assert.That(command.IsEnabled(root), Is.True);

            // TODO: Execute uses SaveFileDialog and writes pack file to disk; skip for filesystem pass two.
            Assert.Ignore("TODO: Execute uses SaveFileDialog and writes pack file to disk; skip for filesystem pass two.");
        }
    }
}
