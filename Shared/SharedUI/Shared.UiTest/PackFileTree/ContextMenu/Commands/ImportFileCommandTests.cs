using System.Collections.Generic;
using System.Threading;
using Moq;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;

namespace Shared.UiTest.PackFileTree.ContextMenu.Commands
{
    [TestFixture]
    internal class ImportFileCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_ReturnsTrueForDirectoryNode()
        {
            var command = new ImportFileCommand(new Mock<IPackFileService>().Object, new Mock<IStandardDialogs>().Object);
            Assert.That(command.ShouldAdd(new TreeNode("dir", NodeType.Directory, CreateContainer(), null)), Is.True);
        }

        [Test]
        public void IsEnabled_ReturnsTrue()
        {
            var owner = CreateContainer(isCa: true);
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var command = new ImportFileCommand(new Mock<IPackFileService>().Object, new Mock<IStandardDialogs>().Object);

            Assert.That(command.IsEnabled(root), Is.True);
        }

        [Test]
        public void Execute_CaPackShowsErrorAndDoesNotImport()
        {
            var owner = CreateContainer(isCa: true);
            var root = new TreeNode("root", NodeType.Root, owner, null);

            var service = new Mock<IPackFileService>();
            var dialogs = new Mock<IStandardDialogs>();
            var command = new ImportFileCommand(service.Object, dialogs.Object);

            command.Execute(root);

            dialogs.Verify(x => x.ShowDialogBox("Unable to edit CA packfile", "Error"), Times.Once);
            service.Verify(x => x.AddFilesToPack(It.IsAny<IPackFileContainer>(), It.IsAny<List<NewPackFileEntry>>()), Times.Never);
        }
    }
}
