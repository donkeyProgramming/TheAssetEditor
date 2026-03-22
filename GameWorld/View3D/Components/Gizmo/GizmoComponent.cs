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
        Gizmo _gizmo;
        bool _isEnabled = false;
        TransformGizmoWrapper _activeTransformation;
        bool _isCtrlPressed = false;

        // ========== [NEW] Blender-style keyboard shortcut state machine ==========
        // Tracks whether we are in a keyboard-activated transform mode
        private bool _isKeyboardTransformActive = false;
        // Stores the mode activated by keyboard (Translate/Rotate/Scale)
        private GizmoMode? _keyboardActivatedMode = null;
        // Tracks whether axis is locked via keyboard (X/Y/Z)
        private bool _isAxisLockedByKeyboard = false;
        // Store original transform for cancel operation
        private Vector3 _originalPosition;
        private Quaternion _originalOrientation;
        private Vector3 _originalScale;
        // ========== [END NEW] ==========

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

            // ========== [NEW] Store original transform on transform start ==========
            _gizmo.StartEvent += StoreOriginalTransform;
            // ========== [END NEW] ==========
        }

        // ========== [NEW METHOD] Store original transform for cancel functionality ==========
        /// <summary>
        /// Stores the original transform values when a transformation starts.
        /// Used to restore the object state when the user cancels the operation.
        /// </summary>
        private void StoreOriginalTransform()
        {
            if (_activeTransformation != null)
            {
                _originalPosition = _activeTransformation.Position;
                _originalOrientation = _activeTransformation.Orientation;
                _originalScale = _activeTransformation.Scale;
            }
        }
        // ========== [END NEW METHOD] ==========

        private void OnSelectionChanged(ISelectionState state)
        {
            _gizmo.Selection.Clear();
            _activeTransformation = TransformGizmoWrapper.CreateFromSelectionState(state, _commandFactory);
            if (_activeTransformation != null)
                _gizmo.Selection.Add(_activeTransformation);

            _gizmo.ResetDeltas();

            // ========== [NEW] Reset keyboard transform state on selection change ==========
            ResetKeyboardTransformState();
            // ========== [END NEW] ==========
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

            // ========== [NEW] Reset keyboard transform state after transform ends ==========
            ResetKeyboardTransformState();
            // ========== [END NEW] ==========
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

        // ========== [NEW METHOD] Handle keyboard shortcut for G/R/S mode activation ==========
        /// <summary>
        /// Checks for G/R/S key presses to activate transform modes (Blender-style).
        /// G = Grab/Translate, R = Rotate, S = Scale.
        /// </summary>
        /// <returns>True if a keyboard shortcut was handled</returns>
        private bool HandleKeyboardShortcutModeActivation()
        {
            // Only handle shortcuts if there is a selection
            if (_activeTransformation == null || !_isEnabled)
                return false;

            // Check for G key - Grab/Translate mode
            if (_keyboard.IsKeyReleased(Keys.G))
            {
                ActivateKeyboardTransformMode(GizmoMode.Translate);
                return true;
            }
            // Check for R key - Rotate mode
            else if (_keyboard.IsKeyReleased(Keys.R))
            {
                ActivateKeyboardTransformMode(GizmoMode.Rotate);
                return true;
            }
            // Check for S key - Scale mode
            else if (_keyboard.IsKeyReleased(Keys.S))
            {
                ActivateKeyboardTransformMode(GizmoMode.NonUniformScale);
                return true;
            }

            return false;
        }
        // ========== [END NEW METHOD] ==========

        // ========== [NEW METHOD] Activate keyboard transform mode ==========
        /// <summary>
        /// Activates a transform mode via keyboard shortcut (Blender-style).
        /// Sets the gizmo to the specified mode and prepares for axis selection.
        /// </summary>
        /// <param name="mode">The gizmo mode to activate</param>
        private void ActivateKeyboardTransformMode(GizmoMode mode)
        {
            _isKeyboardTransformActive = true;
            _keyboardActivatedMode = mode;
            _isAxisLockedByKeyboard = false;

            // Set the gizmo mode
            _gizmo.ActiveMode = mode;

            // Reset axis to None - user must press X/Y/Z or drag to select
            _gizmo.ActiveAxis = GizmoAxis.None;
        }
        // ========== [END NEW METHOD] ==========

        // ========== [NEW METHOD] Handle axis locking via X/Y/Z keys ==========
        /// <summary>
        /// Checks for X/Y/Z key presses to lock transform to specific axis.
        /// Only effective when keyboard transform mode is active.
        /// </summary>
        /// <returns>True if an axis lock was applied</returns>
        private bool HandleAxisLockKeys()
        {
            if (!_isKeyboardTransformActive)
                return false;

            // Check for X key - lock to X axis
            if (_keyboard.IsKeyReleased(Keys.X))
            {
                _isAxisLockedByKeyboard = true;
                _gizmo.ActiveAxis = GizmoAxis.X;
                return true;
            }
            // Check for Y key - lock to Y axis
            else if (_keyboard.IsKeyReleased(Keys.Y))
            {
                _isAxisLockedByKeyboard = true;
                _gizmo.ActiveAxis = GizmoAxis.Y;
                return true;
            }
            // Check for Z key - lock to Z axis
            else if (_keyboard.IsKeyReleased(Keys.Z))
            {
                _isAxisLockedByKeyboard = true;
                _gizmo.ActiveAxis = GizmoAxis.Z;
                return true;
            }

            return false;
        }
        // ========== [END NEW METHOD] ==========

        // ========== [NEW METHOD] Handle Esc key to cancel operation ==========
        /// <summary>
        /// Checks for Escape key press to cancel the current transform operation.
        /// Restores the object to its original transform before the operation started.
        /// </summary>
        /// <returns>True if cancel was triggered</returns>
        private bool HandleEscapeCancel()
        {
            if (!_isKeyboardTransformActive)
                return false;

            if (_keyboard.IsKeyReleased(Keys.Escape))
            {
                CancelCurrentTransform();
                return true;
            }

            return false;
        }
        // ========== [END NEW METHOD] ==========

        // ========== [NEW METHOD] Cancel current transform and restore original state ==========
        /// <summary>
        /// Cancels the current keyboard-initiated transform operation.
        /// Restores the object to its original position/orientation/scale.
        /// </summary>
        private void CancelCurrentTransform()
        {
            // Restore original transform if we have a valid transformation
            if (_activeTransformation != null)
            {
                // Reset the transformation wrapper to original state
                _activeTransformation.Position = _originalPosition;
                _activeTransformation.Orientation = _originalOrientation;
                _activeTransformation.Scale = _originalScale;
            }

            // Reset gizmo state
            _gizmo.ResetDeltas();
            _gizmo.ActiveAxis = GizmoAxis.None;

            // Clear mouse ownership if we own it
            if (_mouse.MouseOwner == this)
            {
                _mouse.MouseOwner = null;
                _mouse.ClearStates();
            }

            // Reset state machine
            ResetKeyboardTransformState();
        }
        // ========== [END NEW METHOD] ==========

        // ========== [NEW METHOD] Reset keyboard transform state machine ==========
        /// <summary>
        /// Resets all keyboard transform state variables to idle state.
        /// </summary>
        public void ResetKeyboardTransformState()
        {
            _isKeyboardTransformActive = false;
            _keyboardActivatedMode = null;
            _isAxisLockedByKeyboard = false;
        }
        // ========== [END NEW METHOD] ==========

        // ========== [NEW METHOD] Check if keyboard transform is active (for testing) ==========
        /// <summary>
        /// Returns whether a keyboard-initiated transform is currently active.
        /// Useful for unit testing the state machine.
        /// </summary>
        public bool IsKeyboardTransformActive()
        {
            return _isKeyboardTransformActive;
        }
        // ========== [END NEW METHOD] ==========

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

            // ========== [NEW] Handle Shift key for precision damping ==========
            // Apply damping factor when Shift is held for fine-tuned adjustments
            bool isShiftPressed = _keyboard.IsKeyDown(Keys.LeftShift) || _keyboard.IsKeyDown(Keys.RightShift);
            _gizmo.SetDampingFactor(isShiftPressed ? 0.1f : 1.0f);
            // ========== [END NEW] ==========

            _isCtrlPressed = _keyboard.IsKeyDown(Keys.LeftControl);
            if (_gizmo.ActiveMode == GizmoMode.NonUniformScale && _isCtrlPressed)
                _gizmo.ActiveMode = GizmoMode.UniformScale;
            else if (_gizmo.ActiveMode == GizmoMode.UniformScale && !_isCtrlPressed)
                _gizmo.ActiveMode = GizmoMode.NonUniformScale;

            // ========== [NEW] Handle Blender-style keyboard shortcuts ==========
            // Priority: Esc cancel > Axis lock > Mode activation
            if (HandleEscapeCancel())
            {
                // Transform was cancelled, skip further processing
                return;
            }

            if (HandleAxisLockKeys())
            {
                // Axis was locked, continue to allow further input
            }

            if (HandleKeyboardShortcutModeActivation())
            {
                // Mode was activated via keyboard, continue to allow axis selection
            }
            // ========== [END NEW] ==========

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

            // ========== [NEW] Reset keyboard state when disabled ==========
            ResetKeyboardTransformState();
            // ========== [END NEW] ==========
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
