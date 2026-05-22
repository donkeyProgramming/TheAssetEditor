using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.Rendering.RenderItems;
using GameWorld.Core.SceneNodes;
using Microsoft.Xna.Framework;

namespace GameWorld.Core.Components
{
    public class SceneInformationComponent : BaseComponent
    {
        private TimeSpan _timeElapsed;
        private readonly RenderEngineComponent _renderEngineComponent;
        private readonly SceneManager _sceneManager;
        private readonly SelectionManager _selectionManager;

        // Cached scene statistics (updated once per second)
        private int _objectCount;
        private int _vertexCount;
        private int _faceCount;

        public SceneInformationComponent(RenderEngineComponent renderEngineComponent, SceneManager sceneManager, SelectionManager selectionManager)
        {
            _renderEngineComponent = renderEngineComponent;
            _sceneManager = sceneManager;
            _selectionManager = selectionManager;
        }

        public override void Update(GameTime gameTime)
        {
            _timeElapsed += gameTime.ElapsedGameTime;
            if (_timeElapsed >= TimeSpan.FromSeconds(1))
            {
                _timeElapsed -= TimeSpan.FromSeconds(1);
                var meshNodes = SceneNodeHelper.GetChildrenOfType<IEditableGeometry>(_sceneManager.RootNode);
                _objectCount = meshNodes.Count;
                _vertexCount = 0;
                _faceCount = 0;
                foreach (var node in meshNodes)
                {
                    if (node.Geometry != null)
                    {
                        _vertexCount += node.Geometry.VertexCount();
                        _faceCount += node.Geometry.IndexArray.Length / 3;
                    }
                }
            }
        }

        public override void Draw(GameTime gameTime)
        {
            var statsText = BuildStatsText();
            var statsItem = new FontRenderItem(_renderEngineComponent, statsText, new Vector2(5, 25), Color.LightGray);
            _renderEngineComponent.AddRenderItem(RenderBuckedId.Font, statsItem);
        }

        private string BuildStatsText()
        {
            var selectionState = _selectionManager.GetState();

            if (selectionState is ObjectSelectionState objectState && objectState.SelectionCount() > 0)
            {
                var selectedObjects = objectState.CurrentSelection();
                var selectedVerts = 0;
                var selectedFaces = 0;

                foreach (var selectable in selectedObjects)
                {
                    if (selectable?.Geometry == null)
                        continue;

                    selectedVerts += selectable.Geometry.VertexCount();
                    selectedFaces += selectable.Geometry.IndexArray.Length / 3;
                }

                return $"Selected Objects: {selectedObjects.Count}  Verts: {selectedVerts}  Faces: {selectedFaces}";
            }

            if (selectionState is FaceSelectionState faceState && faceState.SelectionCount() > 0 && faceState.RenderObject?.Geometry != null)
            {
                var geometry = faceState.RenderObject.Geometry;
                var uniqueVerts = new HashSet<int>();

                foreach (var faceStartIndex in faceState.SelectedFaces)
                {
                    if (faceStartIndex < 0 || faceStartIndex + 2 >= geometry.IndexArray.Length)
                        continue;

                    uniqueVerts.Add(geometry.IndexArray[faceStartIndex]);
                    uniqueVerts.Add(geometry.IndexArray[faceStartIndex + 1]);
                    uniqueVerts.Add(geometry.IndexArray[faceStartIndex + 2]);
                }

                return $"Selected Faces: {faceState.SelectedFaces.Count}  Verts: {uniqueVerts.Count}  Objects: 1";
            }

            if (selectionState is VertexSelectionState vertexState && vertexState.SelectionCount() > 0)
            {
                return $"Selected Vertices: {vertexState.SelectedVertices.Count}  Objects: 1";
            }

            return $"Objects: {_objectCount}  Verts: {_vertexCount}  Faces: {_faceCount}";
        }
    }
}
