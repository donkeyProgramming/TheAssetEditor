using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu;

namespace Shared.Ui.BaseDialogs.PackFileTree
{
    public class PackFileTreeViewFactory
    {
        private readonly IPackFileService _packFileService;
        private readonly IEventHub _eventHub;
        private readonly ContextMenuFactory _contextMenuFactory;

        public PackFileTreeViewFactory(IPackFileService packFileService, IEventHub eventHub, ContextMenuFactory contextMenuFactory)
        {
            _packFileService = packFileService;
            _eventHub = eventHub;
            _contextMenuFactory = contextMenuFactory;
        }

        public PackFileBrowserViewModel Create(ContextMenuType contextMenu, bool showCaFiles, bool showFoldersOnly)
        {
            var contextMenuBuilder = _contextMenuFactory.GetContextMenu(contextMenu);
            var fileTree = new PackFileBrowserViewModel(contextMenuBuilder, _packFileService, _eventHub, showCaFiles, showFoldersOnly);
            return fileTree;
        }
    }
}
