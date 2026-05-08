using System.Collections.Generic;
using System.Threading;
using Moq;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;

namespace Shared.UiTest.ContextMenuCommands
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    internal class ImportDirectoryCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_IsEnabled_Execute_CaPackShowsError()
        {
            var owner = CreateContainer(isCa: true);
            var root = new TreeNode("root", NodeType.Root, owner, null);

            var service = new Mock<IPackFileService>();
            var dialogs = new Mock<IStandardDialogs>();
            var command = new ImportDirectoryCommand(service.Object, dialogs.Object);

            Assert.That(command.ShouldAdd(new TreeNode("dir", NodeType.Directory, CreateContainer(), null)), Is.True);
            Assert.That(command.IsEnabled(root), Is.True);

            command.Execute(root);

            dialogs.Verify(x => x.ShowDialogBox("Unable to edit CA packfile", "Error"), Times.Once);
            service.Verify(x => x.AddFilesToPack(It.IsAny<IPackFileContainer>(), It.IsAny<List<NewPackFileEntry>>()), Times.Never);
        }
    }
}
