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
    public interface INode
    {
        string Name { get; set; }
        Matrix ModelMatrix { get; }
        SceneNode Parent { get; set; }
    }

    public interface IEditableGeometry : INode
    {
        IGeometry Geometry { get; set; }
    }

    public interface IDrawableItem : INode
    {
        void Render(RenderEngineComponent renderEngine, Matrix parentWorld);
    }


    public interface ISelectable : INode
    {
        IGeometry Geometry { get; set; }
        bool IsSelectable { get; set; }
    }

    public interface IUpdateable : INode
    {
        void Update(GameTime time);
    }

    public abstract class SceneNode : NotifyPropertyChangedImpl, INode
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public SceneManager SceneManager { get; set; }

        List<SceneNode> _children = new List<SceneNode>();

        public List<SceneNode> Children { get { return _children; } }

        public SceneNode Parent { get; set; }

        string _name = "";
        public string Name { get => _name; set => SetAndNotify(ref _name, value); }

        bool _isVisible = true;
        public bool IsVisible { get => _isVisible; set => SetAndNotify(ref _isVisible, value); }

        bool _isEditable = true;
        public bool IsEditable { get => _isEditable; set => SetAndNotify(ref _isEditable, value); }

        bool _isExpanded = true;
        public bool IsExpanded { get => _isExpanded; set => SetAndNotify(ref _isExpanded, value); }

        bool _isSelectAble = true;
        public bool IsSelectable
        {
            get => _isSelectAble && IsVisible && _isEditable;
            set => _isSelectAble = value;
        }

        public Matrix ModelMatrix { get; protected set; } = Matrix.Identity;


        public SceneNode AddObject(SceneNode item)
        {
            item.SceneManager = SceneManager;
            item.ForeachNode((node) => node.SceneManager = SceneManager);

            item.Parent = this;
            _children.Add(item);
            SceneManager?.TriggerAddObjectEvent(this, item);
            return item;
        }

        public SceneNode RemoveObject(SceneNode item)
        {
            _children.Remove(item);
            SceneManager?.TriggerRemoveObjectEvent(this, item);
            return item;
        }

        public override string ToString()
        {
            return Name;
        }

        public abstract SceneNode Clone();

        public void ForeachNode(Action<SceneNode> func)
        {
            func.Invoke(this);
            foreach (var child in _children)
                child.ForeachNode(func);
        }
    }
}
