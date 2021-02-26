using System;
using System.Collections.Generic;
using System.Text;
using View3D.SceneNodes;

namespace KitbasherEditor.ViewModels.SceneExplorerNodeViews
{
    public interface ISceneNodeViewModel
    { }

    public static class SceneNodeViewFactory
    {
        public static ISceneNodeViewModel Create(SceneNode node)
        {
            switch (node)
            {
                case Rmv2ModelNode m:
                    return new ModelSceneNodeViewModel(m);

                case Rmv2LodNode l:
                    return new LodSceneNodeViewModel(l);

                case MeshNode m:
                    return new MeshSceneNodeViewModel(m);

                default:
                    return null;
            }
        }
    }
}
