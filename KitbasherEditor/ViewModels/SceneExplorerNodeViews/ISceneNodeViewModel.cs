using CommonControls.Services;
using View3D.SceneNodes;
using View3D.Utility;

namespace KitbasherEditor.ViewModels.SceneExplorerNodeViews
{
    public interface ISceneNodeViewModel
    { }

    public static class SceneNodeViewFactory
    {
        public static ISceneNodeViewModel Create(ISceneNode node, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, PackFileService pf, AnimationControllerViewModel animationControllerViewModel)
        {
            switch (node)
            {
                case Rmv2ModelNode m:
                    return new ModelSceneNodeViewModel(m, skeletonAnimationLookUpHelper, animationControllerViewModel);

                case Rmv2LodNode l:
                    return new LodSceneNodeViewModel(l);

                case Rmv2MeshNode m:
                    return new MeshSceneNodeViewModel(m, pf, skeletonAnimationLookUpHelper);
                case SkeletonNode s:
                    return new SkeletonSceneNodeViewModel(s, pf, skeletonAnimationLookUpHelper);

                default:
                    return null;
            }
        }
    }
}
