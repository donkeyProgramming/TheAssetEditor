using GameWorld.Core.Components.Input;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Shared.Core.Events;

namespace GameWorld.Core.Components.Navigation
{
    public class ViewportSelector : BaseComponent
    {
        private readonly ArcBallCamera _camera;
        private readonly IKeyboardComponent _keyboard;
        private readonly IMouseComponent _mouse;
        private readonly FocusSelectableObjectService _focusService;
        private readonly ViewportGizmo _navigationGizmo;
        private readonly IEventHub _eventHub;
        private readonly CameraTransitionAnimation _cameraTransition;

        // Orthographic view state
        private bool _isInOrthoView = false;
        private ViewPresetType _currentOrthoView;

        public ViewportSelector(
            ArcBallCamera camera,
            IKeyboardComponent keyboard,
            IMouseComponent mouse,
            FocusSelectableObjectService focusService,
            ViewportGizmo viewportGizmo,
            IEventHub eventHub)
        {
            _camera = camera;
            _keyboard = keyboard;
            _mouse = mouse;
            _focusService = focusService;
            _navigationGizmo = viewportGizmo;
            _eventHub = eventHub;
            _cameraTransition = new CameraTransitionAnimation(_camera);

            UpdateOrder = (int)ComponentUpdateOrderEnum.Camera;
            DrawOrder = (int)ComponentDrawOrderEnum.Default;

            _eventHub.Register<ChangeViewportEvent>(this, e => SwitchToView(e.Type));
        }

        public override void Update(GameTime gameTime)
        {
            _mouse.MouseOwner = null;

            // Update camera transition animation
            _cameraTransition.Update(gameTime);

            // If transitioning, skip other processing
            if (_cameraTransition.IsTransitioning)
                return;

            // Handle numpad shortcuts
            HandleNumpadShortcuts();

            // Update gizmo hover state
            _navigationGizmo.Update(gameTime);

            // Handle mouse click on navigation gizmo
            if (_mouse.IsMouseButtonPressed(MouseButton.Left))
            {
                Console.WriteLine("We are ere");
                if (_navigationGizmo.HandleClick(_mouse.Position()))
                {
                    _mouse.MouseOwner = this;
                    _mouse.ClearStates(); // Prevent clicks from affecting other components after interacting with the gizmo
                }
            }

            // In ortho view, exit ortho mode when middle mouse is pressed for rotation
            // Camera.cs handles the actual rotation and mouse ownership
            // We just need to detect the middle mouse press and exit ortho view
            if (_isInOrthoView && _mouse.IsMouseButtonPressed(MouseButton.Middle))
            {
                if (!_keyboard.IsKeyDown(Keys.LeftShift) && !_keyboard.IsKeyDown(Keys.RightShift))
                {
                    // Middle mouse without shift - exit ortho view to allow free rotation
                    ExitOrthoView();
                }
            }
        }

        private void HandleNumpadShortcuts()
        {
            // Numpad 1 - Front view / Ctrl+Numpad1 - Back view
            if (_keyboard.IsKeyReleased(Keys.NumPad1))
            {
                var view = IsCtrlDown()
                    ? ViewPresetType.Back
                    : ViewPresetType.Front;
                SwitchToView(view);
            }

            // Numpad 3 - Right view / Ctrl+Numpad3 - Left view
            if (_keyboard.IsKeyReleased(Keys.NumPad3))
            {
                var view = IsCtrlDown()
                    ? ViewPresetType.Left
                    : ViewPresetType.Right;
                SwitchToView(view);
            }

            // Numpad 7 - Top view / Ctrl+Numpad7 - Bottom view
            if (_keyboard.IsKeyReleased(Keys.NumPad7))
            {
                var view = IsCtrlDown()
                    ? ViewPresetType.Bottom
                    : ViewPresetType.Top;
                SwitchToView(view);
            }

            // Numpad 5 - Toggle perspective/orthographic
            if (_keyboard.IsKeyReleased(Keys.NumPad5))
            {
                ToggleProjectionType();
            }

            // Numpad . (Decimal) - Focus on selection (Blender style)
            if (_keyboard.IsKeyReleased(Keys.Decimal))
            {
                _focusService.FocusSelection();
            }
        }

        private bool IsCtrlDown() => _keyboard.IsKeyDown(Keys.LeftControl) || _keyboard.IsKeyDown(Keys.RightControl);

        private void SwitchToView(ViewPresetType view)
        {
            _currentOrthoView = view;
            _isInOrthoView = (view != ViewPresetType.Perspective);
            _cameraTransition.StartTransition(view, _camera.LookAt);
        }

        private void ExitOrthoView()
        {
            _isInOrthoView = false;
            _currentOrthoView = ViewPresetType.Perspective;
            _cameraTransition.StartTransition(ViewPresetType.Perspective);
        }

        private void ToggleProjectionType()
        {
            if (_isInOrthoView)
            {
                ExitOrthoView();
            }
            else
            {
                // Switch to the closest orthographic view
                var detected = ViewPresets.DetectViewPreset(_camera.Yaw, _camera.Pitch);
                var targetView = detected ?? ViewPresetType.Front;
                SwitchToView(targetView);
            }
        }
    }
}
