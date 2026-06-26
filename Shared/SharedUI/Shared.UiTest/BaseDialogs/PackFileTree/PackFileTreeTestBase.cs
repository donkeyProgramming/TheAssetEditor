using System.IO;
using Moq;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.Settings;
using Shared.Core.ToolCreation;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu;
using Test.TestingUtility.TestUtility;

namespace Shared.UiTest.BaseDialogs.PackFileTree
{
    internal abstract class PackFileTreeTestBase
    {
        protected Mock<IScopeRepository> _scopeRepo;
        protected LocalScopeEventHub _eventHub;
        protected GlobalEventHub _globalEventHub;
        protected PackFileService _packFileService;

        [SetUp]
        public void Setup()
        {
            _scopeRepo = new Mock<IScopeRepository>();
            _globalEventHub = new GlobalEventHub(_scopeRepo.Object);
            _eventHub = new LocalScopeEventHub(_scopeRepo.Object, MockScopedLogger.Create());

            _packFileService = new PackFileService(_globalEventHub)
            {
                EnforceGameFilesMustBeLoaded = false
            };

            _scopeRepo.Setup(x => x.GetRequiredServiceRootScope<IEventHub>()).Returns(_eventHub);
            _scopeRepo.Setup(x => x.GetEditorHandles()).Returns([]);
        }

        [TearDown]
        public void TearDown()
        {
            _eventHub.Dispose();
            _globalEventHub.Dispose();
        }

        protected IPackFileContainer AddPackFiles(bool isCa, string containerName, string fileSystemPath, params string[] files)
        {
            var packfileContainer = isCa ? PackFileContainer.CreateCaPackFile(containerName, fileSystemPath) : PackFileContainer.CreatePackFile(containerName, fileSystemPath);
            foreach (var file in files)
            {
                var packFile = PackFile.CreateFromASCII(Path.GetFileName(file), "content");
                packfileContainer.AddOrUpdateFile(file, packFile);
            }
            _packFileService.AddContainer(packfileContainer);
            return packfileContainer;
        }

        protected PackFileBrowserViewModel PackFileBrowser()
        {
            var settings = new ApplicationSettingsService(GameTypeEnum.Warhammer3);
            return new PackFileBrowserViewModel(settings, null, ContextMenuType.None, _packFileService, _eventHub, null, true, false);
        }
    }
}
