using System;
using System.Collections.Generic;
using System.Text;
using View3D.SceneNodes;
using View3D.Utility;

namespace KitbasherEditor.ViewModels.SceneExplorerNodeViews
{
    public interface ISceneNodeViewModel
    { }

    public static class SceneNodeViewFactory
    {
        public static ISceneNodeViewModel Create(SceneNode node, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper)
        {
            switch (node)
            {
                case Rmv2ModelNode m:
                    return new ModelSceneNodeViewModel(m, skeletonAnimationLookUpHelper);

                case Rmv2LodNode l:
                    return new LodSceneNodeViewModel(l);

                case Rmv2MeshNode m:
                    return new MeshSceneNodeViewModel(m);

                default:
                    return null;
            }
        }
    }
}
