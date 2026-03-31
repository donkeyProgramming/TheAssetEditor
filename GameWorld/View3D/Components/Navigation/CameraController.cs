using GameWorld.Core.Components.Input;
using GameWorld.Core.Components.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Shared.Core.Settings;

namespace GameWorld.Core.Components.Navigation
{
    public class CameraController : BaseComponent
    {
        private readonly ArcBallCamera _arcBallCamera;
        private readonly IMouseComponent _mouseComponent;
        private readonly IKeyboardComponent _keyboardComponent;
        private readonly ApplicationSettingsService _applicationSettingsService;

        public CameraController(ArcBallCamera arcBallCamera, IMouseComponent mouseComponent, IKeyboardComponent keyboardComponent, ApplicationSettingsService applicationSettingsService)
        {
            _arcBallCamera = arcBallCamera;
            _mouseComponent = mouseComponent;
            _keyboardComponent = keyboardComponent;
            _applicationSettingsService = applicationSettingsService;
            UpdateOrder = (int)ComponentUpdateOrderEnum.Camera;
        }

        public override void Update(GameTime gameTime)
        {
            Update(_mouseComponent, _keyboardComponent);
        }

        public void Update(IMouseComponent mouse, IKeyboardComponent keyboard)
        {
            var deltaMouseX = -mouse.DeltaPosition().X;
            var deltaMouseY = mouse.DeltaPosition().Y;
            var deltaMouseWheel = mouse.DeletaScrollWheel();
            var controlMode = _applicationSettingsService.CurrentSettings.CameraControlMode;

            // Reset camera
            if (keyboard.IsKeyReleased(Keys.F4))
            {
                _arcBallCamera.Zoom = 10;
                _arcBallCamera.LookAt = Vector3.Zero;
            }

            // Zoom
            // Check for controll if not blender! 
            if (deltaMouseWheel != 0)
            {
                if (Math.Abs(deltaMouseWheel) > 250)   // Weird bug, sometimes this value is very large, probably related to state clearing. Temp fix
                    deltaMouseWheel = 250 * Math.Sign(deltaMouseWheel);

                // In orthographic mode, adjust OrthoSize instead of Zoom
                if (_arcBallCamera.CurrentProjectionType == ProjectionType.Orthographic)
                {
                    // Slower zoom in ortho mode for better control
                    _arcBallCamera.OrthoSize += deltaMouseWheel * 0.001f * _arcBallCamera.OrthoSize;
                    _arcBallCamera.OrthoSize = Math.Max(0.1f, _arcBallCamera.OrthoSize);  // Prevent zero or negative
                    _arcBallCamera.SetDirtyProjectionMatrix();
                }
                else
                {
                    var oldZoom = _arcBallCamera.Zoom / 10;
                    _arcBallCamera.Zoom += deltaMouseWheel * 0.005f * oldZoom;
                }
            }

            // Check for middle mouse button (Blender-style navigation)
            // Middle mouse navigation has priority and can take ownership from other components
            var isMiddleMouseDown = mouse.IsMouseButtonDown(MouseButton.Middle);
            var isShiftDown = keyboard.IsKeyDown(Keys.LeftShift);

            if (controlMode == CameraControlMode.BlenderStyle)
            {
                // Blender-style: Middle mouse button navigation (no Alt required)
                if (isMiddleMouseDown)
                {
                    // Take ownership for camera navigation (overrides other components)
                    mouse.MouseOwner = this;

                    if (isShiftDown)
                    {
                        // Shift + Middle mouse = Pan view
                        _arcBallCamera.MoveCameraRight(deltaMouseX * 0.01f * _arcBallCamera.Zoom * .1f);
                        _arcBallCamera.MoveCameraUp(-deltaMouseY * 0.01f * _arcBallCamera.Zoom * .1f);
                    }
                    else
                    {
                        // Middle mouse only = Rotate view
                        _arcBallCamera.Yaw += deltaMouseX * 0.01f;
                        _arcBallCamera.Pitch += deltaMouseY * 0.01f;

                        _arcBallCamera.CurrentProjectionType = ProjectionType.Perspective;
                    }
                }
            }
            else
            {
                // Check mouse ownership for other operations
                if (!mouse.IsMouseOwner(this) && mouse.MouseOwner != null)
                    return;

                // Original Alt+Left/Right mouse navigation (kept for compatibility)
                var ownsMouse = mouse.MouseOwner;
                if (keyboard.IsKeyDown(Keys.LeftAlt))
                {
                    mouse.MouseOwner = this;
                }
                else
                {
                    // Only release mouse ownership if middle mouse is not pressed
                    if (ownsMouse == this && !isMiddleMouseDown)
                    {
                        mouse.MouseOwner = null;
                        mouse.ClearStates();
                        return;
                    }
                }

                if (keyboard.IsKeyDown(Keys.LeftAlt))
                {
                    mouse.MouseOwner = this;
                    if (mouse.IsMouseButtonDown(MouseButton.Left))
                    {
                        _arcBallCamera.Yaw += deltaMouseX * 0.01f;
                        _arcBallCamera.Pitch += deltaMouseY * 0.01f;

                        _arcBallCamera.CurrentProjectionType = ProjectionType.Perspective;
                    }
                    if (mouse.IsMouseButtonDown(MouseButton.Right))
                    {
                        _arcBallCamera.MoveCameraRight(deltaMouseX * 0.01f * _arcBallCamera.Zoom * .1f);
                        _arcBallCamera.MoveCameraUp(-deltaMouseY * 0.01f * _arcBallCamera.Zoom * .1f);
                    }
                }
            }
        }


    }
}
