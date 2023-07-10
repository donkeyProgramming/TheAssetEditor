using CommonControls.Services;
using KitbasherEditor.ViewModels.SceneExplorerNodeViews.Rmv2;
using System;
using View3D.SceneNodes;
using View3D.Utility;

namespace KitbasherEditor.ViewModels.SceneExplorerNodeViews
{
    public interface ISceneNodeViewModel : IDisposable
    {
    }

    public class SceneNodeViewFactory
    {
        private readonly SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        private readonly PackFileService _packFileService;
        private readonly ComponentManagerResolver _componentManagerResolver;
        private readonly ApplicationSettingsService _applicationSettingsService;

        public SceneNodeViewFactory(SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, PackFileService packFileService, ComponentManagerResolver componentManagerResolver, ApplicationSettingsService applicationSettingsService)
        {
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
                    return new MainEditableNodeViewModel(mainNode, _skeletonAnimationLookUpHelper, _packFileService, _componentManagerResolver.ComponentManager);
                case Rmv2MeshNode m:
                    return new MeshEditorViewModel(m, _packFileService, _skeletonAnimationLookUpHelper, _componentManagerResolver.ComponentManager, _applicationSettingsService);
                case SkeletonNode s:
                    return new SkeletonSceneNodeViewModel(s, _packFileService, _skeletonAnimationLookUpHelper);
                case Rmv2LodNode n:
                    {
                        if (n.IsEditable && n.Parent != null)
                            return new LodGroupNodeViewModel(n, _componentManagerResolver.ComponentManager);

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
