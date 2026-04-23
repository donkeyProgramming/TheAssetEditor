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
        public ISelectionState? NewState { get; internal set; }
    }

    public class SelectionManager : BaseComponent, IDisposable
    {
        ISelectionState _currentState;
        private readonly IEventHub _eventHub;
        private readonly RenderEngineComponent _renderEngine;
        BasicShader _wireframeEffect;
        BasicShader _selectedFacesEffect;

        VertexInstanceMesh _vertexRenderer;
        EdgeQuadInstanceMesh _edgeQuadRenderer;
        EdgeQuadRenderItem _edgeQuadRenderItem;
        VertexRenderItem _vertexRenderItem;
        float _vertexSelectionFalloff = 0;
        private readonly IScopedResourceLibrary _resourceLib;
        private readonly IDeviceResolver _deviceResolverComponent;
        private readonly IGraphicsResourceCreator _graphicsResourceCreator;

        private (int v0, int v1)[] _cachedEdgeIndices;
        private Rmv2MeshNode _cachedEdgeMesh;
        private bool _edgeDataDirty = true;

        private Vector3 _samplePos0, _samplePos1;
        private int _sampleIdx0 = 0;
        private int _sampleIdx1 = 1;

        const int MaxRenderEdges = 50000;
        private readonly EdgeData[] _edgeDataCache = new EdgeData[MaxRenderEdges];

        public SelectionManager(IEventHub eventHub, RenderEngineComponent renderEngine, IScopedResourceLibrary resourceLib, IDeviceResolver deviceResolverComponent, IGraphicsResourceCreator graphicsResourceCreator)
        {
            _eventHub = eventHub;
            _renderEngine = renderEngine;
            _resourceLib = resourceLib;
            _deviceResolverComponent = deviceResolverComponent;
            _graphicsResourceCreator = graphicsResourceCreator;
        }

        public override void Initialize()
        {
            CreateSelectionSate(GeometrySelectionMode.Object, null, false);

            _vertexRenderer = new VertexInstanceMesh(_deviceResolverComponent, _resourceLib, _graphicsResourceCreator);
            _edgeQuadRenderer = new EdgeQuadInstanceMesh(_deviceResolverComponent, _resourceLib, _graphicsResourceCreator);
            _edgeQuadRenderItem = new EdgeQuadRenderItem { EdgeQuadRenderer = _edgeQuadRenderer };
            _vertexRenderItem = new VertexRenderItem { VertexRenderer = _vertexRenderer };

            _wireframeEffect = new BasicShader(_deviceResolverComponent.Device, _graphicsResourceCreator);
            _wireframeEffect.DiffuseColour = new Vector3(0.0f, 0.0f, 0.0f);

            _selectedFacesEffect = new BasicShader(_deviceResolverComponent.Device, _graphicsResourceCreator);
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

            _currentState = mode switch
            {
                GeometrySelectionMode.Object => new ObjectSelectionState(),
                GeometrySelectionMode.Face => new FaceSelectionState(),
                GeometrySelectionMode.Vertex => new VertexSelectionState(selectedObj, _vertexSelectionFalloff),
                GeometrySelectionMode.Bone => new BoneSelectionState(selectedObj),
                _ => throw new Exception(),
            };
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
            if (state == null)
                return;

            if (_currentState != null)
                _currentState.SelectionChanged -= SelectionManager_SelectionChanged;

            _currentState = state;
            _currentState.SelectionChanged += SelectionManager_SelectionChanged;
            SelectionManager_SelectionChanged(_currentState, true);
        }

        private void SelectionManager_SelectionChanged(ISelectionState state, bool sendEvent)
        {
            _edgeDataDirty = true;
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
                var geo = vertexObject.Geometry;

                if (_cachedEdgeMesh != vertexObject)
                {
                    _cachedEdgeMesh = vertexObject;
                    _cachedEdgeIndices = BuildEdgeIndexCache(geo);
                    _edgeDataDirty = true;
                }

                if (selectionVertexState.SelectedVertices.Count >= 2)
                {
                    _sampleIdx0 = selectionVertexState.SelectedVertices[0];
                    _sampleIdx1 = selectionVertexState.SelectedVertices[1];
                }
                else if (selectionVertexState.SelectedVertices.Count == 1)
                {
                    _sampleIdx0 = selectionVertexState.SelectedVertices[0];
                    _sampleIdx1 = _sampleIdx0 < geo.VertexCount() - 1 ? _sampleIdx0 + 1 : 0;
                }

                if (!_edgeDataDirty && geo.VertexCount() >= 2)
                {
                    var p0 = geo.GetVertexById(_sampleIdx0);
                    var p1 = geo.GetVertexById(_sampleIdx1);
                    if (p0 != _samplePos0 || p1 != _samplePos1)
                        _edgeDataDirty = true;
                }

                if (_edgeDataDirty)
                {
                    UpdateEdgeQuadData(vertexObject, selectionVertexState);
                    _edgeDataDirty = false;

                    if (geo.VertexCount() >= 2)
                    {
                        _samplePos0 = geo.GetVertexById(_sampleIdx0);
                        _samplePos1 = geo.GetVertexById(_sampleIdx1);
                    }
                }

                _renderEngine.AddRenderItem(RenderBuckedId.Normal, _edgeQuadRenderItem);
                _vertexRenderItem.Node = vertexObject;
                _vertexRenderItem.ModelMatrix = vertexObject.RenderMatrix;
                _vertexRenderItem.SelectedVertices = selectionVertexState;
                _renderEngine.AddRenderItem(RenderBuckedId.Normal, _vertexRenderItem);
            }
            else
            {
                _cachedEdgeMesh = null;
                _edgeDataDirty = true;
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

            if (_edgeQuadRenderer != null)
            {
                _edgeQuadRenderer.Dispose();
                _edgeQuadRenderer = null;
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

        public float VertexSelectionFalloff => _vertexSelectionFalloff;

        private static (int v0, int v1)[] BuildEdgeIndexCache(GameWorld.Core.Rendering.Geometry.MeshObject geo)
        {
            var processedEdges = new HashSet<(int, int)>();
            var result = new List<(int, int)>();

            for (var i = 0; i < geo.IndexArray.Length; i += 3)
            {
                var i0 = geo.IndexArray[i];
                var i1 = geo.IndexArray[i + 1];
                var i2 = geo.IndexArray[i + 2];

                var edges = new[] {
                    (Math.Min(i0, i1), Math.Max(i0, i1)),
                    (Math.Min(i1, i2), Math.Max(i1, i2)),
                    (Math.Min(i0, i2), Math.Max(i0, i2))
                };

                foreach (var edge in edges)
                {
                    if (processedEdges.Add(edge))
                        result.Add(edge);
                }
            }

            return result.ToArray();
        }

        private void UpdateEdgeQuadData(Rmv2MeshNode meshNode, VertexSelectionState selectionState)
        {
            var geo = meshNode.Geometry;
            var matrix = meshNode.RenderMatrix;
            var weights = selectionState.VertexWeights;

            var wireColor = new Vector3(0.15f, 0.15f, 0.15f);
            var selectColor = new Vector3(1.0f, 0.47f, 0.0f);

            var edgeCount = Math.Min(_cachedEdgeIndices.Length, MaxRenderEdges);
            for (var i = 0; i < edgeCount; i++)
            {
                var (v0, v1) = _cachedEdgeIndices[i];
                var w0 = weights[v0];
                var w1 = weights[v1];

                _edgeDataCache[i] = new EdgeData
                {
                    P0 = Vector3.Transform(geo.GetVertexById(v0), matrix),
                    P1 = Vector3.Transform(geo.GetVertexById(v1), matrix),
                    C0 = Vector3.Lerp(wireColor, selectColor, w0),
                    C1 = Vector3.Lerp(wireColor, selectColor, w1),
                    Width = 0
                };
            }

            _edgeQuadRenderItem.Edges = _edgeDataCache;
            _edgeQuadRenderItem.EdgeCount = edgeCount;
            _edgeQuadRenderItem.MarkDirty();
        }
    }
}

