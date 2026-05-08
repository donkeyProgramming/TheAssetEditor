using Moq;
using Shared.Core.PackFiles.Models;
using Shared.Ui.BaseDialogs.PackFileTree;

namespace Shared.UiTest.ContextMenuCommands
{
    internal abstract class ContextMenuCommandTestBase
    {
        protected static IPackFileContainer CreateContainer(bool isCa = false, string name = "pack", string systemFilePath = "C:\\temp\\pack.pack")
        {
            var container = new Mock<IPackFileContainer>();
            container.SetupGet(x => x.Name).Returns(name);
            container.SetupGet(x => x.SystemFilePath).Returns(systemFilePath);
            container.SetupProperty(x => x.IsCaPackFile, isCa);
            return container.Object;
        }
    }
}
