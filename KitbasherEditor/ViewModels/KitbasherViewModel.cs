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
using View3D.Rendering.Geometry;
using View3D.Scene;
using View3D.SceneNodes;
using View3D.Services;
using View3D.Utility;
using static KitbasherEditor.KitbasherEditor_DependencyInjectionContainer;

namespace KitbasherEditor.ViewModels
{
    public class KitbasherViewModel : NotifyPropertyChangedImpl, IEditorViewModel, IDropTarget<TreeNode>
    {
        ILogger _logger = Logging.Create<KitbasherViewModel>();
        PackFileService _packFileService;
        ApplicationSettingsService _applicationSettingsService;

        SceneContainer _scene;
        CommandExecutor _commandExecutor;
        private readonly ResourceLibary _resourceLibary;
        private readonly ComponentManagerResolver _componentManagerResolver;
        private readonly SceneManager _sceneManager;
        private readonly KitbashSceneCreator _kitbashSceneCreator;

        public SceneContainer Scene { get => _scene; set => SetAndNotify(ref _scene, value); }
        public SceneExplorerViewModel SceneExplorer { get; set; }
        public MenuBarViewModel MenuBar { get; set; }
        public AnimationControllerViewModel Animation { get; set; }

        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("3D Viewer");

        public PackFile MainFile { get; set; }


        private bool _hasUnsavedChanges;

        public KitbasherViewModel(PackFileService pf, ApplicationSettingsService applicationSettingsService,
            SceneContainer sceneContainer, ComponentInserter componentInserter, CommandExecutor commandExecutor, MenuBarViewModel menuBarViewModel, 
            ResourceLibary resourceLibary, ComponentManagerResolver componentManagerResolver, SceneManager sceneManager, AnimationControllerViewModel animationControllerViewModel,
            KitbashSceneCreator kitbashSceneCreator)
        {
            _packFileService = pf;
            _applicationSettingsService = applicationSettingsService;
            Scene = sceneContainer;
            _commandExecutor = commandExecutor;

            componentInserter.Execute();
            


            _commandExecutor.CommandStackChanged += CommandExecutorOnCommandStackChanged;

            Animation = animationControllerViewModel;
            _kitbashSceneCreator = kitbashSceneCreator;
            SceneExplorer = new SceneExplorerViewModel(Scene, _packFileService, Animation, _applicationSettingsService);

            MenuBar = menuBarViewModel;
            _resourceLibary = resourceLibary;
            _componentManagerResolver = componentManagerResolver;
            _sceneManager = sceneManager;
            Scene.SceneInitialized += OnSceneInitialized;
        }

        public void OpenEditor(PackFile file)
        {
        }

        private void CommandExecutorOnCommandStackChanged()
        {
            HasUnsavedChanges = _commandExecutor.HasSavableChanges();
        }

        private void OnSceneInitialized(WpfGame scene)
        {
            _kitbashSceneCreator.Create(MainFile);

            var mainNode = _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);

            MenuBar.General.ModelSaver = new SceneSaverService(_packFileService, this, mainNode, _applicationSettingsService);
            MenuBar.General.WsModelGeneratorService = new WsModelGeneratorService(_packFileService, this, mainNode);

            SceneExplorer.EditableMeshNode = mainNode;  // WTF is this used for?
            
            if (MainFile != null)
            {
                try
                {
                    _kitbashSceneCreator.LoadMainEditableModel(MainFile );

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
            _packFileService = null;
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
            _kitbashSceneCreator.LoadReference(node.Item );
            return true;
        }
    }
}
