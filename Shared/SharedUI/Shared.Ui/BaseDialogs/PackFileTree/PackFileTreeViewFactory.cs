using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.Settings;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu;
using Shared.Ui.Common.MenuSystem;

namespace Shared.Ui.BaseDialogs.PackFileTree
{
    public class PackFileTreeViewFactory
    {
        private readonly ApplicationSettingsService _applicationSettingsService;
        private readonly IPackFileService _packFileService;
        private readonly IEventHub _eventHub;
        private readonly ContextMenuFactory _contextMenuFactory;
        private readonly IWindowsKeyboard _windowKeyboard;

        public PackFileTreeViewFactory(ApplicationSettingsService applicationSettingsService, IPackFileService packFileService, IEventHub eventHub, ContextMenuFactory contextMenuFactory, IWindowsKeyboard windowKeyboard)
        {
            _applicationSettingsService = applicationSettingsService;
            _packFileService = packFileService;
            _eventHub = eventHub;
            _contextMenuFactory = contextMenuFactory;
            _windowKeyboard = windowKeyboard;
        }

        public PackFileBrowserViewModel Create(ContextMenuType contextMenu, bool showCaFiles, bool showFoldersOnly)
        {
            var contextMenuBuilder = _contextMenuFactory.GetContextMenu(contextMenu);
            var fileTree = new PackFileBrowserViewModel(_applicationSettingsService, contextMenuBuilder, _packFileService, _eventHub, _windowKeyboard, showCaFiles, showFoldersOnly);
            return fileTree;
        }
    }
}
