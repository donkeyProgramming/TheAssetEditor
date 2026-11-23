using GameWorld.Core.Components;
using GameWorld.Core.Components.Gizmo;
using GameWorld.Core.Components.Input;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Services;
using GameWorld.Core.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Editors.AnimationMeta.SuperView.Editing
{
    /// <summary>
    /// The gizmo SuperView moves and turns META tags with. Like CscGizmoComponent, it reuses the low-level
    /// <see cref="Gizmo"/> widget rather than the stock <see cref="GizmoComponent"/>, which bakes transforms into
    /// mesh vertices: here a drag edits the tag the <see cref="CombatMetaDataEditSession"/> is on, in the frame the
    /// tag's values are stored in. Escape during a drag puts the tag back as it was.
    /// </summary>
    public class CombatMetaDataGizmoComponent : BaseComponent, IDisposable
    {
        readonly ArcBallCamera _camera;
        readonly IMouseComponent _mouse;
        readonly IKeyboardComponent _keyboard;
        readonly RenderEngineComponent _renderEngine;
        readonly IDeviceResolver _deviceResolver;
        readonly IGraphicsResourceCreator _graphicsResourceCreator;
        readonly CombatMetaDataEditSession _session;
        readonly TargetAdapter _adapter = new();

        Gizmo? _gizmo;
        bool _enabled;
        bool _dragActive;
        bool _ignoreUntilRelease;
        CombatMetaDataTransformMode _transformMode;

        public bool IsDragActive => _dragActive;

        // The pointer is over one of the gizmo's handles, so a click there is the gizmo's and not a marker pick.
        public bool IsHandleHovered => IsActive && !_keyboard.IsKeyDown(Keys.LeftAlt) && _gizmo!.ActiveAxis != GizmoAxis.None;

        public CombatMetaDataGizmoComponent(
            ArcBallCamera camera,
            IMouseComponent mouseComponent,
            IKeyboardComponent keyboardComponent,
            RenderEngineComponent renderEngine,
            IDeviceResolver deviceResolver,
            IGraphicsResourceCreator graphicsResourceCreator,
            CombatMetaDataEditSession session)
        {
            _camera = camera;
            _mouse = mouseComponent;
            _keyboard = keyboardComponent;
            _renderEngine = renderEngine;
            _deviceResolver = deviceResolver;
            _graphicsResourceCreator = graphicsResourceCreator;
            _session = session;

            UpdateOrder = (int)ComponentUpdateOrderEnum.Gizmo;
            DrawOrder = (int)ComponentDrawOrderEnum.Gizmo;
        }

        public override void Initialize()
        {
            _gizmo = new Gizmo(_camera, _mouse, _deviceResolver.Device, _renderEngine, _graphicsResourceCreator)
            {
                ActivePivot = PivotType.ObjectCenter,
                GizmoDisplaySpace = TransformSpace.Local,
                ActiveMode = ToGizmoMode(_transformMode),
            };
            _gizmo.TranslateEvent += OnTranslate;
            _gizmo.RotateEvent += OnRotate;
            _gizmo.StartEvent += OnDragStart;
            _gizmo.StopEvent += OnDragEnd;
            _gizmo.Selection.Add(_adapter);
        }

        public bool IsEnabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value)
                    return;
                if (!value)
                    EndDrag();
                _enabled = value;
            }
        }

        public CombatMetaDataTransformMode TransformMode
        {
            get => _transformMode;
            set
            {
                if (_transformMode == value)
                    return;
                EndDrag();
                _transformMode = value;
                if (_gizmo != null)
                {
                    _gizmo.ActiveMode = ToGizmoMode(value);
                    _gizmo.ResetDeltas();
                }
            }
        }

        static GizmoMode ToGizmoMode(CombatMetaDataTransformMode mode) => mode == CombatMetaDataTransformMode.Rotate ? GizmoMode.Rotate : GizmoMode.Translate;

        /// <summary>Ends a drag still held, before the session moves to another tag. What the drag already did is kept as
        /// its own step; the rest of the held drag is ignored so it can't move the next tag.</summary>
        public void EndDrag()
        {
            if (!_dragActive)
                return;

            _ignoreUntilRelease = true;
            CompleteDrag(cancelled: false);
            _gizmo?.ResetDeltas();
        }

        bool CanEdit => _enabled && _session.CurrentWorldPosition != null;
        bool IsActive => CanEdit && _gizmo != null;

        public override void Update(GameTime gameTime)
        {
            if (_ignoreUntilRelease && _mouse.State().LeftButton == ButtonState.Released)
                _ignoreUntilRelease = false;
            if (_dragActive && _mouse.MouseOwner != this)
                EndDrag(); // another component took the mouse mid-drag
            if (_dragActive && _keyboard.IsKeyDown(Keys.Escape))
                CancelDrag();

            if (!IsActive)
                return;

            if (!_dragActive)
                SyncAdapter();
            var isCameraMoving = _keyboard.IsKeyDown(Keys.LeftAlt);
            _gizmo!.Update(gameTime, !isCameraMoving);
        }

        public override void Draw(GameTime gameTime)
        {
            if (IsActive)
                _gizmo!.Draw();
        }

        void SyncAdapter()
        {
            _adapter.Position = _session.CurrentWorldPosition ?? Vector3.Zero;
            _adapter.Orientation = _session.CurrentWorldOrientation ?? Quaternion.Identity;
        }

        void OnDragStart()
        {
            if (_ignoreUntilRelease || _dragActive || !CanEdit)
                return;
            if (_mouse.MouseOwner != null && _mouse.MouseOwner != this)
                return;
            if (!_session.BeginGesture(_transformMode))
                return;

            _mouse.MouseOwner = this;
            _dragActive = true;
        }

        void OnDragEnd() => CompleteDrag(cancelled: false);

        void CancelDrag()
        {
            _ignoreUntilRelease = true;
            CompleteDrag(cancelled: true);
            _gizmo?.ResetDeltas();
        }

        void CompleteDrag(bool cancelled)
        {
            if (!_dragActive)
                return;

            _dragActive = false;
            if (_mouse.MouseOwner == this)
            {
                _mouse.MouseOwner = null;
                _mouse.ClearStates();
            }
            if (cancelled)
                _session.CancelGesture();
            else
                _session.EndGesture();
            SyncAdapter();
        }

        void OnTranslate(ITransformable transformable, TransformationEventArgs e)
        {
            if (!_dragActive || _ignoreUntilRelease)
                return;

            var worldDelta = (Vector3)e.Value!;
            if (_session.Translate(worldDelta))
                _adapter.Position += worldDelta;
        }

        void OnRotate(ITransformable transformable, TransformationEventArgs e)
        {
            if (!_dragActive || _ignoreUntilRelease)
                return;

            if (_session.Rotate((Matrix)e.Value!) && _session.CurrentWorldOrientation is { } orientation)
                _adapter.Orientation = orientation;
        }

        public void Dispose()
        {
            EndDrag();
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
