using GameWorld.Core.Components.Rendering;
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

        // Cached scene statistics (updated once per second)
        private int _objectCount;
        private int _vertexCount;
        private int _faceCount;

        public SceneInformationComponent(RenderEngineComponent renderEngineComponent, SceneManager sceneManager)
        {
            _renderEngineComponent = renderEngineComponent;
            _sceneManager = sceneManager;
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
            var statsItem = new FontRenderItem(_renderEngineComponent, $"Objects: {_objectCount}  Verts: {_vertexCount}  Faces: {_faceCount}", new Vector2(5, 25), Color.LightGray);
            _renderEngineComponent.AddRenderItem(RenderBuckedId.Font, statsItem);
        }
    }
}
