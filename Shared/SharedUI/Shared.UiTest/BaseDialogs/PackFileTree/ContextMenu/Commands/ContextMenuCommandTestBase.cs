using System.IO;
using System.Reflection.Metadata;
using Moq;
using Shared.Core.DependencyInjection;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.Settings;
using Shared.Core.ToolCreation;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu;
using Shared.Ui.BaseDialogs.PackFileTree.Utility;

namespace Shared.UiTest.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    internal abstract class ContextMenuCommandTestBase : PackFileTreeTestBase
    {
        protected static IPackFileContainer CreateContainer(bool isCa = false, string name = "pack", string systemFilePath = "C:\\temp\\pack.pack")
        {
            var container = new Mock<IPackFileContainer>();
            container.SetupGet(x => x.Name).Returns(name);
            container.SetupGet(x => x.SystemFilePath).Returns(systemFilePath);
            container.SetupProperty(x => x.IsCaPackFile, isCa);
            return container.Object;
        }

        protected static Mock<IPackFileService> CreatePackFileService(IPackFileContainer? container = null, PackFile? packFile = null)
        {
            var service = new Mock<IPackFileService>();
            service.Setup(x => x.GetAllPackfileContainers()).Returns(container == null ? [] : [container]);

            if (container != null && packFile != null)
                service.Setup(x => x.GetPackFileContainer(packFile)).Returns(container);

            return service;
        }

        protected static TreeNode CreateRoot(IPackFileContainer container) => new RootTreeNode(container.Name, container);

        protected static TreeNode CreateNodePath(TreeNode root, string path, NodeType leafType = NodeType.File)
        {
            var segments = path.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries);
            var current = root;

            for (var i = 0; i < segments.Length; i++)
            {
                var nodeType = i == segments.Length - 1 ? leafType : NodeType.Directory;
                var child = new TreeNode(segments[i], nodeType, current);
                current.AddChild(child);
                current = child;
            }

            return current;
        }
    }
}
