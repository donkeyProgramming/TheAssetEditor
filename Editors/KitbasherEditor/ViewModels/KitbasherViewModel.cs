﻿using System.IO;
using System.Windows;
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
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.Core.ToolCreation;
using Shared.Ui.BaseDialogs.PackFileBrowser;
using Shared.Ui.Common;

namespace KitbasherEditor.ViewModels
{
    public class KitbasherViewModel : NotifyPropertyChangedImpl, IEditorViewModel, ISaveableEditor,
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

        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("3D Viewer");

        PackFile _mainFile;
        public PackFile MainFile { get => _mainFile; set { _mainFile = value; LoadScene(value); }  }

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

        private void LoadScene(PackFile fileToLoad)
        {
            try
            {
                _kitbashSceneCreator.CreateFromPackFile(fileToLoad);
                _focusSelectableObjectComponent.FocusScene();
                DisplayName.Value = fileToLoad.Name;
            }
            catch (Exception e)
            {
                _logger.Here().Error($"Error loading file {fileToLoad?.Name} - {e}");
                MessageBox.Show($"Unable to load file\n+{e.Message}");
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
                NotifyPropertyChanged();
            }
        }

        public bool AllowDrop(TreeNode node, TreeNode targeNode = null) => _dropHandler.AllowDrop(node, targeNode);
        public bool Drop(TreeNode node, TreeNode targeNode = null) => _dropHandler.Drop(node, targeNode);

        void OnFileSaved(ScopedFileSavedEvent notification)
        {
            HasUnsavedChanges = false;
            DisplayName.Value = Path.GetFileName(notification.NewPath);
        }

        void OnCommandStackChanged(CommandStackChangedEvent notification)
        {
            if (notification.IsMutation)
                HasUnsavedChanges = true;
        }
    }
}
