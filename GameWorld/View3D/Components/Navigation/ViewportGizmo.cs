using GameWorld.Core.Components.Input;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering.RenderItems;
using GameWorld.Core.Services;
using GameWorld.Core.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared.Core.Events;

namespace GameWorld.Core.Components.Navigation
{
    public class ViewportGizmo : BaseComponent, IDisposable
    {
        private readonly IDeviceResolver _deviceResolver;
        private readonly ArcBallCamera _camera;
        private readonly IMouseComponent _mouse;
        private readonly RenderEngineComponent _renderEngine;
        private readonly IEventHub _eventHub;
        private readonly IGraphicsResourceCreator _graphicsResourceCreator;

        // Gizmo size and position
        private const float GIZMO_SIZE = 70f;           // Display size (pixels)
        private const float GIZMO_MARGIN = 20f;         // Margin from edge
        private const float AXIS_LENGTH = 1.0f;         // Axis length in local space
        private const float HIT_RADIUS = 18f;           // Click detection radius (pixels)
        private const float LINE_THICKNESS = 2f;        // Axis line thickness
        private const float CIRCLE_RADIUS = 8f;         // Label circle radius
        private const float CENTER_RADIUS = 6f;         // Center indicator radius

        // Colors (Blender style)
        private static readonly Color s_colorX = new(220, 60, 60);    // Red
        private static readonly Color s_colorY = new(60, 220, 60);    // Green
        private static readonly Color s_colorZ = new(60, 100, 220);   // Blue
        private static readonly Color s_colorHighlight = new(255, 200, 50); // Gold
        private static readonly Color s_colorOutline = new(40, 40, 40); // Dark outline
        private static readonly Color s_colorCenter = new(80, 80, 80); // Center circle

        // Rendering
        private Texture2D _whiteTexture;
        private NavigationAxis _hoveredAxis = NavigationAxis.None;
        private Vector2 _screenPosition;

        // Axis data for rendering
        private record AxisDrawData(NavigationAxis Axis, float Depth, Vector2 ScreenPos, bool IsPositive, int AxisIndex);

        public ViewportGizmo(IDeviceResolver deviceResolver, 
            ArcBallCamera camera,
            IMouseComponent mouse, 
            RenderEngineComponent renderEngine,
            IEventHub eventHub,
            IGraphicsResourceCreator graphicsResourceCreator)
        {
            _deviceResolver = deviceResolver;
            _camera = camera;
            _mouse = mouse;
            _renderEngine = renderEngine;
            _eventHub = eventHub;
            _graphicsResourceCreator = graphicsResourceCreator;
        }

        public override void Initialize()
        {
            _whiteTexture = _graphicsResourceCreator.CreateTexture2D(1, 1);
            _whiteTexture.SetData([Color.White]);

            base.Initialize();
        }
        public override void Update(GameTime gameTime)
        {
            // Calculate screen position (top-right corner)
            _screenPosition = new Vector2(
                _deviceResolver.Device.Viewport.Width - GIZMO_MARGIN - GIZMO_SIZE / 2,
                GIZMO_MARGIN + GIZMO_SIZE / 2
            );

            _hoveredAxis = HitTestAxis(_mouse.Position());
        }

        public NavigationAxis HitTestAxis(Vector2 mousePos)
        {
            var axisEndpoints = GetAllAxisScreenPositions();
            var minDist = float.MaxValue;
            var closestAxis = NavigationAxis.None;

            foreach (var data in axisEndpoints)
            {
                var dist = Vector2.Distance(mousePos, data.ScreenPos);
                if (dist < HIT_RADIUS && dist < minDist)
                {
                    minDist = dist;
                    closestAxis = data.Axis;
                }
            }

            return closestAxis;
        }

        /// <summary>
        /// Get screen positions and data for all 6 axis endpoints
        /// </summary>
        private List<AxisDrawData> GetAllAxisScreenPositions()
        {
            var result = new List<AxisDrawData>();
            var rotationMatrix = Matrix.CreateFromYawPitchRoll(_camera.Yaw, _camera.Pitch, 0);
            var scale = GIZMO_SIZE / (AXIS_LENGTH * 2);

            // 6 axes: +X, -X, +Y, -Y, +Z, -Z
            var axes = new[] { NavigationAxis.PosX, NavigationAxis.NegX,
                              NavigationAxis.PosY, NavigationAxis.NegY,
                              NavigationAxis.PosZ, NavigationAxis.NegZ };

            foreach (var axis in axes)
            {
                var axisIndex = ((int)axis - 1) / 2;  // 0=X, 1=Y, 2=Z
                var isPositive = ((int)axis - 1) % 2 == 0;

                // Get base axis direction
                var baseDir = axisIndex switch
                {
                    0 => Vector3.UnitX,
                    1 => Vector3.UnitY,
                    2 => Vector3.UnitZ,
                    _ => Vector3.Zero
                };

                // Apply sign and rotate
                var axisEnd = baseDir * (isPositive ? AXIS_LENGTH : -AXIS_LENGTH);
                var viewMatrix = _camera.ViewMatrix;
                var rotatedAxis = Vector3.TransformNormal(axisEnd, viewMatrix);

                // Calculate screen position
                var screenPos = _screenPosition + new Vector2(-rotatedAxis.X, -rotatedAxis.Y) * scale;

                result.Add(new AxisDrawData(axis,rotatedAxis.Z, screenPos, isPositive, axisIndex));
            }

            return result;
        }

        /// <summary>
        /// Handle mouse click
        /// </summary>
        public bool HandleClick(Vector2 mousePos)
        {
            var hitAxis = HitTestAxis(mousePos);
            if (hitAxis != NavigationAxis.None)
            {
                var viewPreset = ViewPresets.AxisToViewPreset(hitAxis);
               /// ViewPresetRequested?.Invoke(viewPreset);

                _eventHub.PublishGlobalEvent(new ChangeViewportEvent(viewPreset));
                return true;
            }
            return false;
        }

        public override void Draw(GameTime gameTime)
        {
            var axisDataList = GetAllAxisScreenPositions();
            axisDataList.Sort((a, b) => a.Depth.CompareTo(b.Depth));

            DrawAxesLines(axisDataList);
            DrawAxisEndpoints(axisDataList);
            DrawCenterIndicator();
        }

        private void DrawAxesLines(List<AxisDrawData> axisDataList)
        {
            // Use view matrix for correct world-to-screen axis projection
            var viewMatrix = _camera.ViewMatrix;
            var scale = GIZMO_SIZE / (AXIS_LENGTH * 2);

            // Draw lines for each axis pair (+/-)
            for (int axisIndex = 0; axisIndex < 3; axisIndex++)
            {
                var baseDir = axisIndex switch
                {
                    0 => Vector3.UnitX,
                    1 => Vector3.UnitY,
                    2 => Vector3.UnitZ,
                    _ => Vector3.Zero
                };

                // Get color for this axis
                var axisColor = axisIndex switch
                {
                    0 => s_colorX,
                    1 => s_colorY,
                    2 => s_colorZ,
                    _ => Color.White
                };

                // Check if either positive or negative is hovered
                var isHovered = (_hoveredAxis == (NavigationAxis)(axisIndex * 2 + 1)) ||
                                 (_hoveredAxis == (NavigationAxis)(axisIndex * 2 + 2));
                if (isHovered)
                    axisColor = s_colorHighlight;

                // Draw positive line (from center to +endpoint)
                var posEnd = baseDir * AXIS_LENGTH;
                var rotatedPos = Vector3.TransformNormal(posEnd, viewMatrix);
                var posScreen = _screenPosition + new Vector2(-rotatedPos.X, -rotatedPos.Y) * scale;
                DrawThickLine(_screenPosition, posScreen, axisColor * 0.8f, LINE_THICKNESS);

                // Draw negative line (from center to -endpoint)
                var negEnd = -baseDir * AXIS_LENGTH;
                var rotatedNeg = Vector3.TransformNormal(negEnd, viewMatrix);
                var negScreen = _screenPosition + new Vector2(-rotatedNeg.X, -rotatedNeg.Y) * scale;
                DrawThickLine(_screenPosition, negScreen, axisColor * 0.6f, LINE_THICKNESS);
            }
        }

        /// <summary>
        /// Draw axis endpoint circles (6 endpoints)
        /// </summary>
        private void DrawAxisEndpoints(List<AxisDrawData> axisDataList)
        {
            foreach (var data in axisDataList)
            {
                var isHovered = (_hoveredAxis == data.Axis);
                var isFront = data.Depth <= 0;

                // Get base color
                var baseColor = data.AxisIndex switch
                {
                    0 => s_colorX,
                    1 => s_colorY,
                    2 => s_colorZ,
                    _ => Color.White
                };

                // Determine final color
                Color circleColor;
                if (isHovered)
                {
                    circleColor = s_colorHighlight;
                }
                else if (data.IsPositive)
                {
                    // Positive axis: full color
                    circleColor = baseColor * (isFront ? 1.0f : 0.7f);
                }
                else
                {
                    // Negative axis: dimmer, blended with background for back-facing
                    if (isFront)
                    {
                        // Front-facing negative: blend with white
                        circleColor = new Color(
                            (int)(baseColor.R * 0.5f + 127),
                            (int)(baseColor.G * 0.5f + 127),
                            (int)(baseColor.B * 0.5f + 127),
                            220
                        );
                    }
                    else
                    {
                        // Back-facing negative: very dim
                        circleColor = baseColor * 0.4f;
                    }
                }

                // Draw circle
                DrawFilledCircle(data.ScreenPos, CIRCLE_RADIUS, circleColor);
                DrawCircleOutline(data.ScreenPos, CIRCLE_RADIUS, s_colorOutline * 0.8f, 1f);

                // Draw label only for positive axes
                if (data.IsPositive)
                    DrawAxisLabel(data.AxisIndex, data.ScreenPos, isHovered);
            }
        }

        /// <summary>
        /// Draw axis label (X, Y, Z) - only for positive axes
        /// </summary>
        private void DrawAxisLabel(int axisIndex, Vector2 position, bool isHovered)
        {
            string label = axisIndex switch
            {
                0 => "X",
                1 => "Y",
                2 => "Z",
                _ => ""
            };

            var font = _renderEngine.DefaultFont;
            var textSize = font.MeasureString(label);
            var textPos = position - textSize / 2;

            // Draw outline for better visibility
            Color outlineColor = s_colorOutline;
            for (int ox = -1; ox <= 1; ox++)
            {
                for (int oy = -1; oy <= 1; oy++)
                {
                    if (ox != 0 || oy != 0)
                    {
                        _renderEngine.AddRenderItem(RenderBuckedId.Font, new FontRenderItem(_renderEngine, label, textPos + new Vector2(ox, oy), outlineColor));
                    }
                }
            }

            _renderEngine.AddRenderItem(RenderBuckedId.Font, new FontRenderItem(_renderEngine, label, textPos, Color.White));
        }

        private void DrawCenterIndicator()
        {
            // Draw center circle background
            DrawFilledCircle(_screenPosition, CENTER_RADIUS, s_colorCenter * 0.9f);

            // Draw projection mode indicator
            var isPerspective = _camera.CurrentProjectionType == ProjectionType.Perspective;

            if (isPerspective)
            {
                // Perspective: draw a small filled circle
                DrawFilledCircle(_screenPosition, CENTER_RADIUS * 0.4f, Color.White * 0.8f);
            }
            else
            {
                // Ortho: draw a small outline square
                var size = CENTER_RADIUS * 0.5f;
                var p1 = _screenPosition + new Vector2(-size, -size);
                var p2 = _screenPosition + new Vector2(size, -size);
                var p3 = _screenPosition + new Vector2(size, size);
                var p4 = _screenPosition + new Vector2(-size, size);
                DrawThickLine(p1, p2, Color.White * 0.8f, 1.5f);
                DrawThickLine(p2, p3, Color.White * 0.8f, 1.5f);
                DrawThickLine(p3, p4, Color.White * 0.8f, 1.5f);
                DrawThickLine(p4, p1, Color.White * 0.8f, 1.5f);
            }

            // Draw outline
            DrawCircleOutline(_screenPosition, CENTER_RADIUS, s_colorOutline * 0.7f, 1f);
        }

        private void DrawThickLine(Vector2 start, Vector2 end, Color color, float thickness)
        {
            var delta = end - start;
            var length = delta.Length();
            if (length < 0.001f) 
                return; // Skip zero-length lines

            var angle = (float)Math.Atan2(delta.Y, delta.X);
            var origin = new Vector2(0, 0.5f);
            var scale = new Vector2(length, thickness);
            _renderEngine.AddRenderItem(RenderBuckedId.Texture2D, new TextureRenderItem(_renderEngine, _whiteTexture, start, color, angle, origin, scale));
        }

        private void DrawFilledCircle(Vector2 center, float radius, Color color)
        {
            var r = (int)Math.Ceiling(radius);
            for (var y = -r; y <= r; y++)
            {
                for (var x = -r; x <= r; x++)
                {
                    if (x * x + y * y <= radius * radius)
                    {
                        _renderEngine.AddRenderItem(RenderBuckedId.Texture2D, new TextureRenderItem(_renderEngine, _whiteTexture, new Vector2((int)(center.X + x), (int)(center.Y + y)), color, 0, new Vector2(0,0), new Vector2(1, 1)));
                    }
                }
            }
        }

        private void DrawCircleOutline(Vector2 center, float radius, Color color, float thickness)
        {
            var segments = 24;
            var angleStep = MathHelper.TwoPi / segments;

            for (var i = 0; i < segments; i++)
            {
                var angle1 = i * angleStep;
                var angle2 = (i + 1) * angleStep;

                var p1 = center + new Vector2((float)Math.Cos(angle1), (float)Math.Sin(angle1)) * radius;
                var p2 = center + new Vector2((float)Math.Cos(angle2), (float)Math.Sin(angle2)) * radius;

                DrawThickLine(p1, p2, color, thickness);
            }
        }

        public void Dispose()
        {
            _whiteTexture = _graphicsResourceCreator.DisposeTracked(_whiteTexture);
        }
    }
}
