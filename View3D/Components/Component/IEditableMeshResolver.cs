using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace View3D.Components.Component
{
    public interface IEditableMeshResolver : IGameComponent
    {
        SceneNode GetEditableMeshNode();
    }
}
