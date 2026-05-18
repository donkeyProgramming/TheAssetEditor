using System.IO;
using System.Reflection.Metadata;
using Moq;
using Shared.Core.DependencyInjection;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.ToolCreation;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu;

namespace Shared.UiTest.BaseDialogs.PackFileTree.ContextMenu.Commands
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

        protected static (IPackFileContainer Container, TreeNode Root, TreeNode FileNode, PackFile PackFile) CreateResolvedFileSelection(string filePath = "file.txt", string content = "a", bool isCa = false, string name = "pack", string systemFilePath = "C:\\temp\\pack.pack")
        {
            var container = new Mock<IPackFileContainer>();
            container.SetupGet(x => x.Name).Returns(name);
            container.SetupGet(x => x.SystemFilePath).Returns(systemFilePath);
            container.SetupProperty(x => x.IsCaPackFile, isCa);

            var root = CreateRoot(container.Object);
            var fileNode = CreateNodePath(root, filePath);
            var packFile = PackFile.CreateFromASCII(Path.GetFileName(filePath), content);
            var fullPath = fileNode.GetFullPath();

            container.Setup(x => x.FindFile(fullPath)).Returns(packFile);
            container.Setup(x => x.ContainsFile(fullPath)).Returns(true);
            container.Setup(x => x.GetFullPath(packFile)).Returns(fullPath);

            return (container.Object, root, fileNode, packFile);
        }



        protected Mock<IScopeRepository> _scopeRepo;
        protected LocalScopeEventHub _eventHub;
        protected SingletonScopeEventHub _globalEventHub;
        protected IPackFileService _packFileService;

        [SetUp]
        public void Setup()
        {
            _scopeRepo = new Mock<IScopeRepository>();

        //    var mockedEditorHandle = new Mock<IEditorInterface>();



            _globalEventHub = new SingletonScopeEventHub(_scopeRepo.Object);
            _eventHub = new LocalScopeEventHub(_scopeRepo.Object);

            _packFileService = new PackFileService(_globalEventHub);

            _scopeRepo.Setup(x => x.GetRequiredServiceRootScope<IEventHub>()).Returns(_eventHub);
            _scopeRepo.Setup(x => x.GetEditorHandles()).Returns([]);
        }

        protected IPackFileContainer AddPackFiles(bool isCa, string containerName, string fileSystemPath, params string[] files)
        {
            var packfileContainer = new PackFileContainer(containerName) { IsCaPackFile = isCa, SystemFilePath = fileSystemPath };
            foreach (var file in files)
            {
                var packFile = PackFile.CreateFromASCII(Path.GetFileName(file), "content");
                packfileContainer.AddOrUpdateFile(file, packFile);
            }
            _packFileService.AddContainer(packfileContainer);
            return packfileContainer;
        }

        protected PackFileBrowserViewModel GetViewModel()
        {

            var packFileBrowserViewModel = new PackFileBrowserViewModel(null, null, ContextMenuType.None, _packFileService, _eventHub, null, null, true, false);
            return packFileBrowserViewModel;
        }

        [TearDown]
        public void TearDown()
        {
            _eventHub.Dispose();
            _globalEventHub.Dispose();
        }
    }
}
