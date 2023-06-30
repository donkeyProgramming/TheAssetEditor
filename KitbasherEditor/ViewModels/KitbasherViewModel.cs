using CommonControls.Common;
using CommonControls.Events;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.PackFileBrowser;
using CommonControls.Services;
using KitbasherEditor.Services;
using KitbasherEditor.ViewModels.MenuBarViews;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using View3D.Components;
using View3D.Components.Component;
using View3D.Scene;
using View3D.Services;

namespace KitbasherEditor.ViewModels
{
    public class KitbasherViewModel : NotifyPropertyChangedImpl, IEditorViewModel, 
        IDropTarget<TreeNode>,
        INotificationHandler<FileSavedEvent>,
        INotificationHandler<SceneInitializedEvent>, 
        INotificationHandler<CommandStackChangedEvent>
    {
        public IServiceScope ServiceScope { get; set; }
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
            MainScene sceneContainer, MenuBarViewModel menuBarViewModel, 
            AnimationControllerViewModel animationControllerViewModel, IComponentInserter componentInserter,
            KitbashSceneCreator kitbashSceneCreator, SceneExplorerViewModel sceneExplorerViewModel, ActiveFileResolver activeFileResolver, FocusSelectableObjectService focusSelectableObjectComponent, KitbashViewDropHandler dropHandler)
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
        }

        public bool Save() => true;

        public void Close()
        {
            Scene.Dispose();
            MenuBar = null;
            Scene = null;
            Scene = null;
            SceneExplorer = null;
            MenuBar = null;
            Animation = null;
            MainFile = null;
        }

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


        public Task Handle(FileSavedEvent notification, CancellationToken cancellationToken)
        {
            HasUnsavedChanges = false;
            return Task.CompletedTask;
        }

        public Task Handle(SceneInitializedEvent notification, CancellationToken cancellationToken)
        {
            _activeFileResolver.ActiveFileName = _packFileService.GetFullPath(MainFile);
            _kitbashSceneCreator.Create(MainFile);

            if (MainFile != null)
            {
                try
                {
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

            return Task.CompletedTask;
        }

        public Task Handle(CommandStackChangedEvent notification, CancellationToken cancellationToken)
        {
            if(notification.IsMutation)
                HasUnsavedChanges = true;
            return Task.CompletedTask;
        }
    }
}
