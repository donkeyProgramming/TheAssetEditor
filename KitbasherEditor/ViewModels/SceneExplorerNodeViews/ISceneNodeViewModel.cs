using CommonControls.Services;
using System;
using View3D.SceneNodes;
using View3D.Utility;

namespace KitbasherEditor.ViewModels.SceneExplorerNodeViews
{
    public interface ISceneNodeViewModel : IDisposable
    { 
    }

    public static class SceneNodeViewFactory
    {
        public static ISceneNodeViewModel Create(ISceneNode node, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, PackFileService pf, AnimationControllerViewModel animationControllerViewModel)
        {
            switch (node)
            {
                case MainEditableNode mainNode:
                    return new MainEditableNodeViewModel(mainNode, skeletonAnimationLookUpHelper, animationControllerViewModel, pf);

                case Rmv2ModelNode m:
                    return new ModelSceneNodeViewModel(m);

                case Rmv2LodNode l:
                    return new LodSceneNodeViewModel(l);

                case Rmv2MeshNode m:
                    return new MeshSceneNodeViewModel(m, pf, skeletonAnimationLookUpHelper);
                case SkeletonNode s:
                    return new SkeletonSceneNodeViewModel(s, pf, skeletonAnimationLookUpHelper);
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
