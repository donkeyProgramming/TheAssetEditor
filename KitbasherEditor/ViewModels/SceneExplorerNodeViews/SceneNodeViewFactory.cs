using CommonControls.Services;
using KitbasherEditor.ViewModels.SceneExplorerNodeViews.Rmv2;
using View3D.Components.Component;
using View3D.Components.Rendering;
using View3D.SceneNodes;
using View3D.Utility;

namespace KitbasherEditor.ViewModels.SceneExplorerNodeViews
{
    public class SceneNodeViewFactory
    {
        private readonly SceneManager _sceneManager;
        private readonly KitbasherRootScene _kitbasherRootScene;
        private readonly SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        private readonly PackFileService _packFileService;
        private readonly ApplicationSettingsService _applicationSettingsService;
        private readonly RenderEngineComponent _renderEngineComponent;

        public SceneNodeViewFactory(SceneManager sceneManager,
            KitbasherRootScene kitbasherRootScene, 
            SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, 
            PackFileService packFileService, 
            ApplicationSettingsService applicationSettingsService,
            RenderEngineComponent renderEngineComponent)
        {
            _sceneManager = sceneManager;
            _kitbasherRootScene = kitbasherRootScene;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _packFileService = packFileService;
            _applicationSettingsService = applicationSettingsService;
            _renderEngineComponent = renderEngineComponent;
        }

        public ISceneNodeViewModel CreateEditorView(ISceneNode node)
        {
            switch (node)
            {
                case MainEditableNode mainNode:
                    return new MainEditableNodeViewModel(_kitbasherRootScene, mainNode, _skeletonAnimationLookUpHelper, _packFileService, _renderEngineComponent);
                case Rmv2MeshNode m:
                    return new MeshEditorViewModel(_kitbasherRootScene, m, _packFileService, _skeletonAnimationLookUpHelper, _sceneManager, _applicationSettingsService);
                case SkeletonNode s:
                    return new SkeletonSceneNodeViewModel(s, _packFileService, _skeletonAnimationLookUpHelper);
                case GroupNode n:
                    {
                        if (n.IsEditable && n.Parent != null)
                            return new GroupNodeViewModel(n);

                        return null;
                    }


                default:
                    return null;
            }
        }
    }
}
