using CommonControls.Common;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.PackFileBrowser;
using CommonControls.Services;
using KitbasherEditor.Services;
using KitbasherEditor.ViewModels.MenuBarViews;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using View3D.Components.Component;
using View3D.Components.Component.Selection;
using View3D.Scene;
using View3D.SceneNodes;
using View3D.Services;

using static KitbasherEditor.KitbasherEditor_DependencyInjectionContainer;


namespace KitbasherEditor.ViewModels
{
    public class KitbasherViewModel : NotifyPropertyChangedImpl, IEditorViewModel, IDropTarget<TreeNode>
    {
        ILogger _logger = Logging.Create<KitbasherViewModel>();
        private readonly PackFileService _packFileService;
        private SceneContainer _scene;
        private readonly CommandExecutor _commandExecutor;
        private readonly SceneManager _sceneManager;
        private readonly KitbashSceneCreator _kitbashSceneCreator;
        private readonly ActiveFileResolver _activeFileResolver;

        public SceneContainer Scene { get => _scene; set => SetAndNotify(ref _scene, value); }
        public SceneExplorerViewModel SceneExplorer { get; set; }
        public MenuBarViewModel MenuBar { get; set; }
        public AnimationControllerViewModel Animation { get; set; }

        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("3D Viewer");

        public PackFile MainFile { get; set; }


        private bool _hasUnsavedChanges;

        public KitbasherViewModel(PackFileService packFileService, 
            SceneContainer sceneContainer, ComponentInserter componentInserter, CommandExecutor commandExecutor, MenuBarViewModel menuBarViewModel, 
            SceneManager sceneManager, AnimationControllerViewModel animationControllerViewModel,
            KitbashSceneCreator kitbashSceneCreator, SceneExplorerViewModel sceneExplorerViewModel, ActiveFileResolver activeFileResolver)
        {
            _packFileService = packFileService;

            Scene = sceneContainer;
            _kitbashSceneCreator = kitbashSceneCreator;
            _commandExecutor = commandExecutor;
            _sceneManager = sceneManager;

            componentInserter.Execute();
            
            _commandExecutor.CommandStackChanged += CommandExecutorOnCommandStackChanged;  // ToDo - MediatR

            Animation = animationControllerViewModel;
            SceneExplorer = sceneExplorerViewModel;
            _activeFileResolver = activeFileResolver;
            MenuBar = menuBarViewModel;

            Scene.SceneInitialized += OnSceneInitialized; // ToDo - MediatR
        }

        public void OpenEditor(PackFile file)
        {
            // TODO
        }

        private void CommandExecutorOnCommandStackChanged()
        {
            HasUnsavedChanges = _commandExecutor.HasSavableChanges();
        }

        private void OnSceneInitialized(WpfGame scene)
        {
            _activeFileResolver.ActiveFileName = _packFileService.GetFullPath(MainFile);
            _kitbashSceneCreator.Create(MainFile);

            if (MainFile != null)
            {
                try
                {
                    var mainNode = _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);
                    _kitbashSceneCreator.LoadMainEditableModel(MainFile);

                    var nodes = mainNode.GetMeshNodes(0)
                        .Select(x => x as ISelectable)
                        .Where(x => x != null)
                        .ToList();
                    Scene.GetComponent<FocusSelectableObjectComponent>().FocusObjects(nodes);
                   DisplayName.Value = MainFile.Name;
                }
                catch (Exception e)
                {
                    _logger.Here().Error($"Error loading file {MainFile?.Name} - {e}");
                    MessageBox.Show($"Unable to load file\n+{e.Message}");
                }
            }
        }

        public bool Save()
        {
            HasUnsavedChanges = false;
            _commandExecutor.SaveStackSnapshot();
            return true;
        }

        public void Close()
        {
            Scene.Dispose();
            Scene.SceneInitialized -= OnSceneInitialized;
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

        public bool AllowDrop(TreeNode node, TreeNode targeNode = null)
        {
            if (node != null && node.NodeType == NodeType.File)
            {
                var extention = Path.GetExtension(node.Name).ToLower();
                if (extention == ".rigid_model_v2" || extention == ".wsmodel" || extention == ".variantmeshdefinition")
                    return true;
            }
            return false;
        }

        public bool Drop(TreeNode node, TreeNode targeNode = null)
        {
            _kitbashSceneCreator.LoadReference(node.Item);
            return true;
        }
    }
}
