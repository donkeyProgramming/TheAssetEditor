using Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using View3D.Components.Component;
using View3D.Components.Rendering;
using View3D.Rendering;
using View3D.Rendering.Geometry;

namespace View3D.SceneNodes
{
    public interface IEditableGeometry : ISceneNode
    {
        IGeometry Geometry { get; set; }
    }

    public interface IDrawableItem : ISceneNode
    {
        void Render(RenderEngineComponent renderEngine, Matrix parentWorld);
    }

    public interface ISelectable : ISceneNode
    {
        IGeometry Geometry { get; set; }
        bool IsSelectable { get; set; }
    }

    public interface IUpdateable : ISceneNode
    {
        void Update(GameTime time);
    }

    public abstract class SceneNode : NotifyPropertyChangedImpl, ISceneNode
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public SceneManager SceneManager { get; set; }

        List<ISceneNode> _children = new List<ISceneNode>();

        public List<ISceneNode> Children { get { return _children; } }
        public IEnumerable<ISceneNode> GetChildren() { return Children; }

        public ISceneNode _parent;
        public ISceneNode Parent { get => _parent; set => SetAndNotifyWhenChanged(ref _parent, value); }

        string _name = "";
        public string Name { get => _name; set => SetAndNotifyWhenChanged(ref _name, value); }

        bool _isVisible = true;
        public bool IsVisible { get => _isVisible; set => SetAndNotify(ref _isVisible, value); }

        bool _isEditable = true;
        public bool IsEditable { get => _isEditable; set => SetAndNotify(ref _isEditable, value); }

        bool _isExpanded = true;
        public bool IsExpanded { get => _isExpanded; set => SetAndNotify(ref _isExpanded, value); }


        public Matrix ModelMatrix { get; protected set; } = Matrix.Identity;


        public ISceneNode AddObject(ISceneNode item)
        {
            item.SceneManager = SceneManager;
            item.ForeachNode((node) => node.SceneManager = SceneManager);

            item.Parent = this;
            _children.Add(item);
            SceneManager?.TriggerAddObjectEvent(this, item);
            return item;
        }

        public ISceneNode RemoveObject(ISceneNode item)
        {
            _children.Remove(item);
            SceneManager?.TriggerRemoveObjectEvent(this, item);
            return item;
        }

        public override string ToString()
        {
            return Name;
        }

        public abstract ISceneNode Clone();

        public void ForeachNode(Action<ISceneNode> func)
        {
            func.Invoke(this);
            foreach (var child in _children)
                child.ForeachNode(func);
        }
    }
}
