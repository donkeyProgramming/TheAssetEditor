using CommonControls.Services;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.SceneNodes;
using View3D.Utility;

namespace KitbasherEditor.ViewModels.SceneExplorerNodeViews
{
    public class SkeletonSceneNodeViewModel : ISceneNodeViewModel
    {
        SkeletonNode _meshNode;
        public SkeletonSceneNodeViewModel(SkeletonNode node, PackFileService pf, SkeletonAnimationLookUpHelper animLookUp)
        {
            _meshNode = node;
        }

        public void Dispose()
        {
        }
    }
}
