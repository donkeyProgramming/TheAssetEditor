using Microsoft.Xna.Framework;
using Monogame.WpfInterop.Common;
using System;
using View3D.Components.Rendering;
using View3D.Rendering;
using View3D.Rendering.RenderItems;
using View3D.Rendering.Shading;
using View3D.SceneNodes;
using View3D.Utility;

namespace View3D.Components.Component.Selection
{
    public class SelectionChangedEvent
    {
        public ISelectionState NewState { get; internal set; }
    }

    public class SelectionManager : BaseComponent, IDisposable
    {
        ISelectionState _currentState;
        private readonly EventHub _eventHub;
        private readonly RenderEngineComponent _renderEngine;
        BasicShader _wireframeEffect;
        BasicShader _selectedFacesEffect;

        LineMeshRender _lineGeometry;
        VertexInstanceMesh VertexRenderer;
        float _vertexSelectionFallof = 0;
        private readonly ResourceLibary _resourceLib;
        private readonly DeviceResolverComponent _deviceResolverComponent;

        public SelectionManager(EventHub eventHub, RenderEngineComponent renderEngine, ResourceLibary resourceLib, DeviceResolverComponent deviceResolverComponent)
        {
            _eventHub = eventHub;
            _renderEngine = renderEngine;
            _resourceLib = resourceLib;
            _deviceResolverComponent = deviceResolverComponent;
        }

        public override void Initialize()
        {
            CreateSelectionSate(GeometrySelectionMode.Object, null, false);

            _lineGeometry = new LineMeshRender(_resourceLib);
            VertexRenderer = new VertexInstanceMesh(_deviceResolverComponent, _resourceLib);

            _wireframeEffect = new BasicShader(_deviceResolverComponent.Device);
            _wireframeEffect.DiffuseColor = Vector3.Zero;

            _selectedFacesEffect = new BasicShader(_deviceResolverComponent.Device);
            _selectedFacesEffect.DiffuseColor = new Vector3(1, 0, 0);
            _selectedFacesEffect.SpecularColor = new Vector3(1, 0, 0);
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
                    _currentState = new VertexSelectionState(selectedObj, _vertexSelectionFallof);
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

            _lineGeometry.Clear();
            if (selectionState is ObjectSelectionState objectSelectionState)
            {
                foreach (var item in objectSelectionState.CurrentSelection())
                {
                    if (item is Rmv2MeshNode mesh)
                    {
                        _lineGeometry.AddBoundingBox(item.Geometry.BoundingBox);
                        _renderEngine.AddRenderItem(RenderBuckedId.Selection, new LineRenderItem() { ModelMatrix = mesh.RenderMatrix, LineMesh = _lineGeometry });
                    }
                }
            }

            if (selectionState is FaceSelectionState selectionFaceState && selectionFaceState.RenderObject is Rmv2MeshNode meshNode)
            {
                _renderEngine.AddRenderItem(RenderBuckedId.Selection, new GeoRenderItem() { ModelMatrix = meshNode.RenderMatrix, Geometry = meshNode.Geometry, Shader = _selectedFacesEffect, Faces = selectionFaceState.SelectedFaces });
                _renderEngine.AddRenderItem(RenderBuckedId.Wireframe, new GeoRenderItem() { ModelMatrix = meshNode.RenderMatrix, Geometry = meshNode.Geometry, Shader = _wireframeEffect });
            }

            if (selectionState is VertexSelectionState selectionVertexState && selectionVertexState.RenderObject != null)
            {
                var vertexObject = selectionVertexState.RenderObject as Rmv2MeshNode;
                _renderEngine.AddRenderItem(RenderBuckedId.Selection, new VertexRenderItem() { Node = vertexObject, ModelMatrix = vertexObject.RenderMatrix, SelectedVertices = selectionVertexState, VertexRenderer = VertexRenderer });
                _renderEngine.AddRenderItem(RenderBuckedId.Wireframe, new GeoRenderItem() { ModelMatrix = vertexObject.RenderMatrix, Geometry = vertexObject.Geometry, Shader = _wireframeEffect });
            }

            if(selectionState is BoneSelectionState selectionBoneState && selectionBoneState.RenderObject != null)
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
                        _lineGeometry.AddCube(Matrix.CreateScale(0.06f) * bone * renderMatrix * parentWorld, Color.Red);
                        _renderEngine.AddRenderItem(RenderBuckedId.Line, new LineRenderItem() { LineMesh = _lineGeometry, ModelMatrix = Matrix.Identity });

                    }
                }
            }

            base.Draw(gameTime);
        }

        public void Dispose()
        {
            if (_wireframeEffect != null)
            {
                _wireframeEffect.Effect.Dispose();
                _wireframeEffect = null;
            }

            if (_selectedFacesEffect != null)
            {
                _selectedFacesEffect.Effect.Dispose();
                _selectedFacesEffect = null;
            }

            if (VertexRenderer != null)
            {
                VertexRenderer.Dispose();
                VertexRenderer = null;
            }

            if (_lineGeometry != null)
            {
                _lineGeometry.Dispose();
                _lineGeometry = null;
            }

            _currentState?.Clear();
            _currentState = null;
        }

        public void UpdateVertexSelectionFallof(float newValue)
        {
            _vertexSelectionFallof = Math.Clamp(newValue, 0, float.MaxValue);
            var vertexSelectionState = GetState<VertexSelectionState>();
            if (vertexSelectionState != null)
                vertexSelectionState.UpdateWeights(_vertexSelectionFallof);
        }
    }
}

