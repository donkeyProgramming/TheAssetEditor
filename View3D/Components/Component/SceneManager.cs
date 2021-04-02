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
using View3D.Utility;

namespace View3D.Components.Component
{
    public delegate void SceneObjectAddedDelegate(ISceneNode parent, ISceneNode added);
    public delegate void SceneObjectRemovedDelegate(ISceneNode parent, ISceneNode toRemove);


    public class SceneManager : BaseComponent
    {
        public event SceneObjectAddedDelegate SceneObjectAdded;
        public event SceneObjectRemovedDelegate SceneObjectRemoved;

        public ISceneNode RootNode { get; private set; }

        RenderEngineComponent _renderEngine;

        public SceneManager(WpfGame game) : base(game) 
        {
            RootNode = new GroupNode("Root") { SceneManager = this, IsEditable = true, IsLockable = false };
        }

        public override void Initialize()
        {
            _renderEngine = GetComponent<RenderEngineComponent>();
            base.Initialize();
        }

        public IEnumerable<ISceneNode> GetEnumeratorConditional(Func<ISceneNode, bool> condition)
        {
            return RootNode.Search(i => i.Children, condition, SceneExtentions.GraphTraversal.BreadthFirst);
        }
      
        public void TriggerAddObjectEvent(ISceneNode parent, ISceneNode added)
        {
            SceneObjectAdded?.Invoke(parent, added);
        }

        public void TriggerRemoveObjectEvent(ISceneNode parent, ISceneNode toRemove)
        {
            SceneObjectRemoved?.Invoke(parent, toRemove);
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
                    if (GeometryIntersection.IntersectObject(frustrum, selectableNode.Geometry, selectableNode.ModelMatrix))
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
            if(root.IsVisible)
            {
                if (root is ISelectable selectableNode && selectableNode.IsSelectable)
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
                if (root is SceneNodes.IUpdateable updatableNode)
                    updatableNode.Update(gameTime);

                foreach (var child in root.Children)
                    UpdateSceneHirarchy(child, gameTime);
            }
        }

        public override void Draw(GameTime gameTime)
        {
            DrawBasicSceneHirarchy(RootNode, Matrix.Identity);
            base.Draw(gameTime);
        }

        void DrawBasicSceneHirarchy(ISceneNode root,  Matrix parentMatrix)
        {
            if (root.IsVisible)
            {
                if (root is IDrawableItem drawableNode)
                    drawableNode.Render(_renderEngine, parentMatrix);

                foreach (var child in root.Children)
                    DrawBasicSceneHirarchy(child, parentMatrix * child.ModelMatrix);
            }
        }
    }
}
