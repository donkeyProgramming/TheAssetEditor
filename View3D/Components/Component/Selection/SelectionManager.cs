using CommonControls.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using View3D.Components.Gizmo;
using View3D.Components.Input;
using View3D.Components.Rendering;
using View3D.Rendering;
using View3D.Rendering.Geometry;
using View3D.Rendering.RenderItems;
using View3D.Rendering.Shading;
using View3D.Scene;
using View3D.SceneNodes;
using View3D.Utility;
using static CommonControls.Editors.AnimationPack.Converters.AnimationBinFileToXmlConverter;

namespace View3D.Components.Component.Selection
{
    public delegate void SelectionChangedDelegate(ISelectionState state);
    public class SelectionManager : BaseComponent, IDisposable
    {
        public event SelectionChangedDelegate SelectionChanged;

        ILogger _logger = Logging.Create<SelectionManager>();
        ISelectionState _currentState;
        RenderEngineComponent _renderEngine;
        BasicShader _wireframeEffect;
        BasicShader _selectedFacesEffect;

        LineMeshRender _lineGeometry;
        VertexInstanceMesh VertexRenderer;
        float _vertexSelectionFallof = 0;

        public SelectionManager(IComponentManager componentManager ) : base(componentManager) {}


        public override void Initialize()
        {
            CreateSelectionSate(GeometrySelectionMode.Object, null);
            _renderEngine = ComponentManager.GetComponent<RenderEngineComponent>();
            var resourceLibary = ComponentManager.GetComponent<ResourceLibary>();
            var deviceResolver = ComponentManager.GetComponent<DeviceResolverComponent>();

            _lineGeometry = new LineMeshRender(resourceLibary);
            VertexRenderer = new VertexInstanceMesh(ComponentManager);

            _wireframeEffect = new BasicShader(deviceResolver.Device);
            _wireframeEffect.DiffuseColor = Vector3.Zero;

            _selectedFacesEffect = new BasicShader(deviceResolver.Device);
            _selectedFacesEffect.DiffuseColor = new Vector3(1, 0, 0);
            _selectedFacesEffect.SpecularColor = new Vector3(1, 0, 0);
            _selectedFacesEffect.EnableDefaultLighting();

            base.Initialize();
        }

        public ISelectionState CreateSelectionSate(GeometrySelectionMode mode, ISelectable selectedObj)
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
            SelectionManager_SelectionChanged(_currentState);
            return _currentState;
        }

        public ISelectionState GetState()
        {
            return _currentState;
        }

        public State GetState<State>() where State: class, ISelectionState
        {
            return _currentState as State;
        }

        public ISelectionState GetStateCopy()
        {
            return _currentState.Clone();
        }

        public State GetStateCopy<State>() where State : class, ISelectionState
        {
            return GetState<State>().Clone() as State;
        }

        public void SetState(ISelectionState state)
        {
            _currentState.SelectionChanged -= SelectionManager_SelectionChanged;
            _currentState = state;
            _currentState.SelectionChanged += SelectionManager_SelectionChanged;
            SelectionManager_SelectionChanged(_currentState);
        }

        private void SelectionManager_SelectionChanged(ISelectionState state)
        {
            SelectionChanged?.Invoke(state);
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
                _renderEngine.AddRenderItem(RenderBuckedId.Wireframe, new GeoRenderItem() { ModelMatrix = meshNode.RenderMatrix, Geometry = meshNode.Geometry, Shader = _wireframeEffect});
            }

            if (selectionState is VertexSelectionState selectionVertexState && selectionVertexState.RenderObject != null)
            {
                var vertexObject = selectionVertexState.RenderObject as Rmv2MeshNode;
                _renderEngine.AddRenderItem(RenderBuckedId.Selection, new VertexRenderItem() { Node = vertexObject, ModelMatrix = vertexObject.RenderMatrix, SelectedVertices = selectionVertexState, VertexRenderer = VertexRenderer });
                _renderEngine.AddRenderItem(RenderBuckedId.Wireframe, new GeoRenderItem() { ModelMatrix = vertexObject.RenderMatrix, Geometry = vertexObject.Geometry, Shader = _wireframeEffect });
            }

            if (selectionState is BoneSelectionState boneSelectionState)
            {
                var sceneNode = boneSelectionState.RenderObject as Rmv2MeshNode;
                var animPlayer = sceneNode.AnimationPlayer;
                var currentFrame = animPlayer.GetCurrentAnimationFrame();
                var skeleton = boneSelectionState.Skeleton;

                if (currentFrame != null && skeleton != null)
                {
                    var bones = boneSelectionState.CurrentSelection();
                    var renderMatrix = sceneNode.RenderMatrix;
                    var parentWorld = Matrix.Identity;
                    foreach (var boneIdx in bones)
                    {
                        //var currentBoneMatrix = boneMatrix * Matrix.CreateScale(ScaleMult);
                        //var parentBoneMatrix = Skeleton.GetAnimatedWorldTranform(parentIndex) * Matrix.CreateScale(ScaleMult);
                        //_lineRenderer.AddLine(Vector3.Transform(currentBoneMatrix.Translation, parentWorld), Vector3.Transform(parentBoneMatrix.Translation, parentWorld));
                        var bone = currentFrame.GetSkeletonAnimatedWorld(skeleton, boneIdx);
                        bone.Decompose(out var _, out var _, out var trans);
                        _lineGeometry.AddCube(Matrix.CreateScale(0.06f) *  bone * renderMatrix * parentWorld, Color.Red);
                        _renderEngine.AddRenderItem(RenderBuckedId.Line, new LineRenderItem() { LineMesh = _lineGeometry, ModelMatrix = Matrix.Identity });

                    }
                }
            }

            base.Draw(gameTime);
        }

        public void Dispose()
        {
            _wireframeEffect.Effect.Dispose();
            _wireframeEffect = null;
            _selectedFacesEffect.Effect.Dispose();
            _selectedFacesEffect = null;
            VertexRenderer.Dispose();
            VertexRenderer = null;
            _lineGeometry.Dispose();
            _lineGeometry = null;

            if (SelectionChanged != null)
                foreach (var d in SelectionChanged.GetInvocationList())
                    SelectionChanged -= (d as SelectionChangedDelegate);


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

