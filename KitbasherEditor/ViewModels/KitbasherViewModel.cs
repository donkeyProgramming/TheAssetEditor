using CommonControls.Common;
using CommonControls.Events.Scoped;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.PackFileBrowser;
using CommonControls.Services.ToolCreation;
using KitbasherEditor.ViewModels.MenuBarViews;
using Monogame.WpfInterop.Common;
using System;
using System.IO;
using View3D.Components.Component;
using View3D.Services;
using View3D.Services.SceneSaving;

namespace KitbasherEditor.ViewModels
{
    public class KitbasherViewModel : NotifyPropertyChangedImpl, IEditorViewModel, IEditorScopeResolverHint, ISaveableEditor,
        IDropTarget<TreeNode>
    {
        private readonly KitbasherRootScene _kitbasherRootScene;
        private readonly KitbashViewDropHandler _dropHandler;
        private readonly SaveSettings _saveSettings;

        public Type GetScopeResolverType { get => typeof(IScopeHelper<KitbasherViewModel>); }

        public GameWorld Scene { get; set; }
        public SceneExplorerViewModel SceneExplorer { get; set; }
        public MenuBarViewModel MenuBar { get; set; }
        public AnimationControllerViewModel Animation { get; set; }

        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("3D Viewer");

        public PackFile MainFile { get; set; }
        private bool _hasUnsavedChanges;

        public KitbasherViewModel(
            KitbasherRootScene kitbasherRootScene,
            EventHub eventHub,
            GameWorld gameWorld,
            MenuBarViewModel menuBarViewModel,
            AnimationControllerViewModel animationControllerViewModel,
            SceneExplorerViewModel sceneExplorerViewModel,
            KitbashViewDropHandler dropHandler,
            SaveSettings saveSettings)
        {
            _kitbasherRootScene = kitbasherRootScene;
            _dropHandler = dropHandler;
            _saveSettings = saveSettings;
            Scene = gameWorld;
            Animation = animationControllerViewModel;
            SceneExplorer = sceneExplorerViewModel;
            MenuBar = menuBarViewModel;

            eventHub.Register<FileSavedEvent>(OnFileSaved);
            eventHub.Register<CommandStackChangedEvent>(OnCommandStackChanged);
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

        void OnFileSaved(FileSavedEvent notification)
        {
            HasUnsavedChanges = false;
            DisplayName.Value = Path.GetFileName(_saveSettings.OutputName);
        }

        void OnCommandStackChanged(CommandStackChangedEvent notification)
        {
            if (notification.IsMutation)
                HasUnsavedChanges = true;
        }
    }
}
