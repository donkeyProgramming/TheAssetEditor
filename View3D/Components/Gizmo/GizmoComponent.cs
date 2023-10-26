using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monogame.WpfInterop.Common;
using System;
using View3D.Commands;
using View3D.Components.Component;
using View3D.Components.Component.Selection;
using View3D.Components.Input;
using View3D.Components.Rendering;
using View3D.Utility;

namespace View3D.Components.Gizmo
{
    public class GizmoComponent : BaseComponent, IDisposable
    {
        private readonly MouseComponent _mouse;
        private readonly EventHub _eventHub;

        private readonly KeyboardComponent _keyboard;
        private readonly SelectionManager _selectionManager;
        private readonly CommandExecutor _commandManager;
        private readonly ArcBallCamera _camera;
        private readonly ResourceLibary _resourceLibary;
        private readonly DeviceResolverComponent _deviceResolverComponent;
        private readonly CommandFactory _commandFactory;
        Gizmo _gizmo;
        bool _isEnabled = false;
        TransformGizmoWrapper _activeTransformation;
        bool _isCtrlPressed = false;


        public GizmoComponent(EventHub eventHub,
            KeyboardComponent keyboardComponent, MouseComponent mouseComponent, ArcBallCamera camera, CommandExecutor commandExecutor,
            ResourceLibary resourceLibary, DeviceResolverComponent deviceResolverComponent, CommandFactory commandFactory,
            SelectionManager selectionManager)
        {
            UpdateOrder = (int)ComponentUpdateOrderEnum.Gizmo;
            DrawOrder = (int)ComponentDrawOrderEnum.Gizmo;
            _eventHub = eventHub;
            _keyboard = keyboardComponent;
            _mouse = mouseComponent;
            _camera = camera;
            _commandManager = commandExecutor;
            _resourceLibary = resourceLibary;
            _deviceResolverComponent = deviceResolverComponent;
            _commandFactory = commandFactory;
            _selectionManager = selectionManager;

            _eventHub.Register<SelectionChangedEvent>(Handle);
        }

        public override void Initialize()
        {
            _gizmo = new Gizmo(_camera, _mouse, _deviceResolverComponent.Device, _resourceLibary.CreateSpriteBatch(), _resourceLibary.DefaultFont);
            _gizmo.ActivePivot = PivotType.ObjectCenter;
            _gizmo.TranslateEvent += GizmoTranslateEvent;
            _gizmo.RotateEvent += GizmoRotateEvent;
            _gizmo.ScaleEvent += GizmoScaleEvent;
            _gizmo.StartEvent += GizmoTransformStart;
            _gizmo.StopEvent += GizmoTransformEnd;
        }

        private void OnSelectionChanged(ISelectionState state)
        {
            _gizmo.Selection.Clear();
            _activeTransformation = TransformGizmoWrapper.CreateFromSelectionState(state, _commandFactory);
            if (_activeTransformation != null)
                _gizmo.Selection.Add(_activeTransformation);

            _gizmo.ResetDeltas();
        }

        private void GizmoTransformStart()
        {
            _mouse.MouseOwner = this;
            _activeTransformation.Start(_commandManager);
        }

        private void GizmoTransformEnd()
        {
            _activeTransformation.Stop(_commandManager);
            if (_mouse.MouseOwner == this)
            {
                _mouse.MouseOwner = null;
                _mouse.ClearStates();
            }
        }


        private void GizmoTranslateEvent(ITransformable transformable, TransformationEventArgs e)
        {
            _activeTransformation.GizmoTranslateEvent((Vector3)e.Value, e.Pivot);
        }

        private void GizmoRotateEvent(ITransformable transformable, TransformationEventArgs e)
        {
            _activeTransformation.GizmoRotateEvent((Matrix)e.Value, e.Pivot);
        }

        private void GizmoScaleEvent(ITransformable transformable, TransformationEventArgs e)
        {
            var value = (Vector3)e.Value;
            if (_isCtrlPressed)
            {
                if (value.X != 0)
                    value = new Vector3(value.X);
                else if (value.Y != 0)
                    value = new Vector3(value.Y);
                else if (value.Z != 0)
                    value = new Vector3(value.Z);
            }

            _activeTransformation.GizmoScaleEvent(value, e.Pivot);
        }

        public override void Update(GameTime gameTime)
        {
            if (!(_selectionManager.GetState().Mode == GeometrySelectionMode.Object || _selectionManager.GetState().Mode == GeometrySelectionMode.Vertex))
                return;

            if (!_isEnabled)
                return;

            _isCtrlPressed = _keyboard.IsKeyDown(Keys.LeftControl);
            if (_gizmo.ActiveMode == GizmoMode.NonUniformScale && _isCtrlPressed)
                _gizmo.ActiveMode = GizmoMode.UniformScale;
            else if (_gizmo.ActiveMode == GizmoMode.UniformScale && !_isCtrlPressed)
                _gizmo.ActiveMode = GizmoMode.NonUniformScale;

            //// Toggle space mode:
            //if (_keyboard.IsKeyReleased(Keys.Home))
            //    _gizmo.ToggleActiveSpace();

            var isCameraMoving = _keyboard.IsKeyDown(Keys.LeftAlt);
            _gizmo.Update(gameTime, !isCameraMoving);
        }

        public void SetGizmoMode(GizmoMode mode)
        {
            _gizmo.ActiveMode = mode;
            _isEnabled = true;
        }

        public void SetGizmoPivot(PivotType type)
        {
            _gizmo.ActivePivot = type;
        }

        public void Disable()
        {
            _isEnabled = false;
        }

        public override void Draw(GameTime gameTime)
        {
            if (!(_selectionManager.GetState().Mode == GeometrySelectionMode.Object || _selectionManager.GetState().Mode == GeometrySelectionMode.Vertex))
                return;
            if (!_isEnabled)
                return;

            _gizmo.Draw();
        }

        public void ResetScale()
        {
            _gizmo.ScaleModifier = 1;
        }

        public void ModifyGizmoScale(float v)
        {
            _gizmo.ScaleModifier += v;
        }

        public void Dispose()
        {
            _gizmo.Dispose();
        }

        public void Handle(SelectionChangedEvent notification) => OnSelectionChanged(notification.NewState);
    }
}
