using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering;
using GameWorld.Core.Services;
using GameWorld.Core.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Serilog;
using Shared.Core.ErrorHandling;

namespace GameWorld.Core.Components
{
    /// <summary>
    /// Infinite grid component using a procedural screen-space shader.
    /// Renders a camera-following ground plane quad with analytically anti-aliased
    /// grid lines computed in the fragment shader via frac() + fwidth() + smoothstep().
    /// </summary>
    public class GridComponent : BaseComponent, IDisposable
    {
        private readonly ILogger _logger = Logging.Create<GridComponent>();
        private readonly ArcBallCamera _camera;
        private readonly ResourceLibrary _resourceLibrary;
        private readonly IDeviceResolver _deviceResolver;

        // Shader resources
        private Effect _gridEffect;
        private EffectPass _gridPass;
        private EffectParameter _worldParam;
        private EffectParameter _viewParam;
        private EffectParameter _projectionParam;
        private EffectParameter _cameraPosParam;
        private EffectParameter _gridColorParam;
        private EffectParameter _cameraDistParam;
        private EffectParameter _isOrthoParam;

        // Reusable quad vertices (triangle strip: 4 verts = 2 triangles)
        private readonly VertexPositionTexture[] _quadVertices = new VertexPositionTexture[4];
        private bool _firstRenderLogged = false;

        public bool ShowGrid { get; set; } = true;
        // Pure black: grid lines invisible, only axis lines (red X / blue Z) visible
        public Vector3 GridColur { get; set; } = new Vector3(0f, 0f, 0f);

        public GridComponent(ArcBallCamera camera, ResourceLibrary resourceLibrary, IDeviceResolver deviceResolver)
        {
            _camera = camera;
            _resourceLibrary = resourceLibrary;
            _deviceResolver = deviceResolver;
        }

        public override void Initialize()
        {
            // Clone the cached effect so this GridComponent has its own parameter buffers.
            // The ResourceLibrary singleton caches one Effect shared by all viewports;
            // without cloning, two GridComponents would overwrite each other's shader
            // parameters on the same Effect object, causing incorrect rendering in
            // the second viewport (KitbashEditor, animation editors, etc.).
            var cachedEffect = _resourceLibrary.LoadEffect("Shaders\\GridShader", ShaderTypes.Grid);

            if (cachedEffect == null)
            {
                _logger.Here().Error("GridShader failed to load - effect is null");
                return;
            }

            _gridEffect = cachedEffect.Clone();

            _gridPass = _gridEffect.Techniques["Grid"].Passes[0];
            _worldParam = _gridEffect.Parameters["World"];
            _viewParam = _gridEffect.Parameters["View"];
            _projectionParam = _gridEffect.Parameters["Projection"];
            _cameraPosParam = _gridEffect.Parameters["CameraPosition"];
            _gridColorParam = _gridEffect.Parameters["GridColor"];
            _cameraDistParam = _gridEffect.Parameters["CameraDistance"];
            _isOrthoParam = _gridEffect.Parameters["IsOrthographic"];

            _logger.Here().Information($"GridComponent initialized. Effect={_gridEffect != null}, Pass={_gridPass != null}");
        }

        /// <summary>
        /// Called by RenderEngineComponent between 2D clear and 3D object rendering.
        /// Renders the procedural grid quad with alpha blending and depth testing.
        /// </summary>
        public void RenderGrid(GraphicsDevice device, CommonShaderParameters commonShaderParameters)
        {
            if (!ShowGrid || _gridEffect == null)
                return;

            // Log first render for diagnostics
            if (!_firstRenderLogged)
            {
                _firstRenderLogged = true;
                _logger.Here().Information($"GridComponent first render: " +
                    $"CameraPos={_camera.Position}, CameraLookAt={_camera.LookAt}, " +
                    $"GridColur={GridColur}, ShowGrid={ShowGrid}, " +
                    $"Viewport={device.Viewport.Width}x{device.Viewport.Height}, " +
                    $"GridEffect={_gridEffect != null}, GridPass={_gridPass != null}, " +
                    $"GridColorParam={_gridColorParam != null}, CameraDistParam={_cameraDistParam != null}");
            }

            // Calculate quad size based on camera distance
            float cameraDist = Vector3.Distance(_camera.Position, _camera.LookAt);
            if (_camera.CurrentProjectionType == ProjectionType.Orthographic)
                cameraDist = _camera.OrthoSize;

            float halfSize = Math.Clamp(cameraDist * 5.0f, 25f, 8000f);

            // Snap quad center to integer grid positions (camera following)
            float cx = (float)Math.Round(_camera.Position.X);
            float cz = (float)Math.Round(_camera.Position.Z);

            // Build ground plane quad at Y=0 (triangle strip order)
            _quadVertices[0] = new VertexPositionTexture(new Vector3(cx - halfSize, 0, cz + halfSize), Vector2.Zero);
            _quadVertices[1] = new VertexPositionTexture(new Vector3(cx + halfSize, 0, cz + halfSize), Vector2.Zero);
            _quadVertices[2] = new VertexPositionTexture(new Vector3(cx - halfSize, 0, cz - halfSize), Vector2.Zero);
            _quadVertices[3] = new VertexPositionTexture(new Vector3(cx + halfSize, 0, cz - halfSize), Vector2.Zero);

            // Set render state: no backface culling (visible from both sides), alpha blending, depth test
            device.RasterizerState = RasterizerState.CullNone;
            device.BlendState = BlendState.AlphaBlend;
            device.DepthStencilState = DepthStencilState.Default;

            // Set shader parameters
            _worldParam.SetValue(Matrix.Identity);
            _viewParam.SetValue(commonShaderParameters.View);
            _projectionParam.SetValue(commonShaderParameters.Projection);
            _cameraPosParam.SetValue(commonShaderParameters.CameraPosition);
            _gridColorParam.SetValue(GridColur);
            _cameraDistParam.SetValue(cameraDist);
            _isOrthoParam.SetValue(_camera.CurrentProjectionType == ProjectionType.Orthographic ? 1 : 0);

            _gridPass.Apply();
            device.DrawUserPrimitives(PrimitiveType.TriangleStrip, _quadVertices, 0, 2);

            // Restore default blend state
            device.BlendState = BlendState.Opaque;
        }

        public void Dispose()
        {
            // Cloned effect is owned by this component, dispose it
            _gridEffect?.Dispose();
            _gridEffect = null;
        }
    }
}
