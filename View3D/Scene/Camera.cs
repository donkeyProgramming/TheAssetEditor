using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Text;

namespace View3D.Scene
{
    public class ArcBallCamera : BaseComponent
    {
        GraphicsDevice _graphicsDevice;
        Input.MouseComponent _mouse;
        Input.KeyboardComponent _keyboard;

        public ArcBallCamera(WpfGame game, Vector3 lookAt, float currentZoom) : base(game)
        {
            Zoom = currentZoom;
            _lookAt = lookAt;
        }

        public override void Initialize()
        {
            _graphicsDevice = Game.GraphicsDevice;
            _mouse = GetComponent<Input.MouseComponent>();
            _keyboard = GetComponent<Input.KeyboardComponent>();
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
            Vector3 right = Vector3.Normalize(LookAt - Position); //calculate forward
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
            Vector3 forward = Vector3.Normalize(LookAt - Position);
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

        bool IsMouseMoveKeyDown(MouseState state)
        {
            if (state.LeftButton == ButtonState.Pressed)    // Rotate
                return true;

            if (state.RightButton == ButtonState.Pressed)    // Pan
                return true;

            return false;
        }

        public override void Update(GameTime gameTime)
        {
            Update(_mouse, _keyboard);
        }

        public void Update(Input.MouseComponent mouse, Input.KeyboardComponent keyboard)
        {
            var deltaMouseX = mouse.DeltaPosition().X;
            var deltaMouseY = mouse.DeltaPosition().Y;
            var deltaMouseWheel = mouse.DeletaScrollWheel();

            if (keyboard.IsKeyReleased(Keys.F4))
            {
                Zoom = 10;
                _lookAt = Vector3.Zero;
            }

            if (keyboard.IsKeyDown(Keys.LeftAlt))
            {
                if (mouse.IsMouseButtonPressed(Input.MouseButton.Left))
                {
                    Yaw += deltaMouseX * 0.01f;
                    Pitch += deltaMouseY * 0.01f;
                }
                if (mouse.IsMouseButtonPressed(Input.MouseButton.Right))
                {
                    MoveCameraRight(deltaMouseX * 0.01f);
                    MoveCameraUp(-deltaMouseY * 0.01f);
                }
                else if (deltaMouseWheel != 0)
                {
                    Zoom += deltaMouseWheel * 0.005f;
                }
            }
        }


        Matrix RefreshProjection()
        {
            return Matrix.CreatePerspectiveFieldOfView(
                MathHelper.ToRadians(45), // 45 degree angle
                (float)_graphicsDevice.Viewport.Width /
                (float)_graphicsDevice.Viewport.Height,
                .01f, 150);
        }

        public Ray CreateCameraRay(Vector2 mouseLocation)
        {
            var projection = ProjectionMatrix;

            Vector3 nearPoint = _graphicsDevice.Viewport.Unproject(new Vector3(mouseLocation.X,
                   mouseLocation.Y, 0.0f),
                   projection,
                   ViewMatrix,
                   Matrix.Identity);

            Vector3 farPoint = _graphicsDevice.Viewport.Unproject(new Vector3(mouseLocation.X,
                    mouseLocation.Y, 1.0f),
                    projection,
                    ViewMatrix,
                    Matrix.Identity);

            Vector3 direction = farPoint - nearPoint;
            direction.Normalize();

            return new Ray(nearPoint, direction);
        }
    }
}
