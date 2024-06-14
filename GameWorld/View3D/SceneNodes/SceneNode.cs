using System;
using System.Collections.Generic;
using GameWorld.Core.Components;
using Microsoft.Xna.Framework;
using Shared.Core.Misc;

namespace GameWorld.Core.SceneNodes
{
    public abstract class SceneNode : NotifyPropertyChangedImpl, ISceneNode
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public SceneManager SceneManager { get; set; }

        readonly List<ISceneNode> _children = new();

        public List<ISceneNode> Children { get { return _children; } }

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

        virtual public Matrix ModelMatrix { get; set; } = Matrix.Identity;
        virtual public Matrix RenderMatrix { get; protected set; } = Matrix.Identity;
        public float ScaleMult { get; set; } = 1;

        public override string ToString() => Name;

        public ISceneNode RemoveObject(ISceneNode item)
        {
            _children.Remove(item);
            SceneManager?.TriggerRemoveObjectEvent(this, item);
            return item;
        }

        public void ForeachNodeRecursive(Action<ISceneNode> func)
        {
            func.Invoke(this);
            foreach (var child in _children)
                child.ForeachNodeRecursive(func);
        }

        public T AddObject<T>(T item) where T : ISceneNode
        {
            item.SceneManager = SceneManager;
            item.ForeachNodeRecursive((node) => node.SceneManager = SceneManager);

            item.Parent = this;
            _children.Add(item);
            SceneManager?.TriggerAddObjectEvent(this, item);
            return item;
        }

        public abstract ISceneNode CreateCopyInstance();

        public virtual void CopyInto(ISceneNode target)
        {
            var typedTarget = target as SceneNode;

            target.Parent = Parent;
            typedTarget.SceneManager = SceneManager;
            typedTarget.IsEditable = IsEditable;
            typedTarget.IsVisible = IsVisible;
            typedTarget.ScaleMult = ScaleMult;
            typedTarget.Name = Name + " - Clone";
        }
    }
}
