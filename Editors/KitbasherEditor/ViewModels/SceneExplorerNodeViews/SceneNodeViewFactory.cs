using Editors.Shared.Core.Services;
using KitbasherEditor.ViewModels.SceneExplorerNodeViews.Rmv2;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using View3D.Components.Component;
using View3D.Components.Rendering;
using View3D.SceneNodes;

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
        private readonly IUiCommandFactory _uiCommandFactory;

        public SceneNodeViewFactory(SceneManager sceneManager,
            KitbasherRootScene kitbasherRootScene, 
            SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, 
            PackFileService packFileService, 
            ApplicationSettingsService applicationSettingsService,
            RenderEngineComponent renderEngineComponent,
            IUiCommandFactory uiCommandFactory)
        {
            _sceneManager = sceneManager;
            _kitbasherRootScene = kitbasherRootScene;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _packFileService = packFileService;
            _applicationSettingsService = applicationSettingsService;
            _renderEngineComponent = renderEngineComponent;
            _uiCommandFactory = uiCommandFactory;
        }

        public ISceneNodeViewModel CreateEditorView(ISceneNode node)
        {
            switch (node)
            {
                case MainEditableNode mainNode:
                    return new MainEditableNodeViewModel(_kitbasherRootScene, mainNode, _skeletonAnimationLookUpHelper, _packFileService, _renderEngineComponent);
                case Rmv2MeshNode m:
                    return new MeshEditorViewModel(_kitbasherRootScene, m, _packFileService, _skeletonAnimationLookUpHelper, _sceneManager, _applicationSettingsService, _uiCommandFactory);
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
