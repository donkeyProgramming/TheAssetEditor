using System;
using GameWorld.Core.Commands;
using GameWorld.Core.Components.Input;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.Services;
using GameWorld.Core.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Shared.Core.Events;

namespace GameWorld.Core.Components.Gizmo
{
    public class GizmoComponent : BaseComponent, IDisposable
    {
        private readonly IMouseComponent _mouse;
        private readonly IEventHub _eventHub;

        private readonly IKeyboardComponent _keyboard;
        private readonly SelectionManager _selectionManager;
        private readonly CommandExecutor _commandManager;
        private readonly ArcBallCamera _camera;
        private readonly RenderEngineComponent _resourceLibary;
        private readonly IDeviceResolver _deviceResolverComponent;
        private readonly CommandFactory _commandFactory;
        private int _referenceCursorX;
        private int _referenceCursorY;
        private bool _skipNextDelta = false;  // [FIX] Skip delta after cursor reset
        Gizmo _gizmo;
        bool _isEnabled = false;
        TransformGizmoWrapper _activeTransformation;
        bool _isCtrlPressed = false;

        // ========== [IMMEDIATE TRANSFORM] State Machine ==========
        private enum ImmediateTransformState
        {
            Idle,
            Transforming
        }

        private ImmediateTransformState _immediateState = ImmediateTransformState.Idle;
        private GizmoMode? _immediateMode = null;
        private GizmoAxis _axisLock = GizmoAxis.None;

        // Original transform values for cancellation
        private Vector3 _originalPosition;
        private Quaternion _originalOrientation;
        private Vector3 _originalScale;

        // Sensitivity control
        private float _sensitivityMultiplier = 1.0f;
        private const float PrecisionDampingFactor = 0.1f;


        // Add fields in class (around line 65)
        private bool _immediateCommandStarted = false;

        // [FIX] Accumulated rotation angles to avoid floating point drift
        private float _accumulatedYaw = 0f;
        private float _accumulatedPitch = 0f;
        // [FIX] Flag to skip frame after cursor reset (for rotation)
        private bool _skipRotationFrame = false;
        // ========== [END IMMEDIATE TRANSFORM] ==========

        public GizmoComponent(IEventHub eventHub,
            IKeyboardComponent keyboardComponent, IMouseComponent mouseComponent, ArcBallCamera camera, CommandExecutor commandExecutor,
            RenderEngineComponent resourceLibary, IDeviceResolver deviceResolverComponent, CommandFactory commandFactory,
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

            _eventHub.Register<SelectionChangedEvent>(this, Handle);
        }

        public override void Initialize()
        {
            _gizmo = new Gizmo(_camera, _mouse, _deviceResolverComponent.Device, _resourceLibary);
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

            if (_immediateState == ImmediateTransformState.Transforming)
            {
                CancelImmediateTransform();
            }
        }

        private void GizmoTransformStart()
        {
            if (_immediateState != ImmediateTransformState.Idle)
                return;

            _mouse.MouseOwner = this;
            _activeTransformation.Start(_commandManager);
        }

        private void GizmoTransformEnd()
        {
            if (_immediateState != ImmediateTransformState.Idle)
                return;

            _activeTransformation.Stop(_commandManager);
            if (_mouse.MouseOwner == this)
            {
                _mouse.MouseOwner = null;
                _mouse.ClearStates();
            }
        }

        private void StoreOriginalTransform()
        {
            if (_activeTransformation != null)
            {
                _originalPosition = _activeTransformation.Position;
                _originalOrientation = _activeTransformation.Orientation;
                _originalScale = _activeTransformation.Scale;
                _activeTransformation.SaveOriginalState();  // [FIX] Save original state for cancel
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
            var selectionMode = _selectionManager.GetState().Mode;
            switch (selectionMode)
            {
                case GeometrySelectionMode.Object:
                case GeometrySelectionMode.Face:
                case GeometrySelectionMode.Vertex:
                case GeometrySelectionMode.Bone:
                    break;
                default:
                    return;
            }

            if (!_isEnabled)
                return;

            bool isShiftPressed = _keyboard.IsKeyDown(Keys.LeftShift) || _keyboard.IsKeyDown(Keys.RightShift);
            _sensitivityMultiplier = isShiftPressed ? PrecisionDampingFactor : 1.0f;

            _gizmo.SetDampingFactor(_sensitivityMultiplier);

            _isCtrlPressed = _keyboard.IsKeyDown(Keys.LeftControl);
            if (_gizmo.ActiveMode == GizmoMode.NonUniformScale && _isCtrlPressed)
                _gizmo.ActiveMode = GizmoMode.UniformScale;
            else if (_gizmo.ActiveMode == GizmoMode.UniformScale && !_isCtrlPressed)
                _gizmo.ActiveMode = GizmoMode.NonUniformScale;

            UpdateImmediateTransformState();

            var isCameraMoving = _keyboard.IsKeyDown(Keys.LeftAlt);
            _gizmo.Update(gameTime, !isCameraMoving && _immediateState == ImmediateTransformState.Idle);
        }

        // ========== [IMMEDIATE TRANSFORM] State Machine Implementation ==========

        private void UpdateImmediateTransformState()
        {
            switch (_immediateState)
            {
                case ImmediateTransformState.Idle:
                    UpdateIdleState();
                    break;

                case ImmediateTransformState.Transforming:
                    UpdateTransformingState();
                    break;
            }
        }

        private void UpdateIdleState()
        {
            if (_activeTransformation == null || !_isEnabled)
                return;

            if (_keyboard.IsKeyReleased(Keys.G))
            {
                StartImmediateTransform(GizmoMode.Translate);
                return;
            }

            if (_keyboard.IsKeyReleased(Keys.R))
            {
                StartImmediateTransform(GizmoMode.Rotate);
                return;
            }

            if (_keyboard.IsKeyReleased(Keys.S))
            {
                StartImmediateTransform(GizmoMode.NonUniformScale);
                return;
            }
        }

        private void UpdateTransformingState()
        {
            var currentMouseState = _mouse.State();
            var lastMouseState = _mouse.LastState();

            // [FIX] Check for right mouse button FIRST
            bool rightButtonJustPressed = lastMouseState.RightButton == ButtonState.Released &&
                                           currentMouseState.RightButton == ButtonState.Pressed;

            if (rightButtonJustPressed)
            {
                CancelImmediateTransform();
                return;
            }

            // Check for cancel via Escape key
            if (_keyboard.IsKeyReleased(Keys.Escape))
            {
                CancelImmediateTransform();
                return;
            }

            // Check for axis lock keys (X, Y, Z)
            if (_keyboard.IsKeyReleased(Keys.X))
            {
                _axisLock = (_axisLock == GizmoAxis.X) ? GizmoAxis.None : GizmoAxis.X;
                _gizmo.ActiveAxis = _axisLock;
                return;
            }
            if (_keyboard.IsKeyReleased(Keys.Y))
            {
                _axisLock = (_axisLock == GizmoAxis.Y) ? GizmoAxis.None : GizmoAxis.Y;
                _gizmo.ActiveAxis = _axisLock;
                return;
            }
            if (_keyboard.IsKeyReleased(Keys.Z))
            {
                _axisLock = (_axisLock == GizmoAxis.Z) ? GizmoAxis.None : GizmoAxis.Z;
                _gizmo.ActiveAxis = _axisLock;
                return;
            }

            // Check for commit via Left Mouse Button
            bool leftButtonJustPressed = lastMouseState.LeftButton == ButtonState.Released &&
                                          currentMouseState.LeftButton == ButtonState.Pressed;

            if (leftButtonJustPressed)
            {
                CommitImmediateTransform();
                return;
            }

            // Process mouse movement delta for transform
            ProcessMouseDeltaTransform();
        }

        // Modify StartImmediateTransform method
        private void StartImmediateTransform(GizmoMode mode)
        {
            _immediateMode = mode;
            _immediateState = ImmediateTransformState.Transforming;
            _axisLock = GizmoAxis.None;
            _immediateCommandStarted = false;
            _skipNextDelta = false;
            _skipRotationFrame = false;

            // [FIX] Reset accumulated angles when starting new transform
            _accumulatedYaw = 0f;
            _accumulatedPitch = 0f;

            StoreOriginalTransform();

            _mouse.HideCursor();

            _gizmo.ActiveMode = mode;
            _gizmo.ActiveAxis = GizmoAxis.None;

            _mouse.MouseOwner = this;

            var currentPos = _mouse.Position();
            _referenceCursorX = (int)currentPos.X;
            _referenceCursorY = (int)currentPos.Y;

            _activeTransformation.Start(_commandManager);
            _immediateCommandStarted = true;
        }
        // Modify ProcessMouseDeltaTransform method
        private void ProcessMouseDeltaTransform()
        {
            // [FIX] Skip frame after cursor reset to prevent drift
            if (_skipNextDelta || _skipRotationFrame)
            {
                _skipNextDelta = false;
                _skipRotationFrame = false;
                _mouse.ClearStates();  // Clear stale mouse state
                return;
            }

            var currentPos = _mouse.Position();

            var mouseDelta = new Vector2(
                currentPos.X - _referenceCursorX,
                currentPos.Y - _referenceCursorY
            );

            var effectiveDelta = mouseDelta * _sensitivityMultiplier;

            // [FIX] Skip tiny movements to avoid unnecessary updates
            if (effectiveDelta.LengthSquared() < 0.001f)
                return;

            switch (_immediateMode)
            {
                case GizmoMode.Translate:
                    ApplyTranslationDelta(effectiveDelta);
                    _mouse.SetCursorPosition(_referenceCursorX, _referenceCursorY);
                    _skipNextDelta = true;
                    break;
                case GizmoMode.Rotate:
                    ApplyRotationDelta(effectiveDelta);
                    // Cursor reset is handled in ApplyRotationDelta
                    break;
                case GizmoMode.NonUniformScale:
                case GizmoMode.UniformScale:
                    ApplyScaleDelta(effectiveDelta);
                    _mouse.SetCursorPosition(_referenceCursorX, _referenceCursorY);
                    _skipNextDelta = true;
                    break;
            }
        }
        private void ApplyTranslationDelta(Vector2 mouseDelta)
        {
            if (_activeTransformation == null)
                return;

            var cameraPos = _camera.Position;
            var cameraLookAt = _camera.LookAt;

            var cameraForward = Vector3.Normalize(cameraLookAt - cameraPos);
            var cameraRight = Vector3.Normalize(Vector3.Cross(Vector3.Up, cameraForward));
            var cameraUp = Vector3.Normalize(Vector3.Cross(cameraForward, cameraRight));

            const float pixelToWorldScale = 0.01f;

            var translation = cameraRight * (mouseDelta.X * pixelToWorldScale)
                            + cameraUp * (-mouseDelta.Y * pixelToWorldScale);

            translation = ApplyAxisConstraint(translation);

            _activeTransformation.GizmoTranslateEvent(translation, _gizmo.ActivePivot);
        }

        // Modify ApplyRotationDelta method
        // Completely rewrite ApplyRotationDelta method
        private void ApplyRotationDelta(Vector2 mouseDelta)
        {
            if (_activeTransformation == null)
                return;

            const float rotationSpeed = 0.5f;

            if (_axisLock == GizmoAxis.None)
            {
                // [FIX] Get camera vectors for screen-space rotation
                var cameraPos = _camera.Position;
                var cameraLookAt = _camera.LookAt;
                var cameraForward = Vector3.Normalize(cameraLookAt - cameraPos);

                // [FIX] Screen-space axes:
                // Screen X axis (horizontal on screen, points right) = camera right
                // Screen Y axis (vertical on screen, points up) = camera up

                // Camera right vector (points right in world space)
                var screenX = Vector3.Normalize(Vector3.Cross(Vector3.Up, cameraForward));

                // Camera up vector (points up in world space)
                var screenY = Vector3.Normalize(Vector3.Cross(cameraForward, screenX));

                // [FIX] Negate mouseDelta.X for correct rotation direction
                // Mouse right (+X) -> model rotates left (counterclockwise when viewed from above)
                // So we negate to make mouse right -> model rotates right (clockwise)
                _accumulatedYaw += -mouseDelta.X * rotationSpeed;

                // Mouse down (+Y) -> model pitches down
                _accumulatedPitch += -mouseDelta.Y * rotationSpeed;

                float totalYawRadians = MathHelper.ToRadians(_accumulatedYaw);
                float totalPitchRadians = MathHelper.ToRadians(_accumulatedPitch);

                // [FIX] Call method to apply total rotation from original state
                _activeTransformation.ApplyTotalRotation(screenX, screenY, totalPitchRadians, totalYawRadians);

                // [FIX] Reset cursor for infinite dragging
                _mouse.SetCursorPosition(_referenceCursorX, _referenceCursorY);
                _skipRotationFrame = true;
            }
            else
            {
                // Axis lock mode: rotate around world axis
                Vector3 rotationAxis;
                float rotationDegrees;

                switch (_axisLock)
                {
                    case GizmoAxis.X:
                        rotationAxis = Vector3.UnitX;
                        rotationDegrees = mouseDelta.Y * rotationSpeed;
                        break;
                    case GizmoAxis.Y:
                        rotationAxis = Vector3.UnitY;
                        rotationDegrees = mouseDelta.X * rotationSpeed;
                        break;
                    case GizmoAxis.Z:
                        rotationAxis = Vector3.UnitZ;
                        rotationDegrees = mouseDelta.X * rotationSpeed;
                        break;
                    default:
                        return;
                }

                float rotationRadians = MathHelper.ToRadians(rotationDegrees);
                var rotMatrix = Matrix.CreateFromAxisAngle(rotationAxis, rotationRadians);
                _activeTransformation.GizmoRotateEvent(rotMatrix, _gizmo.ActivePivot);

                // [FIX] Reset cursor for infinite dragging in axis lock mode too
                _mouse.SetCursorPosition(_referenceCursorX, _referenceCursorY);
                _skipRotationFrame = true;
            }
        }
        private void ApplyScaleDelta(Vector2 mouseDelta)
        {
            if (_activeTransformation == null)
                return;

            // [FIX] Scale direction: mouse up (negative Y) -> smaller, mouse down (positive Y) -> larger
            const float scaleSpeed = 0.01f;
            float scaleFactor = 1.0f + (mouseDelta.Y * scaleSpeed);

            Vector3 scaleVector;
            if (_axisLock == GizmoAxis.X)
                scaleVector = new Vector3(scaleFactor, 1.0f, 1.0f);
            else if (_axisLock == GizmoAxis.Y)
                scaleVector = new Vector3(1.0f, scaleFactor, 1.0f);
            else if (_axisLock == GizmoAxis.Z)
                scaleVector = new Vector3(1.0f, 1.0f, scaleFactor);
            else
                scaleVector = new Vector3(scaleFactor);

            var deltaScale = scaleVector - Vector3.One;
            _activeTransformation.GizmoScaleEvent(deltaScale, _gizmo.ActivePivot);
        }

        private Vector3 ApplyAxisConstraint(Vector3 translation)
        {
            if (_axisLock == GizmoAxis.X)
                return new Vector3(translation.X, 0, 0);
            if (_axisLock == GizmoAxis.Y)
                return new Vector3(0, translation.Y, 0);
            if (_axisLock == GizmoAxis.Z)
                return new Vector3(0, 0, translation.Z);

            return translation;
        }

        private void CommitImmediateTransform()
        {
            _mouse.ShowCursor();
            if (_immediateCommandStarted)
            {
                _activeTransformation.Stop(_commandManager);
                _immediateCommandStarted = false;
            }
            // [FIX] 刷新 Gizmo 位置
            _gizmo.ResetDeltas();
            if (_mouse.MouseOwner == this)
            {
                _mouse.MouseOwner = null;
                _mouse.ClearStates();
            }
            ResetImmediateTransformState();
        }

        private void CancelImmediateTransform()
        {
            _mouse.ShowCursor();
            if (_immediateCommandStarted)
            {
                _activeTransformation.Cancel();
                _immediateCommandStarted = false;
            }
            // [FIX] 清除 Gizmo 选择，让坐标轴消失
            // 下次重新选中模型时，坐标轴会正确显示在模型上
            _gizmo.Selection.Clear();
            _gizmo.ResetDeltas();
            _gizmo.ActiveAxis = GizmoAxis.None;
            if (_mouse.MouseOwner == this)
            {
                _mouse.MouseOwner = null;
                _mouse.ClearStates();
            }
            ResetImmediateTransformState();
        }

        // Modify ResetImmediateTransformState method
        private void ResetImmediateTransformState()
        {
            _immediateState = ImmediateTransformState.Idle;
            _immediateMode = null;
            _axisLock = GizmoAxis.None;
            _skipNextDelta = false;
            _skipRotationFrame = false;
            _accumulatedYaw = 0f;
            _accumulatedPitch = 0f;
        }
        public bool IsImmediateTransformActive()
        {
            return _immediateState == ImmediateTransformState.Transforming;
        }

        // ========== [END IMMEDIATE TRANSFORM] ==========

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

            if (_immediateState == ImmediateTransformState.Transforming)
            {
                CancelImmediateTransform();
            }
        }

        public override void Draw(GameTime gameTime)
        {
            var selectionMode = _selectionManager.GetState().Mode;

            switch (selectionMode)
            {
                case GeometrySelectionMode.Object:
                case GeometrySelectionMode.Face:
                case GeometrySelectionMode.Vertex:
                case GeometrySelectionMode.Bone:
                    break;
                default:
                    return;
            }

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
