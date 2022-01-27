using CommonControls.Services;
using KitbasherEditor.ViewModels.SceneExplorerNodeViews.Rmv2;
using MonoGame.Framework.WpfInterop;
using System;
using View3D.Components.Component;
using View3D.SceneNodes;
using View3D.Utility;

namespace KitbasherEditor.ViewModels.SceneExplorerNodeViews
{
    public interface ISceneNodeViewModel : IDisposable
    { 
    }

    public static class SceneNodeViewFactory
    {
        public static ISceneNodeViewModel Create(ISceneNode node, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, PackFileService pf, AnimationControllerViewModel animationControllerViewModel, IComponentManager componentManager)
        {
            switch (node)
            {
                case MainEditableNode mainNode:
                    return new MainEditableNodeViewModel(mainNode, skeletonAnimationLookUpHelper, animationControllerViewModel, pf);
                case Rmv2MeshNode m:
                    return new MeshEditorViewModel(m, pf, skeletonAnimationLookUpHelper, componentManager);
                case SkeletonNode s:
                    return new SkeletonSceneNodeViewModel(s, pf, skeletonAnimationLookUpHelper);
                case Rmv2LodNode n:
                {
                    if (n.IsEditable && n.Parent != null)
                        return new LodGroupNodeViewModel(n, componentManager);

                    return null;
                }
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
