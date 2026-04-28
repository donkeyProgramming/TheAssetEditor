using GameWorld.Core.Components.Rendering;
using Microsoft.Xna.Framework;

namespace GameWorld.Core.Components.Navigation
{
    public class CameraTransitionAnimation
    {
        private readonly ArcBallCamera _camera;

        // Transition state
        private bool _isTransitioning = false;
        private float _transitionProgress = 0f;
        private float _elapsedTime = 0f;

        // Transition duration in seconds
        private readonly float _transitionDuration = 0.25f;

        // Start and target states
        private float _startYaw, _startPitch, _startZoom;
        private float _targetYaw, _targetPitch, _targetZoom;
        private Vector3 _startLookAt, _targetLookAt;
        private ProjectionType _startProjection, _targetProjection;

        public bool IsTransitioning => _isTransitioning;

        public CameraTransitionAnimation(ArcBallCamera camera)
        {
            _camera = camera;
        }

        /// <summary>
        /// Start transition to a view preset
        /// </summary>
        public void StartTransition(ViewPresetType targetView, Vector3? customLookAt = null)
        {
            // Record start state
            _startYaw = _camera.Yaw;
            _startPitch = _camera.Pitch;
            _startZoom = _camera.Zoom;
            _startLookAt = _camera.LookAt;
            _startProjection = _camera.CurrentProjectionType;

            // Calculate target state
            if (targetView == ViewPresetType.Perspective)
            {
                // Return to perspective view
                _targetYaw = 0.8f;
                _targetPitch = 0.32f;
                _targetProjection = ProjectionType.Perspective;
            }
            else
            {
                var (yaw, pitch) = ViewPresets.GetViewAngles(targetView);
                _targetYaw = yaw;
                _targetPitch = pitch;
                _targetProjection = ProjectionType.Orthographic;
            }

            _targetLookAt = customLookAt ?? _startLookAt;
            _targetZoom = CalculateOrthoZoom();

            // Reset transition state
            _transitionProgress = 0f;
            _elapsedTime = 0f;
            _isTransitioning = true;
        }

        /// <summary>
        /// Calculate appropriate zoom for orthographic view
        /// </summary>
        private float CalculateOrthoZoom()
        {
            // Use current zoom as base, scale for orthographic view
            return _camera.Zoom;
        }

        /// <summary>
        /// Update transition each frame
        /// </summary>
        public void Update(GameTime gameTime)
        {
            if (!_isTransitioning)
                return;

            _elapsedTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
            _transitionProgress = Math.Min(1f, _elapsedTime / _transitionDuration);

            // Apply easing (SmoothStep: 3t² - 2t³)
            float t = _transitionProgress * _transitionProgress * (3f - 2f * _transitionProgress);

            // Interpolate camera parameters
            _camera.Yaw = LerpAngle(_startYaw, _targetYaw, t);
            _camera.Pitch = MathHelper.Lerp(_startPitch, _targetPitch, t);
            _camera.Zoom = MathHelper.Lerp(_startZoom, _targetZoom, t);
            _camera.LookAt = Vector3.Lerp(_startLookAt, _targetLookAt, t);

            // Update ortho size based on zoom
            _camera.OrthoSize = _camera.Zoom * 0.5f;

            // Switch projection type at midpoint
            if (_transitionProgress >= 0.5f && _camera.CurrentProjectionType != _targetProjection)
            {
                _camera.CurrentProjectionType = _targetProjection;
            }

            // Transition complete
            if (_transitionProgress >= 1f)
            {
                _isTransitioning = false;
                _camera.Yaw = _targetYaw;
                _camera.Pitch = _targetPitch;
                _camera.Zoom = _targetZoom;
                _camera.LookAt = _targetLookAt;
                _camera.CurrentProjectionType = _targetProjection;
                _camera.OrthoSize = _targetZoom * 0.5f;
            }
        }

        /// <summary>
        /// Angle interpolation (handles angle wrapping)
        /// </summary>
        private float LerpAngle(float start, float end, float t)
        {
            float diff = end - start;
            // Normalize to [-π, π]
            while (diff > MathHelper.Pi) diff -= MathHelper.TwoPi;
            while (diff < -MathHelper.Pi) diff += MathHelper.TwoPi;
            return start + diff * t;
        }

        /// <summary>
        /// Cancel current transition
        /// </summary>
        public void CancelTransition()
        {
            _isTransitioning = false;
        }
    }
}
