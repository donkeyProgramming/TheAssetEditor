using Common;
using CommonControls.Common;
using CommonControls.Events.Scoped;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.PackFileBrowser;
using CommonControls.Services;
using KitbasherEditor.Services;
using KitbasherEditor.ViewModels.MenuBarViews;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using System.Windows;
using View3D.Components;
using View3D.Components.Component;
using View3D.Scene;
using View3D.Services;

namespace KitbasherEditor.ViewModels
{
    public class KitbasherViewModel : NotifyPropertyChangedImpl, IEditorViewModel,
        IDropTarget<TreeNode>
    {
        ILogger _logger = Logging.Create<KitbasherViewModel>();
        private readonly PackFileService _packFileService;
        private readonly KitbashSceneCreator _kitbashSceneCreator;
        private readonly FocusSelectableObjectService _focusSelectableObjectComponent;
        private readonly KitbashViewDropHandler _dropHandler;
        private readonly ActiveFileResolver _activeFileResolver;

        public MainScene Scene { get; set; }
        public SceneExplorerViewModel SceneExplorer { get; set; }
        public MenuBarViewModel MenuBar { get; set; }
        public AnimationControllerViewModel Animation { get; set; }

        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("3D Viewer");

        public PackFile MainFile { get; set; }
        private bool _hasUnsavedChanges;

        public KitbasherViewModel(PackFileService packFileService,
            EventHub eventHub,
            MainScene sceneContainer,
            MenuBarViewModel menuBarViewModel,
            AnimationControllerViewModel animationControllerViewModel, 
            IComponentInserter componentInserter,
            KitbashSceneCreator kitbashSceneCreator, 
            SceneExplorerViewModel sceneExplorerViewModel,
            ActiveFileResolver activeFileResolver,
            FocusSelectableObjectService focusSelectableObjectComponent,
            KitbashViewDropHandler dropHandler)
        {
            _packFileService = packFileService;
            _activeFileResolver = activeFileResolver;
            _kitbashSceneCreator = kitbashSceneCreator;
            _focusSelectableObjectComponent = focusSelectableObjectComponent;
            _dropHandler = dropHandler;
           
            Scene = sceneContainer;
            Animation = animationControllerViewModel;
            SceneExplorer = sceneExplorerViewModel;
            MenuBar = menuBarViewModel;

            componentInserter.Execute();

            eventHub.Register<FileSavedEvent>(OnFileSaved);
            eventHub.Register<CommandStackChangedEvent>(OnCommandStackChanged);
            eventHub.Register<SceneInitializedEvent>(OnSceneInitialized);
        }

        public bool Save() => true;

        public void Close(){ }

        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set
            {
                _hasUnsavedChanges = value;
                NotifyPropertyChanged();
            }
        }

        public bool AllowDrop(TreeNode node, TreeNode targeNode = null) => _dropHandler.AllowDrop(node, targeNode);
        public bool Drop(TreeNode node, TreeNode targeNode = null) => _dropHandler.Drop(node, targeNode);

        void OnFileSaved(FileSavedEvent notification)
        {
            HasUnsavedChanges = false;
        }

        void OnSceneInitialized(SceneInitializedEvent notification)
        {
            try
            {
                _activeFileResolver.ActiveFileName = _packFileService.GetFullPath(MainFile);

                _kitbashSceneCreator.Create();
                _kitbashSceneCreator.LoadMainEditableModel(MainFile);
                _focusSelectableObjectComponent.FocusScene();

                DisplayName.Value = MainFile.Name;
            }
            catch (Exception e)
            {
                _logger.Here().Error($"Error loading file {MainFile?.Name} - {e}");
                MessageBox.Show($"Unable to load file\n+{e.Message}");
            }
        }

        void OnCommandStackChanged(CommandStackChangedEvent notification)
        {
            if(notification.IsMutation)
                HasUnsavedChanges = true;
        }
    }
}
