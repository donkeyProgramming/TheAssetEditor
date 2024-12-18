using CommunityToolkit.Mvvm.ComponentModel;
using GameWorld.Core.Components;
using GameWorld.Core.Components.Selection;
using KitbasherEditor.ViewModels.SceneExplorerNodeViews;
using Shared.Core.Events;

namespace Editors.KitbasherEditor.ViewModels.SceneExplorer
{
    // This object only exists to handle the context menu and passing 
    // the needed attributes the MultiSelectTreeView
    public partial class SceneExplorerViewModel : ObservableObject
    {
        [ObservableProperty] SceneManager _sceneManager;
        [ObservableProperty] IEventHub _eventHub;
        [ObservableProperty] SelectionManager _selectionManager;
        [ObservableProperty] SceneExplorerContextMenuHandler _contextMenu;

        public SceneExplorerViewModel(
            SelectionManager selectionManager,
            SceneManager sceneManager,
            IEventHub eventHub,
            SceneExplorerContextMenuHandler contextMenuHandler)
        {
            _sceneManager = sceneManager;
            _eventHub = eventHub;
            _selectionManager = selectionManager;
            _contextMenu = contextMenuHandler;
        }
    }
}
