using CommonControls.Common;
using Microsoft.Xna.Framework;
using Monogame.WpfInterop.Common;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using View3D.Components.Rendering;
using View3D.SceneNodes;
using View3D.Utility;

namespace View3D.Components.Component
{
    public static class SpecialNodes
    {
        public static string Root { get => "Root"; }
        public static string EditableModel { get => "Editable Model"; }
        public static string ReferenceMeshs { get => "Reference meshes"; }
    }

    public class SceneObjectAddedEvent
    {
        public ISceneNode Parent { get; init; }
        public ISceneNode Added { get; init; }
    }

    public class SceneObjectRemovedEvent
    {
        public ISceneNode Parent { get; init; }
        public ISceneNode ToRemove { get; init; }
    }

    public class SceneManager : BaseComponent, IDisposable
    {
        ILogger _logger = Logging.Create<SceneManager>();
        public ISceneNode RootNode { get; private set; }


        private readonly RenderEngineComponent _renderEngineComponent;
        private readonly EventHub _eventHub;

        public SceneManager(RenderEngineComponent renderEngineComponent, EventHub eventHub)
        {
            _renderEngineComponent = renderEngineComponent;
            _eventHub = eventHub;
            RootNode = new GroupNode(SpecialNodes.Root) { SceneManager = this, IsEditable = true, IsLockable = false };
            
        }

        public T GetNodeByName<T>(string name) where T : class, ISceneNode
        {
            var node = GetEnumeratorConditional(x => x.Name == name).FirstOrDefault() as T;
            return node;
        }

        public IEnumerable<ISceneNode> GetEnumeratorConditional(Func<ISceneNode, bool> condition)
        {
            return RootNode.Search(i => i.Children, condition, SceneExtentions.GraphTraversal.BreadthFirst);
        }

        public void TriggerAddObjectEvent(ISceneNode parent, ISceneNode added)
        {
            _eventHub.Publish(new SceneObjectAddedEvent() { Parent = parent, Added = added });
        }

        public void TriggerRemoveObjectEvent(ISceneNode parent, ISceneNode toRemove)
        {
            _eventHub.Publish(new SceneObjectRemovedEvent() { Parent = parent, ToRemove = toRemove });
        }

        public Matrix GetWorldPosition(ISceneNode node)
        {
            Queue<ISceneNode> nodes = new Queue<ISceneNode>();
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

        void SelectObjectsHirarchy(ISceneNode root, BoundingFrustum frustrum, List<ISelectable> output_selectedNodes)
        {
            if (root.IsVisible)
            {
                if (root is ISelectable selectableNode && selectableNode.IsSelectable)
                {
                    Vector3 pivotPoint = Vector3.Zero;
                    if (selectableNode is Rmv2MeshNode meshNode)
                        pivotPoint = meshNode.Material.PivotPoint;

                    if (IntersectionMath.IntersectObject(frustrum, selectableNode.Geometry, selectableNode.ModelMatrix * Matrix.CreateTranslation(pivotPoint)))
                        output_selectedNodes.Add(selectableNode);
                }

                bool isUnselectableGroup = root is GroupNode groupNode && groupNode.IsLockable == true && groupNode.IsSelectable == false;
                if (!isUnselectableGroup)
                {
                    foreach (var child in root.Children)
                        SelectObjectsHirarchy(child, frustrum, output_selectedNodes);
                }
            }
        }

        void SelectObjectsHirarchy(ISceneNode root, Ray ray, ref ISelectable output_selectedNode, ref float bestDistance)
        {
            if (root.IsVisible)
            {
                if (root is ISelectable selectableNode && selectableNode.IsSelectable)
                {
                    Vector3 pivotPoint = Vector3.Zero;
                    if (selectableNode is Rmv2MeshNode meshNode)
                        pivotPoint = meshNode.Material.PivotPoint;

                    var distance = IntersectionMath.IntersectObject(ray, selectableNode.Geometry, selectableNode.ModelMatrix * Matrix.CreateTranslation(pivotPoint));
                    if (distance != null)
                    {
                        if (distance < bestDistance)
                        {
                            bestDistance = distance.Value;
                            output_selectedNode = selectableNode;
                        }
                    }

                }

                bool isUnselectableGroup = root is GroupNode groupNode && groupNode.IsLockable == true && groupNode.IsSelectable == false;
                if (!isUnselectableGroup)
                {
                    foreach (var child in root.Children)
                        SelectObjectsHirarchy(child, ray, ref output_selectedNode, ref bestDistance);
                }
            }
        }

        public override void Update(GameTime gameTime)
        {
            UpdateSceneHirarchy(RootNode, gameTime);
            base.Update(gameTime);
        }

        void UpdateSceneHirarchy(ISceneNode root, GameTime gameTime)
        {
            if (root.IsVisible)
            {
                foreach (var child in root.Children)
                    UpdateSceneHirarchy(child, gameTime);
            }
        }

        public override void Draw(GameTime gameTime)
        {
            DrawBasicSceneHirarchy(RootNode, Matrix.Identity);
            base.Draw(gameTime);
        }

        void DrawBasicSceneHirarchy(ISceneNode root, Matrix parentMatrix)
        {
            if (root.IsVisible)
            {
                foreach (var child in root.Children)
                {
                    if (child is IDrawableItem drawableNode && child.IsVisible)
                        drawableNode.Render(_renderEngineComponent, parentMatrix);
                    DrawBasicSceneHirarchy(child, parentMatrix * child.ModelMatrix);
                }
            }
        }

        public void DumpToConsole()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("SceneManager content:");
            DumpToConsole(RootNode, 0, stringBuilder);
            _logger.Here().Information(stringBuilder.ToString());
        }

        void DumpToConsole(ISceneNode node, int indentationLevel, StringBuilder output)
        {
            var indent = new string('\t', indentationLevel);
            output.AppendLine($"{indent}[{node.GetType().Name}]{node.Name} isVisible:{node.IsVisible} isEditable:{node.IsEditable} id:{node.Id}");
            foreach (var child in node.Children)
                DumpToConsole(child, indentationLevel + 1, output);
        }


        public void Dispose()
        {
            if (RootNode != null)
                DisposeNode(RootNode);
            RootNode = null;
        }

        void DisposeNode(ISceneNode root)
        {
            if (root is IDisposable disposableObject)
                disposableObject.Dispose();
            foreach (var child in root.Children)
                DisposeNode(child);
        }
    }
}
