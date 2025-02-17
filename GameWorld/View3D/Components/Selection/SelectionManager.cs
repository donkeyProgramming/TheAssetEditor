using System;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering;
using GameWorld.Core.Rendering.Materials.Shaders;
using GameWorld.Core.Rendering.RenderItems;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Services;
using GameWorld.Core.Utility;
using Microsoft.Xna.Framework;
using Shared.Core.Events;

namespace GameWorld.Core.Components.Selection
{
    public class SelectionChangedEvent
    {
        public ISelectionState NewState { get; internal set; }
    }

    public class SelectionManager : BaseComponent, IDisposable
    {
        ISelectionState _currentState;
        private readonly IEventHub _eventHub;
        private readonly RenderEngineComponent _renderEngine;
        BasicShader _wireframeEffect;
        BasicShader _selectedFacesEffect;

        VertexInstanceMesh _vertexRenderer;
        float _vertexSelectionFalloff = 0;
        private readonly IScopedResourceLibrary _resourceLib;
        private readonly IDeviceResolver _deviceResolverComponent;

        public SelectionManager(IEventHub eventHub, RenderEngineComponent renderEngine, IScopedResourceLibrary resourceLib, IDeviceResolver deviceResolverComponent)
        {
            _eventHub = eventHub;
            _renderEngine = renderEngine;
            _resourceLib = resourceLib;
            _deviceResolverComponent = deviceResolverComponent;
        }

        public override void Initialize()
        {
            CreateSelectionSate(GeometrySelectionMode.Object, null, false);

            _vertexRenderer = new VertexInstanceMesh(_deviceResolverComponent, _resourceLib);

            _wireframeEffect = new BasicShader(_deviceResolverComponent.Device);
            _wireframeEffect.DiffuseColour = Vector3.Zero;

            _selectedFacesEffect = new BasicShader(_deviceResolverComponent.Device);
            _selectedFacesEffect.DiffuseColour = new Vector3(1, 0, 0);
            _selectedFacesEffect.SpecularColour = new Vector3(1, 0, 0);
            _selectedFacesEffect.EnableDefaultLighting();

            base.Initialize();
        }


        public ISelectionState CreateSelectionSate(GeometrySelectionMode mode, ISelectable selectedObj, bool sendEvent = true)
        {
            if (_currentState != null)
            {
                _currentState.Clear();
                _currentState.SelectionChanged -= SelectionManager_SelectionChanged;
            }

            switch (mode)
            {
                case GeometrySelectionMode.Object:
                    _currentState = new ObjectSelectionState();
                    break;

                case GeometrySelectionMode.Face:
                    _currentState = new FaceSelectionState();
                    break;

                case GeometrySelectionMode.Vertex:
                    _currentState = new VertexSelectionState(selectedObj, _vertexSelectionFalloff);
                    break;
                case GeometrySelectionMode.Bone:
                    _currentState = new BoneSelectionState(selectedObj);
                    break;

                default:
                    throw new Exception();
            }

            _currentState.SelectionChanged += SelectionManager_SelectionChanged;
            SelectionManager_SelectionChanged(_currentState, sendEvent);
            return _currentState;
        }

        public ISelectionState GetState() => _currentState;
        public State GetState<State>() where State : class, ISelectionState => _currentState as State;
        public ISelectionState GetStateCopy() => _currentState.Clone();
        public State GetStateCopy<State>() where State : class, ISelectionState => GetState<State>().Clone() as State;

        public void SetState(ISelectionState state)
        {
            _currentState.SelectionChanged -= SelectionManager_SelectionChanged;
            _currentState = state;
            _currentState.SelectionChanged += SelectionManager_SelectionChanged;
            SelectionManager_SelectionChanged(_currentState, true);
        }

        private void SelectionManager_SelectionChanged(ISelectionState state, bool sendEvent)
        {
            _eventHub.Publish(new SelectionChangedEvent { NewState = state });
        }

        public override void Draw(GameTime gameTime)
        {
            var selectionState = GetState();

            if (selectionState is ObjectSelectionState objectSelectionState)
            {
                foreach (var item in objectSelectionState.CurrentSelection())
                {
                    if (item is Rmv2MeshNode mesh)
                        _renderEngine.AddRenderLines(LineHelper.AddBoundingBox(item.Geometry.BoundingBox, Color.Black, mesh.PivotPoint));
                }
            }

            if (selectionState is FaceSelectionState selectionFaceState && selectionFaceState.RenderObject is Rmv2MeshNode meshNode)
            {
                _renderEngine.AddRenderItem(RenderBuckedId.Selection, new PartialGeometryRenderItem(meshNode.Geometry, meshNode.RenderMatrix, _selectedFacesEffect, selectionFaceState.SelectedFaces));
                _renderEngine.AddRenderItem(RenderBuckedId.Wireframe, new GeometryRenderItem(meshNode.Geometry, _wireframeEffect, meshNode.RenderMatrix));
            }

            if (selectionState is VertexSelectionState selectionVertexState && selectionVertexState.RenderObject != null)
            {
                var vertexObject = selectionVertexState.RenderObject as Rmv2MeshNode;
                _renderEngine.AddRenderItem(RenderBuckedId.Normal, new VertexRenderItem() { Node = vertexObject, ModelMatrix = vertexObject.RenderMatrix, SelectedVertices = selectionVertexState, VertexRenderer = _vertexRenderer });
                _renderEngine.AddRenderItem(RenderBuckedId.Wireframe, new GeometryRenderItem(vertexObject.Geometry, _wireframeEffect, vertexObject.RenderMatrix));
            }

            if (selectionState is BoneSelectionState selectionBoneState && selectionBoneState.RenderObject != null)
            {
                var sceneNode = selectionBoneState.RenderObject as Rmv2MeshNode;
                var animPlayer = sceneNode.AnimationPlayer;
                var currentFrame = animPlayer.GetCurrentAnimationFrame();
                var skeleton = selectionBoneState.Skeleton;

                if (currentFrame != null && skeleton != null)
                {
                    var bones = selectionBoneState.CurrentSelection();
                    var renderMatrix = sceneNode.RenderMatrix;
                    var parentWorld = Matrix.Identity;
                    foreach (var boneIdx in bones)
                    {
                        //var currentBoneMatrix = boneMatrix * Matrix.CreateScale(ScaleMult);
                        //var parentBoneMatrix = Skeleton.GetAnimatedWorldTranform(parentIndex) * Matrix.CreateScale(ScaleMult);
                        //_lineRenderer.AddLine(Vector3.Transform(currentBoneMatrix.Translation, parentWorld), Vector3.Transform(parentBoneMatrix.Translation, parentWorld));
                        var bone = currentFrame.GetSkeletonAnimatedWorld(skeleton, boneIdx);
                        bone.Decompose(out var _, out var _, out var trans);
                        _renderEngine.AddRenderLines(LineHelper.CreateCube(Matrix.CreateScale(0.06f) * bone * renderMatrix * parentWorld, Color.Red));
                    }
                }
            }

            base.Draw(gameTime);
        }

        public void Dispose()
        {
            _eventHub?.UnRegister(this);

            if(_currentState != null)
                _currentState.SelectionChanged -= SelectionManager_SelectionChanged;

            if (_wireframeEffect != null)
            {
                _wireframeEffect.Dispose();
                _wireframeEffect = null;
            }

            if (_selectedFacesEffect != null)
            {
                _selectedFacesEffect.Dispose();
                _selectedFacesEffect = null;
            }

            if (_vertexRenderer != null)
            {
                _vertexRenderer.Dispose();
                _vertexRenderer = null;
            }

            _currentState?.Clear();
            _currentState = null;
        }

        public void UpdateVertexSelectionFallof(float newValue)
        {
            _vertexSelectionFalloff = Math.Clamp(newValue, 0, float.MaxValue);
            var vertexSelectionState = GetState<VertexSelectionState>();
            if (vertexSelectionState != null)
                vertexSelectionState.UpdateWeights(_vertexSelectionFalloff);
        }
    }
}

