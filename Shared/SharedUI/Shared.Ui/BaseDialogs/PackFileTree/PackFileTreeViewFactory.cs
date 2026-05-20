using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.Services;
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
        private readonly PackFileContextMenuComposer _contextMenuComposer;
        private readonly IWindowsKeyboard _windowKeyboard;
        private readonly IStandardDialogs _standardDialogs;

        public PackFileTreeViewFactory(ApplicationSettingsService applicationSettingsService, IPackFileService packFileService, IEventHub eventHub, PackFileContextMenuComposer contextMenuComposer, IWindowsKeyboard windowKeyboard, IStandardDialogs standardDialogs)
        {
            _applicationSettingsService = applicationSettingsService;
            _packFileService = packFileService;
            _eventHub = eventHub;
            _contextMenuComposer = contextMenuComposer;
            _windowKeyboard = windowKeyboard;
            _standardDialogs = standardDialogs;
        }

        public PackFileBrowserViewModel Create(ContextMenuType contextMenu, bool showCaFiles, bool showFoldersOnly)
        {
            var fileTree = new PackFileBrowserViewModel(_applicationSettingsService, _contextMenuComposer, contextMenu, _packFileService, _eventHub, _windowKeyboard, showCaFiles, showFoldersOnly, _standardDialogs);
            return fileTree;
        }
    }
}
