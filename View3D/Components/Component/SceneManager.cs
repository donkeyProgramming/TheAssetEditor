using Common;
using Filetypes.RigidModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using View3D.Components.Gizmo;
using View3D.Components.Rendering;
using View3D.Rendering;
using View3D.Rendering.Geometry;
using View3D.Rendering.RenderItems;
using View3D.SceneNodes;

namespace View3D.Components.Component
{
    public delegate void SceneObjectAddedDelegate(SceneNode parent, SceneNode added);
    public delegate void SceneObjectRemovedDelegate(SceneNode parent, SceneNode toRemove);


    public class SceneManager : BaseComponent
    {
        public event SceneObjectAddedDelegate SceneObjectAdded;
        public event SceneObjectRemovedDelegate SceneObjectRemoved;

        public SceneNode RootNode { get; private set; }

        RenderEngineComponent _renderEngine;

        public SceneManager(WpfGame game) : base(game) 
        {
            RootNode = new GroupNode("Root") { SceneManager = this };
        }

        public override void Initialize()
        {
            _renderEngine = GetComponent<RenderEngineComponent>();
            base.Initialize();
        }

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

        public override void Draw(GameTime gameTime)
        {
            DrawBasicSceneHirarchy(RootNode, Matrix.Identity);
            base.Draw(gameTime);
        }

        void DrawBasicSceneHirarchy(SceneNode root,  Matrix parentMatrix)
        {
            if (root.IsVisible)
            {
                if (root is IDrawableNode drawableNode)
                    _renderEngine.AddRenderItem(RenderBuckedId.Normal, new MeshRenderItem() { World = parentMatrix, Node = drawableNode });

                foreach (var child in root.Children)
                    DrawBasicSceneHirarchy(child, parentMatrix * child.ModelMatrix);
            }
        }
    }
}
