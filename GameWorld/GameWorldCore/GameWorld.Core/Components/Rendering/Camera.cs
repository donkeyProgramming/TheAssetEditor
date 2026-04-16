using GameWorld.Core.Utility;
using Microsoft.Xna.Framework;

namespace GameWorld.Core.Components.Rendering
{
    public enum ProjectionType
    {
        Perspective,
        Orthographic
    }

    public class ArcBallCamera : BaseComponent, IDisposable
    {
        static readonly float s_minPitch = -MathHelper.PiOver2 + 0.3f;
        static readonly float s_maxPitch = MathHelper.PiOver2 - 0.3f;
        static readonly float s_minZoom = 0.01f;
        static readonly float s_maxZoom = float.MaxValue;

        IDeviceResolver _deviceResolver;


        private Matrix _viewMatrix;
        private Matrix _projectionMatrix;
        private bool _viewMatrixDirty = true;
        private bool _projectionMatrixDirty = true;
        private int _lastViewportWidth = 0;
        private int _lastViewportHeight = 0;

        public ArcBallCamera(IDeviceResolver deviceResolverComponent)
        {
            Zoom = 10;
            Yaw = 0.8f;
            Pitch = 0.32f;
            UpdateOrder = (int)ComponentUpdateOrderEnum.Camera;
            CurrentProjectionType = ProjectionType.Perspective;
            Position = new Vector3(0, 0, Zoom); 

            _deviceResolver = deviceResolverComponent;
        }

        public ProjectionType CurrentProjectionType
        {
            get => field;
            set
            {
                _projectionMatrixDirty = true;
                field = value;
            }
        }

        public float OrthoSize
        {
            get => field;
            set
            {
                _projectionMatrixDirty = true;
                field = Math.Max(0.1f, value);
            }
        }

        public float Pitch
        {
            get { return field; }
            set
            {
                _viewMatrixDirty = true;
                field = MathHelper.Clamp(value, s_minPitch, s_maxPitch);
            }
        }

        public float Yaw
        {
            get { return field; }
            set
            {
                _viewMatrixDirty = true;
                field = value;
            }
        }

        public float Zoom
        {
            get { return field; }
            set
            {
                _viewMatrixDirty = true;
                field = MathHelper.Clamp(value, s_minZoom, s_maxZoom);
            }
        }

        public Vector3 Position
        {
            get
            {
                if (_viewMatrixDirty)
                {
                    ReCreateViewMatrix();
                }
                return field;
            }
            set
            {
                _viewMatrixDirty = true;
                field = value; 
            }
        }

        public Vector3 LookAt
        {
            get { return field; }
            set
            {
                _viewMatrixDirty = true;
                field = value;
            }
        }

        public Matrix ViewMatrix
        {
            get
            {
                if (_viewMatrixDirty)
                {
                    ReCreateViewMatrix();
                }
                return _viewMatrix;
            }
        }

        public void SetDirtyProjectionMatrix() => _projectionMatrixDirty = true;

        public Matrix ProjectionMatrix
        {
            get
            {
                // Check if viewport size changed (happens when window/viewport is resized)
                if (_deviceResolver != null)
                {
                    var currentWidth = _deviceResolver.Device.Viewport.Width;
                    var currentHeight = _deviceResolver.Device.Viewport.Height;
                    if (currentWidth != _lastViewportWidth || currentHeight != _lastViewportHeight)
                    {
                        _lastViewportWidth = currentWidth;
                        _lastViewportHeight = currentHeight;
                        _projectionMatrixDirty = true;
                    }
                }

                if (_projectionMatrixDirty)
                {
                    _projectionMatrix = RefreshProjection();
                    _projectionMatrixDirty = false;
                }
                return _projectionMatrix;
            }
        }

        public void MoveCameraRight(float amount)
        {
            var right = Vector3.Normalize(LookAt - Position); //calculate forward
            right = Vector3.Cross(right, Vector3.Up); //calculate the real right
            right.Y = 0;
            right.Normalize();
            LookAt += right * amount;
        }

        public void MoveCameraUp(float amount)
        {
            LookAt = new Vector3(LookAt.X, LookAt.Y + amount, LookAt.Z);
            _viewMatrixDirty = true;
        }

        Matrix RefreshProjection()
        {
            var aspectRatio = _deviceResolver.Device.Viewport.Width / (float)_deviceResolver.Device.Viewport.Height;
            if (CurrentProjectionType == ProjectionType.Perspective)
            {
                return Matrix.CreatePerspectiveFieldOfView(
                    MathHelper.ToRadians(45), // 45 degree angle
                    aspectRatio,
                    .01f, 25000) * Matrix.CreateScale(-1, 1, 1);
            }
            else
            {
                return Matrix.CreateOrthographic(
                    OrthoSize * aspectRatio,  // width
                    OrthoSize,                 // height
                    0.01f,                      // near
                    25000f                      // far
                ) * Matrix.CreateScale(-1, 1, 1);
            }
        }

        void ReCreateViewMatrix()
        {
            var newPosition = Vector3.Transform(Vector3.Backward, Matrix.CreateFromYawPitchRoll(Yaw, Pitch, 0));
            newPosition *= Zoom;
            newPosition += LookAt;

            _viewMatrix = Matrix.CreateLookAt(newPosition, LookAt, Vector3.Up);
            Position = newPosition;
            _viewMatrixDirty = false;
        }

        public Ray CreateCameraRay(Vector2 mouseLocation)
        {
            var projection = ProjectionMatrix;

            var nearPoint = _deviceResolver.Device.Viewport.Unproject(new Vector3(mouseLocation.X,
                   mouseLocation.Y, 0.0f),
                   projection,
                   ViewMatrix,
                   Matrix.Identity);

            var farPoint = _deviceResolver.Device.Viewport.Unproject(new Vector3(mouseLocation.X,
                    mouseLocation.Y, 1.0f),
                    projection,
                    ViewMatrix,
                    Matrix.Identity);

            var direction = farPoint - nearPoint;
            direction.Normalize();

            return new Ray(nearPoint, direction);
        }

        public BoundingFrustum UnprojectRectangle(Rectangle source)
        {
            //http://forums.create.msdn.com/forums/p/6690/35401.aspx , by "The Friggm"
            // Many many thanks to him...

            // Point in screen space of the center of the region selected
            var regionCenterScreen = new Vector2(source.Center.X, source.Center.Y);

            // Generate the projection matrix for the screen region
            var regionProjMatrix = ProjectionMatrix;

            // Calculate the region dimensions in the projection matrix. M11 is inverse of width, M22 is inverse of height.
            regionProjMatrix.M11 /= source.Width / (float)_deviceResolver.Device.Viewport.Width;
            regionProjMatrix.M22 /= source.Height / (float)_deviceResolver.Device.Viewport.Height;

            // Calculate the region center in the projection matrix. M31 is horizonatal center.
            regionProjMatrix.M31 = (regionCenterScreen.X - _deviceResolver.Device.Viewport.Width / 2f) / (source.Width / 2f);

            // M32 is vertical center. Notice that the screen has low Y on top, projection has low Y on bottom.
            regionProjMatrix.M32 = -(regionCenterScreen.Y - _deviceResolver.Device.Viewport.Height / 2f) / (source.Height / 2f);

            return new BoundingFrustum(ViewMatrix * regionProjMatrix);
        }

        public void Dispose()
        {
            _deviceResolver = null;
        }
    }
}
