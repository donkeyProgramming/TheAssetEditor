using CommonControls.Services;
using KitbasherEditor.ViewModels.SceneExplorerNodeViews.Rmv2;
using View3D.SceneNodes;
using View3D.Utility;

namespace KitbasherEditor.ViewModels.SceneExplorerNodeViews
{
    public class SceneNodeViewFactory
    {
        private readonly KitbasherRootScene _kitbasherRootScene;
        private readonly SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        private readonly PackFileService _packFileService;
        private readonly ComponentManagerResolver _componentManagerResolver;
        private readonly ApplicationSettingsService _applicationSettingsService;

        public SceneNodeViewFactory(KitbasherRootScene kitbasherRootScene, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, PackFileService packFileService, ComponentManagerResolver componentManagerResolver, ApplicationSettingsService applicationSettingsService)
        {
            _kitbasherRootScene = kitbasherRootScene;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _packFileService = packFileService;
            _componentManagerResolver = componentManagerResolver;
            _applicationSettingsService = applicationSettingsService;
        }

        public ISceneNodeViewModel CreateEditorView(ISceneNode node)
        {
            switch (node)
            {
                case MainEditableNode mainNode:
                    return new MainEditableNodeViewModel(_kitbasherRootScene, mainNode, _skeletonAnimationLookUpHelper, _packFileService, _componentManagerResolver.ComponentManager);
                case Rmv2MeshNode m:
                    return new MeshEditorViewModel(_kitbasherRootScene, m, _packFileService, _skeletonAnimationLookUpHelper, _componentManagerResolver.ComponentManager, _applicationSettingsService);
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
