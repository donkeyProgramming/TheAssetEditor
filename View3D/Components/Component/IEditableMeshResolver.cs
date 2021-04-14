using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.SceneNodes;

namespace View3D.Components.Component
{
    public interface IEditableMeshResolver : IGameComponent
    {
        MainEditableNode GeEditableMeshRootNode();
    }
}
