using System;
using Editors.CscEditor.Data;
using GameWorld.Core.Components;
using GameWorld.Core.Components.Gizmo;
using GameWorld.Core.Components.Input;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Services;
using GameWorld.Core.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Editors.CscEditor.Services
{
    /// <summary>
    /// A CSC-specific transform gizmo. The stock <see cref="GizmoComponent"/> bakes transforms
    /// into mesh vertices (the Kitbasher model), which is wrong here - moving a CSC element means
    /// editing its position/rotation/scale channels. This component reuses the low-level
    /// <see cref="Gizmo"/> widget and routes its deltas into the selected element's channel data.
    /// </summary>
    public class CscGizmoComponent : BaseComponent, IDisposable
    {
        readonly ArcBallCamera _camera;
        readonly IMouseComponent _mouse;
        readonly IKeyboardComponent _keyboard;
        readonly RenderEngineComponent _renderEngine;
        readonly IDeviceResolver _deviceResolver;
        readonly IGraphicsResourceCreator _graphicsResourceCreator;
        readonly CscPlaybackContext _context;
        readonly CscSceneGraphBuilder _sceneBuilder;

        Gizmo? _gizmo;
        readonly TargetAdapter _adapter = new();
        CscElement? _element;
        bool _enabled;
        bool _dragActive;
        bool _ignoreUntilRelease;

        /// <summary>Raised after a drag changed element data (mark dirty, refresh details).</summary>
        public event Action? ElementModified;

        /// <summary>Raised when a drag starts and when it ends (or is interrupted), so the whole
        /// drag becomes one undo step.</summary>
        public event Action? DragStarted;
        public event Action? DragCompleted;

        internal bool IsDragActive => _dragActive;

        public CscGizmoComponent(
            ArcBallCamera camera,
            IMouseComponent mouseComponent,
            IKeyboardComponent keyboardComponent,
            RenderEngineComponent renderEngine,
            IDeviceResolver deviceResolver,
            IGraphicsResourceCreator graphicsResourceCreator,
            CscPlaybackContext context,
            CscSceneGraphBuilder sceneBuilder)
        {
            _camera = camera;
            _mouse = mouseComponent;
            _keyboard = keyboardComponent;
            _renderEngine = renderEngine;
            _deviceResolver = deviceResolver;
            _graphicsResourceCreator = graphicsResourceCreator;
            _context = context;
            _sceneBuilder = sceneBuilder;

            UpdateOrder = (int)ComponentUpdateOrderEnum.Gizmo;
            DrawOrder = (int)ComponentDrawOrderEnum.Gizmo;
        }

        public override void Initialize()
        {
            _gizmo = new Gizmo(_camera, _mouse, _deviceResolver.Device, _renderEngine, _graphicsResourceCreator);
            _gizmo.ActivePivot = PivotType.ObjectCenter;
            _gizmo.TranslateEvent += OnTranslate;
            _gizmo.RotateEvent += OnRotate;
            _gizmo.ScaleEvent += OnScale;
            _gizmo.StartEvent += OnDragStart;
            _gizmo.StopEvent += OnDragEnd;
            _gizmo.Selection.Add(_adapter);
        }

        public void SetMode(GizmoMode mode)
        {
            if (_gizmo == null)
                return;
            _gizmo.ActiveMode = mode;
            _enabled = true;
        }

        public void Disable()
        {
            InterruptDrag();
            _enabled = false;
        }

        public void SetTarget(CscElement? element)
        {
            InterruptDrag();
            _element = element != null && !element.IsLegacyRaw ? element : null;
            _gizmo?.ResetDeltas();
        }

        bool IsActive => _enabled && _element != null && _gizmo != null;

        public override void Update(GameTime gameTime)
        {
            if (_ignoreUntilRelease && _mouse.State().LeftButton == ButtonState.Released)
                _ignoreUntilRelease = false;
            if (_dragActive && _mouse.MouseOwner != this)
                InterruptDrag(); // another component took the mouse mid-drag

            if (!IsActive)
                return;

            SyncAdapter();
            var isCameraMoving = _keyboard.IsKeyDown(Keys.LeftAlt);
            _gizmo!.Update(gameTime, !isCameraMoving);
        }

        public override void Draw(GameTime gameTime)
        {
            if (IsActive)
                _gizmo!.Draw();
        }

        Matrix ElementWorld(CscElement element)
        {
            // The animation component's composed matrix (includes attach-bone transforms);
            // domain-only fallback when the node isn't built yet.
            if (_sceneBuilder.ElementNodes.TryGetValue(element.Id, out var node))
                return node.WorldMatrix;
            return element.WorldTransform(_context.CurrentTime);
        }

        void SyncAdapter()
        {
            var world = ElementWorld(_element!);
            _adapter.Position = world.Translation;
            world.Decompose(out _, out var rotation, out _);
            _adapter.Orientation = rotation;
        }

        void OnDragStart()
        {
            if (_ignoreUntilRelease || _dragActive)
                return;

            _mouse.MouseOwner = this;
            _dragActive = true;
            DragStarted?.Invoke();
        }

        void OnDragEnd() => CompleteDrag();

        void CompleteDrag()
        {
            if (!_dragActive)
                return;

            _dragActive = false;
            if (_mouse.MouseOwner == this)
            {
                _mouse.MouseOwner = null;
                _mouse.ClearStates();
            }
            DragCompleted?.Invoke();
        }

        /// <summary>Ends a drag whose target changed (or that lost the mouse) before the button
        /// came up. What the drag already did is kept as its own step; the rest of the held
        /// drag is ignored so it can't move the next target.</summary>
        void InterruptDrag()
        {
            if (!_dragActive)
                return;

            _ignoreUntilRelease = true;
            CompleteDrag();
        }

        /// <summary>
        /// Inverse of the frame the element's local transform lives in (parent world, including
        /// any attach-bone matrix). Recovered from the composed world: W = local * Frame, so
        /// Frame^-1 = W^-1 * local (row-vector convention).
        /// </summary>
        Matrix ParentFrameInverse()
        {
            if (_element!.Parent == null)
                return Matrix.Identity;

            var local = _element.LocalTransform(_context.CurrentTime);
            return Matrix.Invert(ElementWorld(_element)) * local;
        }

        void OnTranslate(ITransformable transformable, TransformationEventArgs e)
        {
            if (!IsActive || _ignoreUntilRelease)
                return;

            var worldDelta = (Vector3)e.Value!;
            var localDelta = Vector3.TransformNormal(worldDelta, ParentFrameInverse());

            var channels = _element!.Position?.Channels;
            if (channels == null || channels.Count < 3)
                return;

            channels[0].OffsetAll(localDelta.X);
            channels[1].OffsetAll(localDelta.Y);
            channels[2].OffsetAll(localDelta.Z);

            _adapter.Position += worldDelta;
            ElementModified?.Invoke();
        }

        void OnRotate(ITransformable transformable, TransformationEventArgs e)
        {
            if (!IsActive || _ignoreUntilRelease)
                return;

            var deltaMatrix = (Matrix)e.Value!;
            var euler = ToEulerXyz(deltaMatrix);

            var channels = _element!.Rotation?.Channels;
            if (channels == null || channels.Count < 3)
                return;

            channels[0].OffsetAll(euler.X);
            channels[1].OffsetAll(euler.Y);
            channels[2].OffsetAll(euler.Z);
            ElementModified?.Invoke();
        }

        void OnScale(ITransformable transformable, TransformationEventArgs e)
        {
            if (!IsActive || _ignoreUntilRelease)
                return;

            var value = (Vector3)e.Value!;
            var component = value.X != 0 ? value.X : value.Y != 0 ? value.Y : value.Z;
            var factor = 1 + component;
            if (Math.Abs(factor) < 0.001f)
                return;

            var channels = _element!.Scale?.Channels;
            if (channels == null || channels.Count < 1)
                return;

            channels[0].ScaleAll(factor);
            ElementModified?.Invoke();
        }

        /// <summary>
        /// Extracts (x, y, z) Euler angles from a rotation matrix under the Rx*Ry*Rz row-vector
        /// composition this editor uses for CSC rotations. Exact for that convention; drag deltas
        /// are tiny per-event, so accumulation stays stable.
        /// </summary>
        internal static Vector3 ToEulerXyz(Matrix m)
        {
            var y = MathF.Asin(Math.Clamp(-m.M13, -1f, 1f));
            float x, z;
            if (MathF.Abs(m.M13) < 0.9999f)
            {
                x = MathF.Atan2(m.M23, m.M33);
                z = MathF.Atan2(m.M12, m.M11);
            }
            else
            {
                x = MathF.Atan2(-m.M32, m.M22);
                z = 0;
            }
            return new Vector3(x, y, z);
        }

        public void Dispose()
        {
            InterruptDrag();
            _gizmo?.Dispose();
        }

        class TargetAdapter : ITransformable
        {
            public Vector3 Position { get; set; }
            public Vector3 Scale { get; set; } = Vector3.One;
            public Quaternion Orientation { get; set; } = Quaternion.Identity;
            public Vector3 GetObjectCentre() => Position;
        }
    }
}
