using System.Threading;
using Moq;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Core.Settings;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;

namespace Shared.UiTest.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    [TestFixture]
    internal class SavePackFileContainerCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_ReturnsTrueForRoot()
        {
            var owner = CreateContainer(systemFilePath: "C:\\temp\\pack.pack");
            var root = CreateRoot(owner);
            var command = new SavePackFileContainerCommand(CreatePackFileService(owner).Object, new Mock<IStandardDialogs>().Object, new ApplicationSettingsService(GameTypeEnum.Warhammer3));

            Assert.That(command.ShouldAdd(root), Is.True);
        }

        [Test]
        public void IsEnabled_ReturnsTrue()
        {
            var owner = CreateContainer(systemFilePath: "C:\\temp\\pack.pack");
            var root = CreateRoot(owner);
            var command = new SavePackFileContainerCommand(CreatePackFileService(owner).Object, new Mock<IStandardDialogs>().Object, new ApplicationSettingsService(GameTypeEnum.Warhammer3));

            Assert.That(command.IsEnabled(root), Is.True);
        }

        [Test]
        public void Execute_SavesPackContainer()
        {
            var owner = CreateContainer(systemFilePath: "C:\\temp\\pack.pack");
            var root = CreateRoot(owner);
            var service = CreatePackFileService(owner);
            var dialogs = new Mock<IStandardDialogs>();
            var appSettings = new ApplicationSettingsService(GameTypeEnum.Warhammer3);

            var command = new SavePackFileContainerCommand(service.Object, dialogs.Object, appSettings);

            command.Execute(root);

            service.Verify(x => x.SavePackContainer(owner, owner.SystemFilePath, false, It.IsAny<GameInformation>()), Times.Once);
        }
    }
}
