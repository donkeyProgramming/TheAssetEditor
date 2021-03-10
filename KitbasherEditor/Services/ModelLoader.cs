using Common;
using Filetypes.RigidModel;
using FileTypes.PackFiles.Models;
using FileTypes.PackFiles.Services;
using KitbasherEditor.ViewModels;
using Serilog;
using View3D.Components.Component;
using View3D.SceneNodes;
using View3D.Services;
using View3D.Utility;

namespace KitbasherEditor.Services
{
    public class ModelLoader
    {
        ILogger _logger = Logging.Create<ModelLoader>();

        public Rmv2ModelNode EditableMeshNode { get; private set; }
        public ISceneNode ReferenceMeshRoot { get; private set; }

        PackFileService _packFileService;
        ResourceLibary _resourceLibary;
        AnimationControllerViewModel _animationView;
        SceneManager _sceneManager;

        public ModelLoader(PackFileService packFileService, ResourceLibary resourceLibary, AnimationControllerViewModel animationView, SceneManager sceneManager)
        {
            _packFileService = packFileService;
            _resourceLibary = resourceLibary;
            _animationView = animationView;
            _sceneManager = sceneManager;

            _sceneManager.RootNode.AddObject(new SkeletonNode(resourceLibary.Content, animationView));
            EditableMeshNode = (Rmv2ModelNode)_sceneManager.RootNode.AddObject(new Rmv2ModelNode("Editable Model"));
            for (int lodIndex = 0; lodIndex < 4; lodIndex++)
            {
                var lodNode = new Rmv2LodNode("Lod " + lodIndex, lodIndex);
                lodNode.IsVisible = lodIndex == 0;
                EditableMeshNode.AddObject(lodNode);
            }

            ReferenceMeshRoot = sceneManager.RootNode.AddObject(new GroupNode("Reference meshs") { IsEditable = false });
        }

        public void LoadEditableModel(PackFile file)
        {
            var rmv = new RmvRigidModel(file.DataSource.ReadData(), file.Name);
            EditableMeshNode.SetModel(rmv, _resourceLibary.GraphicsDevice, _resourceLibary, _animationView.Player);

            _animationView.SetActiveSkeleton(rmv.Header.SkeletonName);
        }

        public void LoadReference(string path)
        {
            _logger.Here().Information($"Loading reference model from path - {path}");

            var refereneceMesh = _packFileService.FindFile(path);
            if (refereneceMesh == null)
            {
                _logger.Here().Error("Unable to find file");
                return;
            }

            LoadReference(refereneceMesh as PackFile);
        }

        public void LoadReference(PackFile file)
        {
            _logger.Here().Information($"Loading reference model - {_packFileService.GetFullPath(file)}");

            SceneLoader loader = new SceneLoader(_packFileService, _resourceLibary);
            var result = loader.Load(file, null, _animationView.Player);
            if (result == null)
            {
                _logger.Here().Error("Unable to load model");
                return;
            }

            result.ForeachNode((node) => 
            { 
                node.IsEditable = false;
                if (node is ISelectable selectable)
                    selectable.IsSelectable = false;
            });
            ReferenceMeshRoot.AddObject(result);
        }
    }
}
