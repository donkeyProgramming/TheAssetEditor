using System;
using System.Collections.Generic;
using Editors.CscEditor.Data;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering;
using GameWorld.Core.SceneNodes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Editors.CscEditor.Services
{
    /// <summary>
    /// The per-element transform carrier. Element nodes sit flat under the scene root with an
    /// identity ModelMatrix - <see cref="CscAnimationComponent"/> composes world matrices in the
    /// domain and pushes them into <see cref="WorldMatrix"/> here and into the ModelMatrix of
    /// every content node (meshes, dummies). That keeps the engine's picking and highlight code
    /// working, since both read a node's own ModelMatrix and ignore parent-chain transforms.
    /// Draws a locator cross, and a highlight box when its element is selected.
    /// </summary>
    public class CscElementSceneNode : GroupNode, IDrawableItem
    {
        public CscElement Element { get; internal set; }
        readonly CscPlaybackContext _context;

        /// <summary>The element's fully-composed world transform for the current frame.</summary>
        public Matrix WorldMatrix { get; set; } = Matrix.Identity;

        public CscElementSceneNode(CscElement element, CscPlaybackContext context)
            : base($"CscElement_{element.Id}")
        {
            Element = element;
            _context = context;
            IsEditable = false;
        }

        public bool IsSelected => _context.SelectedElementId == Element.Id;

        public void Render(RenderEngineComponent renderEngine, Matrix parentWorld)
        {
            renderEngine.AddRenderLines(LineHelper.AddRgbLocator(WorldMatrix.Translation, 0.4f));

            if (IsSelected)
                renderEngine.AddRenderLines(LineHelper.CreateCube(Matrix.CreateScale(0.6f) * WorldMatrix, Color.White));
        }

        public override ISceneNode CreateCopyInstance() => throw new NotSupportedException();
    }

    /// <summary>Marker for leaf content nodes that receive the element's world transform in
    /// their own ModelMatrix each frame (see CscAnimationComponent.PushWorldToContent). Only
    /// leaves may carry it - a world matrix on an intermediate node would double-transform its
    /// descendants through SceneManager's matrix accumulation.</summary>
    public interface ICscContentDummy : ISceneNode
    {
    }

    /// <summary>Wireframe box dummy used for VFX/SFX/prefab/etc. markers.</summary>
    public class CscBoxDummyNode : GroupNode, IDrawableItem, ICscContentDummy
    {
        public Color Colour { get; set; } = Color.Purple;
        public float Scale { get; set; } = 0.5f;

        public CscBoxDummyNode(string name, Color colour, float scale) : base(name)
        {
            Colour = colour;
            Scale = scale;
            IsEditable = false;
        }

        public void Render(RenderEngineComponent renderEngine, Matrix parentWorld)
        {
            var world = Matrix.CreateScale(Scale) * ModelMatrix * parentWorld;
            renderEngine.AddRenderLines(LineHelper.CreateCube(world, Colour));
        }

        public override ISceneNode CreateCopyInstance() => throw new NotSupportedException();
    }

    /// <summary>Point light: wireframe sphere with the live (possibly animated) range and colour.</summary>
    public class CscPointLightNode : GroupNode, IDrawableItem, ICscContentDummy
    {
        readonly CscElement _element;
        readonly CscPlaybackContext _context;

        public CscPointLightNode(CscElement element, CscPlaybackContext context) : base("PointLight")
        {
            _element = element;
            _context = context;
            IsEditable = false;
        }

        public void Render(RenderEngineComponent renderEngine, Matrix parentWorld)
        {
            var t = _context.CurrentTime;
            var colour = ToColour(_element.LightColour(t));
            var range = Math.Max(0.25f, _element.PointLightRange?.Evaluate(t) ?? 1f);

            var world = ModelMatrix * parentWorld;
            renderEngine.AddRenderLines(CscShapeHelper.WireframeSphere(Matrix.CreateScale(range) * world, colour, 12));
            renderEngine.AddRenderLines(LineHelper.CreateCube(Matrix.CreateScale(0.25f) * world, colour));
        }

        internal static Color ToColour(Vector3 rgb) =>
            new(Math.Clamp(rgb.X, 0, 1), Math.Clamp(rgb.Y, 0, 1), Math.Clamp(rgb.Z, 0, 1));

        public override ISceneNode CreateCopyInstance() => throw new NotSupportedException();
    }

    /// <summary>Spot light: inner/outer wireframe cones with the live length/angles/colour.
    /// Cone extends along local +X - confirmed in-game against a real file, NOT the same
    /// forward-axis convention as cameras (+Z) - see WireframeCone.</summary>
    public class CscSpotLightNode : GroupNode, IDrawableItem, ICscContentDummy
    {
        readonly CscElement _element;
        readonly CscPlaybackContext _context;

        public CscSpotLightNode(CscElement element, CscPlaybackContext context) : base("SpotLight")
        {
            _element = element;
            _context = context;
            IsEditable = false;
        }

        public void Render(RenderEngineComponent renderEngine, Matrix parentWorld)
        {
            var t = _context.CurrentTime;
            var colour = CscPointLightNode.ToColour(_element.LightColour(t));
            var length = Math.Clamp(_element.SpotLightLength?.Evaluate(t) ?? 5f, 0.5f, 200f);
            var inner = _element.SpotLightInnerAngle?.Evaluate(t) ?? 0f;
            var outer = _element.SpotLightOuterAngle?.Evaluate(t) ?? 0.5f;

            var world = ModelMatrix * parentWorld;
            renderEngine.AddRenderLines(CscShapeHelper.WireframeCone(world, colour, length, outer, 16));
            if (inner > 0.001f)
                renderEngine.AddRenderLines(CscShapeHelper.WireframeCone(world, Color.Lerp(colour, Color.White, 0.5f), length, inner, 16));
        }

        public override ISceneNode CreateCopyInstance() => throw new NotSupportedException();
    }

    /// <summary>Camera: wireframe view frustum from the live fov/near/far channels. The far plane
    /// is clamped for display so a 100k-unit far distance doesn't dwarf the scene.</summary>
    public class CscCameraFrustumNode : GroupNode, IDrawableItem, ICscContentDummy
    {
        readonly CscElement _element;
        readonly CscPlaybackContext _context;

        public CscCameraFrustumNode(CscElement element, CscPlaybackContext context) : base("CameraFrustum")
        {
            _element = element;
            _context = context;
            IsEditable = false;
        }

        /// <summary>fov &lt;= 0 (usually -1) means "auto/default" in vanilla files.</summary>
        internal static float EffectiveFovDegrees(CscElement element, float t)
        {
            var fov = element.CameraFov?.Evaluate(t) ?? 45f;
            return fov <= 0.5f ? 45f : Math.Clamp(fov, 1f, 175f);
        }

        public void Render(RenderEngineComponent renderEngine, Matrix parentWorld)
        {
            // While the viewport is looking through this camera, its own frustum lines would
            // just clutter the view from inside.
            if (_context.LookThroughElementId == _element.Id)
                return;

            var t = _context.CurrentTime;
            var fovDegrees = EffectiveFovDegrees(_element, t);
            // The real near/far clip planes, clamped for this wireframe's own display scale only -
            // a real far plane can be scene-scale (e.g. 13000 units) and would dwarf the viewport.
            var near = Math.Max(0.01f, _element.CameraNear?.Evaluate(t) ?? 0.1f);
            var far = Math.Clamp(_element.CameraFar?.Evaluate(t) ?? 100f, near + 0.1f, 50f);

            var roll = _element.CameraRoll?.Evaluate(t) ?? 0f;
            var world = Matrix.CreateRotationZ(roll) * ModelMatrix * parentWorld;
            renderEngine.AddRenderLines(CscShapeHelper.WireframeFrustum(world, Color.DeepSkyBlue, fovDegrees, 16f / 9f, near, far));
            renderEngine.AddRenderLines(LineHelper.CreateCube(Matrix.CreateScale(0.3f) * world, Color.DeepSkyBlue));
        }

        public override ISceneNode CreateCopyInstance() => throw new NotSupportedException();
    }

    /// <summary>Sound sphere: inner/outer falloff radii around the element.</summary>
    public class CscSoundSphereNode : GroupNode, IDrawableItem, ICscContentDummy
    {
        public float InnerRadius { get; set; } = 1;
        public float OuterRadius { get; set; } = 2;

        public CscSoundSphereNode() : base("SoundSphere")
        {
            IsEditable = false;
        }

        public void Render(RenderEngineComponent renderEngine, Matrix parentWorld)
        {
            var world = ModelMatrix * parentWorld;
            renderEngine.AddRenderLines(CscShapeHelper.WireframeSphere(Matrix.CreateScale(Math.Max(0.1f, InnerRadius)) * world, Color.Lime, 12));
            renderEngine.AddRenderLines(CscShapeHelper.WireframeSphere(Matrix.CreateScale(Math.Max(0.1f, OuterRadius)) * world, Color.Green, 12));
        }

        public override ISceneNode CreateCopyInstance() => throw new NotSupportedException();
    }

    public static class CscShapeHelper
    {
        public static VertexPositionColor[] WireframeSphere(Matrix transform, Color colour, int segments)
        {
            var lines = new List<VertexPositionColor>();

            void Circle(Func<float, Vector3> pointAt)
            {
                Vector3? prev = null;
                for (var i = 0; i <= segments; i++)
                {
                    var angle = MathF.Tau * i / segments;
                    var point = Vector3.Transform(pointAt(angle), transform);
                    if (prev.HasValue)
                    {
                        lines.Add(new VertexPositionColor(prev.Value, colour));
                        lines.Add(new VertexPositionColor(point, colour));
                    }
                    prev = point;
                }
            }

            Circle(a => new Vector3(MathF.Cos(a), MathF.Sin(a), 0));
            Circle(a => new Vector3(MathF.Cos(a), 0, MathF.Sin(a)));
            Circle(a => new Vector3(0, MathF.Cos(a), MathF.Sin(a)));
            return [.. lines];
        }

        /// <summary>Cone with apex at the origin extending along +X (with lateral spread on Y/Z) -
        /// spot lights use a DIFFERENT local-forward axis than cameras (which use +Z, see
        /// WireframeFrustum): verified against the real `campaign_teleport_portal_kho_idle.csc`
        /// spotlight, whose local +X transforms to world (0, -1, 0) - exactly straight down, which
        /// is what the user confirmed seeing in-game, while the editor (still assuming +Z at the
        /// time) rendered it along world +X instead.
        /// <paramref name="angle"/> is the full cone angle in radians.</summary>
        public static VertexPositionColor[] WireframeCone(Matrix transform, Color colour, float length, float angle, int segments)
        {
            var radius = length * MathF.Tan(Math.Clamp(angle, 0.001f, MathF.PI - 0.01f) * 0.5f);
            var apex = Vector3.Transform(Vector3.Zero, transform);

            var lines = new List<VertexPositionColor>();
            var basePoints = new Vector3[segments];
            for (var i = 0; i < segments; i++)
            {
                var a = MathF.Tau * i / segments;
                basePoints[i] = Vector3.Transform(new Vector3(length, radius * MathF.Cos(a), radius * MathF.Sin(a)), transform);
            }

            for (var i = 0; i < segments; i++)
            {
                lines.Add(new VertexPositionColor(apex, colour));
                lines.Add(new VertexPositionColor(basePoints[i], colour));
                lines.Add(new VertexPositionColor(basePoints[i], colour));
                lines.Add(new VertexPositionColor(basePoints[(i + 1) % segments], colour));
            }

            return [.. lines];
        }

        /// <summary>View frustum looking along +Z (CSC's camera convention, verified against the
        /// nor_sayl porthole scene where the camera's local +Z points at the character): near/far
        /// rectangles joined at the corners.</summary>
        public static VertexPositionColor[] WireframeFrustum(Matrix transform, Color colour, float fovDegrees, float aspect, float near, float far)
        {
            var halfTan = MathF.Tan(MathHelper.ToRadians(fovDegrees) * 0.5f);

            Vector3[] Plane(float distance)
            {
                var halfHeight = halfTan * distance;
                var halfWidth = halfHeight * aspect;
                return
                [
                    Vector3.Transform(new Vector3(-halfWidth, -halfHeight, distance), transform),
                    Vector3.Transform(new Vector3(halfWidth, -halfHeight, distance), transform),
                    Vector3.Transform(new Vector3(halfWidth, halfHeight, distance), transform),
                    Vector3.Transform(new Vector3(-halfWidth, halfHeight, distance), transform),
                ];
            }

            var n = Plane(near);
            var f = Plane(far);

            var lines = new List<VertexPositionColor>();
            void Line(Vector3 a, Vector3 b)
            {
                lines.Add(new VertexPositionColor(a, colour));
                lines.Add(new VertexPositionColor(b, colour));
            }

            for (var i = 0; i < 4; i++)
            {
                Line(n[i], n[(i + 1) % 4]);
                Line(f[i], f[(i + 1) % 4]);
                Line(n[i], f[i]);
            }

            // A small "up" tick on the far plane so the camera's roll is readable.
            var farTop = (f[2] + f[3]) * 0.5f;
            var tip = farTop + (farTop - Vector3.Transform(new Vector3(0, 0, far), transform)) * 0.3f;
            Line(f[2], tip);
            Line(f[3], tip);

            return [.. lines];
        }
    }
}
