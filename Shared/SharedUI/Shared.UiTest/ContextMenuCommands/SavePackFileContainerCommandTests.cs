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
    internal class SavePackFileContainerCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_IsEnabled_Execute()
        {
            var owner = CreateContainer(systemFilePath: "C:\\temp\\pack.pack");
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var service = new Mock<IPackFileService>();
            var dialogs = new Mock<IStandardDialogs>();
            var appSettings = new ApplicationSettingsService(GameTypeEnum.Warhammer3);

            var command = new SavePackFileContainerCommand(service.Object, dialogs.Object, appSettings);

            Assert.That(command.ShouldAdd(root), Is.True);
            Assert.That(command.IsEnabled(root), Is.True);

            command.Execute(root);

            service.Verify(x => x.SavePackContainer(owner, owner.SystemFilePath, false, It.IsAny<GameInformation>()), Times.Once);
        }
    }
}
