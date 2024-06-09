using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using View3D.Components.Component;
using View3D.Components.Rendering;
using View3D.Rendering.Geometry;

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
