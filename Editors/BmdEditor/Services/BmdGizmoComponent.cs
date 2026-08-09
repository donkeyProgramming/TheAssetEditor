using System;
using Editors.BmdEditor.Exporting;
using Editors.BmdEditor.ViewModels;
using GameWorld.Core.Components;
using GameWorld.Core.Components.Gizmo;
using GameWorld.Core.Components.Input;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Services;
using GameWorld.Core.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Editors.BmdEditor.Services
{
    /// <summary>
    /// A BMD-specific transform gizmo, ported from <c>CscGizmoComponent</c>. The stock
    /// <see cref="GizmoComponent"/> bakes transforms into mesh vertices, which is wrong here -
    /// moving a BMD element means editing its own position/rotation/scale fields. This component
    /// reuses the low-level <see cref="Gizmo"/> widget and routes its deltas into whichever
    /// element is currently selected in the tree, via the gizmo-support members on
    /// <see cref="BmdElementViewModel"/>. Unlike Csc, BMD has no timeline and no parent/attach
    /// hierarchy (every scene group node is an identity transform), so world delta == local delta
    /// and there's no need to re-evaluate a world matrix every frame - the view model's own
    /// position/orientation is already the ground truth.
    /// </summary>
    public class BmdGizmoComponent : BaseComponent, IDisposable
    {
        readonly ArcBallCamera _camera;
        readonly IMouseComponent _mouse;
        readonly IKeyboardComponent _keyboard;
        readonly RenderEngineComponent _renderEngine;
        readonly IDeviceResolver _deviceResolver;
        readonly IGraphicsResourceCreator _graphicsResourceCreator;

        Gizmo? _gizmo;
        readonly TargetAdapter _adapter = new();
        BmdElementViewModel? _element;
        bool _enabled;

        public BmdGizmoComponent(
            ArcBallCamera camera,
            IMouseComponent mouseComponent,
            IKeyboardComponent keyboardComponent,
            RenderEngineComponent renderEngine,
            IDeviceResolver deviceResolver,
            IGraphicsResourceCreator graphicsResourceCreator)
        {
            _camera = camera;
            _mouse = mouseComponent;
            _keyboard = keyboardComponent;
            _renderEngine = renderEngine;
            _deviceResolver = deviceResolver;
            _graphicsResourceCreator = graphicsResourceCreator;

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
            if (mode == GizmoMode.Rotate && _element?.SupportsRotate != true)
                return;
            if (mode == GizmoMode.NonUniformScale && _element?.SupportsScale != true)
                return;
            _gizmo.ActiveMode = mode;
            _enabled = true;
        }

        public void Disable() => _enabled = false;

        public void SetTarget(BmdElementViewModel? element)
        {
            _element = element;
            _gizmo?.ResetDeltas();
        }

        bool IsActive => _enabled && _element != null && _gizmo != null;

        public override void Update(GameTime gameTime)
        {
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

        void SyncAdapter()
        {
            _adapter.Position = _element!.GizmoPosition;
            _adapter.Orientation = _element.GizmoOrientation;
        }

        void OnDragStart()
        {
            _mouse.MouseOwner = this;
        }

        void OnDragEnd()
        {
            if (_mouse.MouseOwner == this)
            {
                _mouse.MouseOwner = null;
                _mouse.ClearStates();
            }
        }

        void OnTranslate(ITransformable transformable, TransformationEventArgs e)
        {
            if (!IsActive)
                return;

            var worldDelta = (Vector3)e.Value!;
            _element!.ApplyTranslateDelta(worldDelta);
            _adapter.Position += worldDelta;
        }

        void OnRotate(ITransformable transformable, TransformationEventArgs e)
        {
            if (!IsActive || _element?.SupportsRotate != true)
                return;

            var deltaMatrix = (Matrix)e.Value!;
            var deltaDegrees = TerryTransform.ExtractSmallRotationDeltaDegrees(deltaMatrix);
            _element.ApplyRotateDelta(deltaDegrees);
        }

        void OnScale(ITransformable transformable, TransformationEventArgs e)
        {
            if (!IsActive || _element?.SupportsScale != true)
                return;

            var value = (Vector3)e.Value!;
            var component = value.X != 0 ? value.X : value.Y != 0 ? value.Y : value.Z;
            var factor = 1 + component;
            if (Math.Abs(factor) < 0.001f)
                return;

            _element.ApplyScaleFactor(factor);
        }

        public void Dispose()
        {
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
