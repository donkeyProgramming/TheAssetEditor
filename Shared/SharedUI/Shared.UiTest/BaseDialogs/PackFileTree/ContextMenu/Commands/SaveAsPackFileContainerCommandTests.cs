using System.Threading;
using Moq;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.Core.Settings;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;

namespace Shared.UiTest.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    [TestFixture]
    internal class SaveAsPackFileContainerCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_ReturnsTrueForRoot()
        {
            var owner = CreateContainer();
            var root = CreateRoot(owner);
            var command = new SaveAsPackFileContainerCommand(CreatePackFileService(owner).Object, new ApplicationSettingsService(GameTypeEnum.Warhammer3), new Mock<IStandardDialogs>().Object);

            Assert.That(command.ShouldAdd(root), Is.True);
        }

        [Test]
        public void IsEnabled_ReturnsTrue()
        {
            var owner = CreateContainer();
            var root = CreateRoot(owner);
            var command = new SaveAsPackFileContainerCommand(CreatePackFileService(owner).Object, new ApplicationSettingsService(GameTypeEnum.Warhammer3), new Mock<IStandardDialogs>().Object);

            Assert.That(command.IsEnabled(root), Is.True);
        }

        [Test]
        public void Execute_SaveDialogCancelled_DoesNotSave()
        {
            var owner = CreateContainer();
            var root = CreateRoot(owner);
            var service = CreatePackFileService(owner);
            var dialogs = new Mock<IStandardDialogs>();
            dialogs.Setup(x => x.ShowSystemSaveFileDialog(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new SystemSaveFileDialogResult(false, null));

            var command = new SaveAsPackFileContainerCommand(service.Object, new ApplicationSettingsService(GameTypeEnum.Warhammer3), dialogs.Object);
            command.Execute(root);

            service.Verify(x => x.SavePackContainer(It.IsAny<IPackFileContainer>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<GameInformation>()), Times.Never);
        }
    }
}
