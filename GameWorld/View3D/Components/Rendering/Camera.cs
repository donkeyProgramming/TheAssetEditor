using GameWorld.Core.Components.Input;
using GameWorld.Core.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace GameWorld.Core.Components.Rendering
{
    public class ArcBallCamera : BaseComponent, IDisposable
    {
        GraphicsDevice _graphicsDevice;
        private readonly IMouseComponent _mouse;
        private readonly IKeyboardComponent _keyboard;

        public ArcBallCamera(IDeviceResolver deviceResolverComponent, IKeyboardComponent keyboardComponent, IMouseComponent mouseComponent)
        {
            Zoom = 10;
            Yaw = 0.8f;
            Pitch = 0.32f;
            UpdateOrder = (int)ComponentUpdateOrderEnum.Camera;

            _deviceResolverComponent = deviceResolverComponent;
            _mouse = mouseComponent;
            _keyboard = keyboardComponent;
        }

        public override void Initialize()
        {
            _graphicsDevice = _deviceResolverComponent.Device;
            base.Initialize();
        }

        /// <summary>
        /// Recreates our view matrix, then signals that the view matrix
        /// is clean.
        /// </summary>
        private void ReCreateViewMatrix()
        {
            //Calculate the relative position of the camera                        
            position = Vector3.Transform(Vector3.Backward, Matrix.CreateFromYawPitchRoll(yaw, pitch, 0));
            //Convert the relative position to the absolute position
            position *= _zoom;
            position += _lookAt;

            //Calculate a new viewmatrix
            viewMatrix = Matrix.CreateLookAt(position, _lookAt, Vector3.Up);
            viewMatrixDirty = false;
        }


        #region HelperMethods

        /// <summary>
        /// Moves the camera and lookAt at to the right,
        /// as seen from the camera, while keeping the same height
        /// </summary>        
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
            _lookAt.Y += amount;
            viewMatrixDirty = true;
        }

        /// <summary>
        /// Moves the camera and lookAt forward,
        /// as seen from the camera, while keeping the same height
        /// </summary>        
        public void MoveCameraForward(float amount)
        {
            var forward = Vector3.Normalize(LookAt - Position);
            forward.Y = 0;
            forward.Normalize();
            LookAt += forward * amount;
        }

        #endregion

        #region FieldsAndProperties
        //We don't need an update method because the camera only needs updating
        //when we change one of it's parameters.
        //We keep track if one of our matrices is dirty
        //and reacalculate that matrix when it is accesed.
        private bool viewMatrixDirty = true;

        public float MinPitch = -MathHelper.PiOver2 + 0.3f;
        public float MaxPitch = MathHelper.PiOver2 - 0.3f;
        private float pitch;
        public float Pitch
        {
            get { return pitch; }
            set
            {
                viewMatrixDirty = true;
                pitch = MathHelper.Clamp(value, MinPitch, MaxPitch);
            }
        }

        private float yaw;
        public float Yaw
        {
            get { return yaw; }
            set
            {
                viewMatrixDirty = true;
                yaw = value;
            }
        }

        public static float MinZoom = 0.01f;
        public static float MaxZoom = float.MaxValue;
        private float _zoom = 1;
        public float Zoom
        {
            get { return _zoom; }
            set
            {
                viewMatrixDirty = true;
                _zoom = MathHelper.Clamp(value, MinZoom, MaxZoom);
            }
        }


        private Vector3 position;
        public Vector3 Position
        {
            get
            {
                if (viewMatrixDirty)
                {
                    ReCreateViewMatrix();
                }
                return position;
            }
        }

        private Vector3 _lookAt;
        public Vector3 LookAt
        {
            get { return _lookAt; }
            set
            {
                viewMatrixDirty = true;
                _lookAt = value;
            }
        }
        #endregion

        #region ICamera Members        

        private Matrix viewMatrix;
        private readonly IDeviceResolver _deviceResolverComponent;

        public Matrix ViewMatrix
        {
            get
            {
                if (viewMatrixDirty)
                {
                    ReCreateViewMatrix();
                }
                return viewMatrix;
            }
        }

        public Matrix ProjectionMatrix
        {
            get
            {
                return RefreshProjection();
            }
        }
        #endregion

        public override void Update(GameTime gameTime)
        {
            Update(_mouse, _keyboard);
        }

        public void Update(IMouseComponent mouse, IKeyboardComponent keyboard)
        {
            if (!mouse.IsMouseOwner(this))
                return;

            var deltaMouseX = -mouse.DeltaPosition().X;
            var deltaMouseY = mouse.DeltaPosition().Y;
            var deltaMouseWheel = mouse.DeletaScrollWheel();

            if (keyboard.IsKeyReleased(Keys.F4))
            {
                Zoom = 10;
                _lookAt = Vector3.Zero;
            }

            var ownsMouse = mouse.MouseOwner;
            if (keyboard.IsKeyDown(Keys.LeftAlt))
            {
                mouse.MouseOwner = this;
            }
            else
            {
                if (ownsMouse == this)
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
                    Yaw += deltaMouseX * 0.01f;
                    Pitch += deltaMouseY * 0.01f;
                }
                if (mouse.IsMouseButtonDown(MouseButton.Right))
                {
                    MoveCameraRight(deltaMouseX * 0.01f * Zoom * .1f);
                    MoveCameraUp(-deltaMouseY * 0.01f * Zoom * .1f);
                }
                else if (deltaMouseWheel != 0)
                {
                    if (Math.Abs(deltaMouseWheel) > 250)   // Weird bug, sometimes this value is very large, probably related to state clearing. Temp fix
                        deltaMouseWheel = 250 * Math.Sign(deltaMouseWheel);

                    var oldZoom = Zoom / 10;
                    Zoom += deltaMouseWheel * 0.005f * oldZoom;
                    //_logger.Here().Information($"Setting zoom {Zoom} - {deltaMouseWheel} - {oldZoom}");
                }
            }
        }


        Matrix RefreshProjection()
        {
            return Matrix.CreatePerspectiveFieldOfView(
                MathHelper.ToRadians(45), // 45 degree angle
                _graphicsDevice.Viewport.Width /
                (float)_graphicsDevice.Viewport.Height,
                .01f, 25000) * Matrix.CreateScale(-1, 1, 1);
        }

        public Ray CreateCameraRay(Vector2 mouseLocation)
        {
            var projection = ProjectionMatrix;

            var nearPoint = _graphicsDevice.Viewport.Unproject(new Vector3(mouseLocation.X,
                   mouseLocation.Y, 0.0f),
                   projection,
                   ViewMatrix,
                   Matrix.Identity);

            var farPoint = _graphicsDevice.Viewport.Unproject(new Vector3(mouseLocation.X,
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
            regionProjMatrix.M11 /= source.Width / (float)_graphicsDevice.Viewport.Width;
            regionProjMatrix.M22 /= source.Height / (float)_graphicsDevice.Viewport.Height;

            // Calculate the region center in the projection matrix. M31 is horizonatal center.
            regionProjMatrix.M31 = (regionCenterScreen.X - _graphicsDevice.Viewport.Width / 2f) / (source.Width / 2f);

            // M32 is vertical center. Notice that the screen has low Y on top, projection has low Y on bottom.
            regionProjMatrix.M32 = -(regionCenterScreen.Y - _graphicsDevice.Viewport.Height / 2f) / (source.Height / 2f);

            return new BoundingFrustum(ViewMatrix * regionProjMatrix);
        }

        public void Dispose()
        {
            _graphicsDevice = null;
        }
    }
}
