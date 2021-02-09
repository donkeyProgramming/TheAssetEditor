using Common;
using Filetypes.RigidModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using View3D.Components.Gizmo;
using View3D.Rendering;
using View3D.Rendering.Geometry;

namespace View3D.Components.Component
{

    public static class SceneExtentions
    {
        public static IEnumerable<T> Search<T>(this T node, Func<T, IEnumerable<T>> childs, Func<T, bool> condition, GraphTraversal mode = GraphTraversal.DepthFirst)
        {
            if (node == null || childs == null || condition == null)
                throw new ArgumentNullException();
            if (mode == GraphTraversal.DepthFirst)
                return node.depthFirstTraversal(childs, condition);
            else if (mode == GraphTraversal.DepthFirstNoStackOverflow)
                return node.depthFirstTraversalWithoutStackoverflow(childs, condition);
            else
                return node.breadthFirstTraversal(childs, condition);
        }

        private static IEnumerable<T> depthFirstTraversal<T>(this T node, Func<T, IEnumerable<T>> childs, Func<T, bool> condition)
        {
            IEnumerable<T> childrens = childs(node);
            if (childrens == null)
                yield break;
            if (condition(node))
                yield return node;
            foreach (T i in childrens)
            {
                foreach (T j in depthFirstTraversal(i, childs, condition))
                {
                    if (condition(j))
                        yield return j;
                }
            }
        }

        private static IEnumerable<T> breadthFirstTraversal<T>(this T node, Func<T, IEnumerable<T>> childs, Func<T, bool> condition)
        {
            Queue<T> queue = new Queue<T>();
            queue.Enqueue(node);
            while (queue.Count > 0)
            {
                T currentnode = queue.Dequeue();
                if (condition(currentnode))
                    yield return currentnode;
                IEnumerable<T> childrens = childs(currentnode);
                if (childrens != null)
                {
                    foreach (T child in childrens)
                        queue.Enqueue(child);
                }
            }
        }

        private static IEnumerable<T> depthFirstTraversalWithoutStackoverflow<T>(this T node, Func<T, IEnumerable<T>> childs, Func<T, bool> condition)
        {
            Stack<T> stack = new Stack<T>();
            stack.Push(node);
            while (stack.Count > 0)
            {
                T currentnode = stack.Pop();
                if (condition(currentnode))
                    yield return currentnode;
                var childrens = childs(currentnode);
                if (childrens != null)
                {
                    foreach (var child in childrens)
                        stack.Push(child);
                }
            }
        }

        public enum GraphTraversal { DepthFirst, DepthFirstNoStackOverflow, BreadthFirst }
    }

    public delegate void SceneObjectAddedDelegate(SceneNode parent, SceneNode added);
    public delegate void SceneObjectRemovedDelegate(SceneNode parent, SceneNode toRemove);


    public class SceneManager : BaseComponent
    {
        public event SceneObjectAddedDelegate SceneObjectAdded;
        public event SceneObjectRemovedDelegate SceneObjectRemoved;

        public SceneNode RootNode { get; private set; }

        public SceneManager(WpfGame game) : base(game) 
        {
            RootNode = new GroupNode("Root") { SceneManager = this };
        }

        public IEnumerable<SceneNode> GetEnumerator { get { return RootNode.Search(i => i.Children, i => true, SceneExtentions.GraphTraversal.BreadthFirst); } }

        public IEnumerable<SceneNode> GetEnumeratorConditional(Func<SceneNode, bool> condition)
        {
            return RootNode.Search(i => i.Children, condition, SceneExtentions.GraphTraversal.BreadthFirst);
        }
      
        public void TriggerAddObjectEvent(SceneNode parent, SceneNode added)
        {
            SceneObjectAdded?.Invoke(parent, added);
        }

        public void TriggerRemoveObjectEvent(SceneNode parent, SceneNode toRemove)
        {
            SceneObjectRemoved?.Invoke(parent, toRemove);
        }

        public Matrix GetWorldPosition(INode node)
        {
            Queue<INode> nodes = new Queue<INode>();
            while (node != null)
            {
                nodes.Enqueue(node);
                node = node.Parent;
            }

            var matrix = Matrix.Identity;
            while (nodes.Count != 0)
                matrix *= nodes.Dequeue().ModelMatrix;

            return matrix;
        }

        public bool ContainsObject(SceneNode item)
        {
            foreach (var sceneItem in GetEnumerator)
            {
                if (item == sceneItem)
                    return true;
            }

            return false;
        }

        public ISelectable SelectObject(Ray ray)
        {
            ISelectable bestItem = null;
            float bestDistance = float.MaxValue;
            SelectObjectsHirarchy(RootNode, ray, ref bestItem, ref bestDistance);
            return bestItem;
        }

        public List<ISelectable> SelectObjects(BoundingFrustum frustrum)
        {
            var output = new List<ISelectable>();
            SelectObjectsHirarchy(RootNode, frustrum, output);
            return output;
        }


        void SelectObjectsHirarchy(SceneNode root, BoundingFrustum frustrum, List<ISelectable> output_selectedNodes)
        {
            if (root.IsSelectable)
            {
                if(root is ISelectable selectableNode)
                if (GeometryIntersection.IntersectObject(frustrum, selectableNode.Geometry, selectableNode.ModelMatrix))
                        output_selectedNodes.Add(selectableNode);

                foreach (var child in root.Children)
                    SelectObjectsHirarchy(child, frustrum, output_selectedNodes);
            }
        }

        void SelectObjectsHirarchy(SceneNode root, Ray ray, ref ISelectable output_selectedNode, ref float bestDistance)
        {
            if (root.IsSelectable)
            {
                if (root is ISelectable selectableNode)
                {
                    var distance = GeometryIntersection.IntersectObject(ray, selectableNode.Geometry, selectableNode.ModelMatrix);
                    if (distance != null)
                    {
                        if (distance < bestDistance)
                        {
                            bestDistance = distance.Value;
                            output_selectedNode = selectableNode;
                        }
                    }
                }

                foreach (var child in root.Children)
                    SelectObjectsHirarchy(child, ray, ref output_selectedNode, ref bestDistance);
            }
        }
    }

    public abstract class SceneNode : NotifyPropertyChangedImpl, INode
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public SceneManager SceneManager { get; set; }

        List<SceneNode> _children = new List<SceneNode>();

        public List<SceneNode> Children{ get { return _children; } }

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

    public class VariantMeshNode : GroupNode
    {
        public VariantMeshNode(string name) : base(name) { }
    }

    public class SlotNode : GroupNode
    {
        public SlotNode(string name) : base(name) { }
    }

    public class SlotsNode : GroupNode
    {
        public SlotsNode(string name) : base(name) { }
    }


    public class Rmv2LodNode : GroupNode
    {
        public Rmv2LodNode(string name) : base(name) { }
    }

    public class Rmv2ModelNode: GroupNode
    {
        public Rmv2ModelNode(RmvRigidModel model, GraphicsDevice device, string name)
        {
            Name = name;

            for (int lodIndex = 0; lodIndex < model.Header.LodCount; lodIndex++)
            {
                var lodNode = new Rmv2LodNode("Lod " + lodIndex);

                for (int modelIndex = 0; modelIndex < model.LodHeaders[lodIndex].MeshCount; modelIndex++)
                {
                    var geometry = new Rmv2Geometry(model.MeshList[lodIndex][modelIndex], device);
                    var node = RenderItemHelper.CreateRenderItem(geometry, new Vector3(0, 0, 0), new Vector3(1.0f), model.MeshList[lodIndex][modelIndex].Header.ModelName, device);
                    node.LodIndex = lodIndex;
                    lodNode.AddObject(node);
                }

                lodNode.IsVisible = lodIndex == 0;
                AddObject(lodNode);
            }
        }

        public Rmv2ModelNode(string name)
        {
            Name = name;

            for (int lodIndex = 0; lodIndex < 4; lodIndex++)
            {
                var lodNode = new Rmv2LodNode("Lod " + lodIndex);
                lodNode.IsVisible = lodIndex == 0;
                AddObject(lodNode);
            }
        }

        public void AddModel(RmvRigidModel model, GraphicsDevice device)
        {
            for (int lodIndex = 0; lodIndex < model.Header.LodCount; lodIndex++)
            {
                var lodNode = Children[lodIndex];

                for (int modelIndex = 0; modelIndex < model.LodHeaders[lodIndex].MeshCount; modelIndex++)
                {
                    var geometry = new Rmv2Geometry(model.MeshList[lodIndex][modelIndex], device);
                    var node = RenderItemHelper.CreateRenderItem(geometry, new Vector3(0, 0, 0), new Vector3(1.0f), model.MeshList[lodIndex][modelIndex].Header.ModelName, device);
                    node.LodIndex = lodIndex;
                    lodNode.AddObject(node);
                }
            }
        }
    }

    public class MeshNode: GroupNode, ITransformable, IDrawableNode, ISelectable
    {
        public BasicEffect DefaultEffect { get; set; }
        public BasicEffect WireframeEffect { get; set; }
        public BasicEffect SelectedFacesEffect { get; set; }

        public int LodIndex { get; set; } = -1;
        public IGeometry Geometry { get; set; }

        public void DrawWireframeOverlay(GraphicsDevice device, Matrix parentWorldMatrix, CommonShaderParameters shaderParams)
        {
            WireframeEffect.Projection = shaderParams.Projection;
            WireframeEffect.View = shaderParams.View;
            WireframeEffect.World = ModelMatrix;
            Geometry.ApplyMesh(WireframeEffect, device);
        }

        public void DrawSelectedFaces(GraphicsDevice device, Matrix parentWorldMatrix, CommonShaderParameters shaderParams, List<int> faces)
        {
            SelectedFacesEffect.Projection = shaderParams.Projection;
            SelectedFacesEffect.View = shaderParams.View;
            SelectedFacesEffect.World = ModelMatrix;
            Geometry.ApplyMeshPart(SelectedFacesEffect, device, faces);
        }

        public void DrawBasic(GraphicsDevice device, Matrix parentWorldMatrix, CommonShaderParameters shaderParams)
        {
            DefaultEffect.Projection = shaderParams.Projection;
            DefaultEffect.View = shaderParams.View;
            DefaultEffect.World = ModelMatrix;
            Geometry.ApplyMesh(DefaultEffect, device);
        }

        public override SceneNode Clone()
        {
            var newItem = new MeshNode()
            { 
                Geometry = Geometry.Clone(),
                Position = Position,
                Orientation = Orientation,
                Scale = Scale,
                SceneManager = SceneManager,
                IsEditable = IsEditable,
                IsVisible = IsVisible,
                LodIndex = LodIndex,
                Name = Name + " - Clone",
            };
            newItem.DefaultEffect = (BasicEffect)DefaultEffect.Clone();
            newItem.WireframeEffect = (BasicEffect)WireframeEffect.Clone();
            newItem.SelectedFacesEffect = (BasicEffect)SelectedFacesEffect.Clone();
            return newItem;
        }
    }


    public class GroupNode : SceneNode, ITransformable
    {
        Quaternion _orientation = Quaternion.Identity;
        Vector3 _position = Vector3.Zero;
        Vector3 _scale = Vector3.One;

        public Vector3 Position { get { return _position; } set { _position = value; UpdateMatrix(); } }
        public Vector3 Scale { get { return _scale; } set { _scale = value; UpdateMatrix(); } }
        public Quaternion Orientation { get { return _orientation; } set { _orientation = value; UpdateMatrix(); } }

        public GroupNode(string name = "")
        {
            Name = name;
        }

        public override SceneNode Clone()
        {
            var newItem = new GroupNode()
            {
                Position = Position,
                Orientation = Orientation,
                Scale = Scale,
                SceneManager = SceneManager,
                IsEditable = IsEditable,
                IsVisible = IsVisible,
                Name = Name + " - Clone",
            };
            return newItem;
        }

        void UpdateMatrix()
        {
            ModelMatrix = Matrix.CreateScale(Scale) * Matrix.CreateFromQuaternion(Orientation) * Matrix.CreateTranslation(Position);
        }
    }

    public interface IDrawableNode : INode
    {
        IGeometry Geometry { get; set; }

        void DrawWireframeOverlay(GraphicsDevice device, Matrix parentWorldMatrix, CommonShaderParameters shaderParams);
        void DrawSelectedFaces(GraphicsDevice device, Matrix parentWorldMatrix, CommonShaderParameters shaderParams, List<int> faces);
        void DrawBasic(GraphicsDevice device, Matrix parentWorldMatrix, CommonShaderParameters shaderParams);
    }

    public interface ISelectable : INode
    {
        IGeometry Geometry { get; set; }
        bool IsSelectable { get; set; }
    }

    public interface INode
    {
        string Name { get; set; }
        Matrix ModelMatrix { get; }
        SceneNode Parent { get; set; }
    }
}
