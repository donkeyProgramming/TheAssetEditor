using GameWorld.Core.Components;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering.Geometry;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace GameWorld.Core.SceneNodes
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

        void ForeachNodeRecursive(Action<ISceneNode> func);
        ISceneNode RemoveObject(ISceneNode item);


        ISceneNode CreateCopyInstance();
        void CopyInto(ISceneNode target);
    }

    public interface IEditableGeometry : ISceneNode
    {
        MeshObject Geometry { get; set; }
    }

    public interface IDrawableItem : ISceneNode
    {
        void Render(RenderEngineComponent renderEngine, Matrix parentWorld);
    }

    public interface ISelectable : ISceneNode
    {
        MeshObject Geometry { get; set; }
        bool IsSelectable { get; set; }
    }

}
