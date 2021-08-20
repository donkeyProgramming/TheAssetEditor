using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using View3D.Components.Component;

namespace View3D.SceneNodes
{
    public interface ISceneNode
    {
        List<ISceneNode> Children { get; }
        string Id { get; set; }
        bool IsEditable { get; set; }
        bool IsExpanded { get; set; }
        bool IsVisible { get; set; }
        Matrix ModelMatrix { get; set; }
        Matrix RenderMatrix { get; }
        string Name { get; set; }
        ISceneNode Parent { get; set; }
        SceneManager SceneManager { get; set; }

        T AddObject<T>(T item) where T : ISceneNode;
        ISceneNode Clone();
        void ForeachNode(Action<ISceneNode> func);
        ISceneNode RemoveObject(ISceneNode item);
    }
}