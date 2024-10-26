using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Editors.KitbasherEditor.EventHandlers;
using Editors.KitbasherEditor.Services;
using Editors.KitbasherEditor.ViewModels.SceneExplorer;
using Editors.KitbasherEditor.ViewModels.SceneNodeEditor;
using GameWorld.Core.Components;
using GameWorld.Core.Services;
using KitbasherEditor.ViewModels.MenuBarViews;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;
using Shared.Core.Events.Scoped;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.Core.ToolCreation;
using Shared.Ui.BaseDialogs.PackFileBrowser;
using Shared.Ui.Common;

namespace KitbasherEditor.ViewModels
{
    public partial class KitbasherViewModel : ObservableObject, 
        IEditorInterface, 
        IFileEditor,
        ISaveableEditor,
        IDropTarget<TreeNode>
    {
        private readonly ILogger _logger = Logging.Create<KitbasherViewModel>();

        private readonly KitbashViewDropHandler _dropHandler;
        private readonly KitbashSceneCreator _kitbashSceneCreator;
        private readonly FocusSelectableObjectService _focusSelectableObjectComponent;

        public IWpfGame Scene { get; set; }
        public SceneExplorerViewModel SceneExplorer { get; set; }
        public SceneNodeEditorViewModel SceneNodeEditor { get; set; }
        public MenuBarViewModel MenuBar { get; set; }
        public AnimationControllerViewModel Animation { get; set; }

        [ObservableProperty] string _displayName = "Kitbash Tool";

        PackFile _inputFileReference;
        public PackFile CurrentFile { get => _inputFileReference; }

        private bool _hasUnsavedChanges;

        public KitbasherViewModel(
            EventHub eventHub,
            IWpfGame gameWorld,
            MenuBarViewModel menuBarViewModel,
            AnimationControllerViewModel animationControllerViewModel,
            SceneExplorerViewModel sceneExplorerViewModel,
            KitbashViewDropHandler dropHandler,
            KitbashSceneCreator kitbashSceneCreator,
            FocusSelectableObjectService focusSelectableObjectComponent,
            IComponentInserter componentInserter,
            SkeletonChangedHandler skeletonChangedHandler, 
            SceneNodeEditorViewModel sceneNodeEditorView)
        {
            _dropHandler = dropHandler;
            _kitbashSceneCreator = kitbashSceneCreator;
            _focusSelectableObjectComponent = focusSelectableObjectComponent;
            Scene = gameWorld;
            Animation = animationControllerViewModel;
            SceneExplorer = sceneExplorerViewModel;
            MenuBar = menuBarViewModel;
            SceneNodeEditor = sceneNodeEditorView;
            
            // Events
            eventHub.Register<ScopedFileSavedEvent>(this, OnFileSaved);
            eventHub.Register<CommandStackChangedEvent>(this, OnCommandStackChanged);
            skeletonChangedHandler.Subscribe(eventHub);
            
            // Ensure all game components are added to the editor
            componentInserter.Execute();
        }

        public void LoadFile(PackFile fileToLoad)
        {
            try
            {
                _inputFileReference = fileToLoad;
                _kitbashSceneCreator.CreateFromPackFile(fileToLoad);
                _focusSelectableObjectComponent.FocusScene();
                DisplayName = fileToLoad.Name;
            }
            catch (Exception e)
            {
                var errorMessage = $"Unable to load file '{fileToLoad?.Name}' \n {e.Message}";
                _logger.Here().Error(errorMessage);
                MessageBox.Show(errorMessage);
            }
        }

        public bool Save() => true;

        public void Close() { }

        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set
            {
                _hasUnsavedChanges = value;
                OnPropertyChanged(nameof(HasUnsavedChanges));
            }
        }

        public bool AllowDrop(TreeNode node, TreeNode targeNode = null) => _dropHandler.AllowDrop(node, targeNode);
        public bool Drop(TreeNode node, TreeNode targeNode = null) => _dropHandler.Drop(node, targeNode);

        void OnFileSaved(ScopedFileSavedEvent notification)
        {
            HasUnsavedChanges = false;
            DisplayName = Path.GetFileName(notification.NewPath);
        }

        void OnCommandStackChanged(CommandStackChangedEvent notification)
        {
            if (notification.IsMutation)
                HasUnsavedChanges = true;
        }
    }
}
