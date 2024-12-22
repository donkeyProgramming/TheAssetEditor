using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.Settings;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu;

namespace Shared.Ui.BaseDialogs.PackFileTree
{
    public class PackFileTreeViewFactory
    {
        private readonly ApplicationSettingsService _applicationSettingsService;
        private readonly IPackFileService _packFileService;
        private readonly IEventHub _eventHub;
        private readonly ContextMenuFactory _contextMenuFactory;

        public PackFileTreeViewFactory(ApplicationSettingsService applicationSettingsService, IPackFileService packFileService, IEventHub eventHub, ContextMenuFactory contextMenuFactory)
        {
            _applicationSettingsService = applicationSettingsService;
            _packFileService = packFileService;
            _eventHub = eventHub;
            _contextMenuFactory = contextMenuFactory;
        }

        public PackFileBrowserViewModel Create(ContextMenuType contextMenu, bool showCaFiles, bool showFoldersOnly)
        {
            var contextMenuBuilder = _contextMenuFactory.GetContextMenu(contextMenu);
            var fileTree = new PackFileBrowserViewModel(_applicationSettingsService, contextMenuBuilder, _packFileService, _eventHub, showCaFiles, showFoldersOnly);
            return fileTree;
        }
    }
}
