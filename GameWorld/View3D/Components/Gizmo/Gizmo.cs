using GameWorld.Core.Components.Input;
using GameWorld.Core.Components.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

// -------------------------------------------------------------
// -- XNA 3D Gizmo (Component)
// -------------------------------------------------------------
// -- open-source gizmo component for any 3D level editor.
// -- contains any feature you may be looking for in a transformation gizmo.
// -- 
// -- for additional information and instructions visit codeplex.
// --
// -- codeplex url: http://xnagizmo.codeplex.com/
// --
// -----------------Please Do Not Remove ----------------------
// -- Work by Tom Looman, licensed under Ms-PL
// -- My Blog: http://coreenginedev.blogspot.com
// -- My Portfolio: http://tomlooman.com
// -- You may find additional XNA resources and information on these sites.
// ------------------------------------------------------------

namespace GameWorld.Core.Components.Gizmo
{
    public class Gizmo : IDisposable
    {
        /// <summary>
        /// only active if atleast one entity is selected.
        /// </summary>
        private bool _isActive = true;

        /// <summary>
        /// Enabled if gizmo should be able to select objects and axis.
        /// </summary>
        public bool Enabled { get; set; }

        private readonly GraphicsDevice _graphics;
        private readonly RenderEngineComponent _renderEngineComponent;

        private readonly BasicEffect _lineEffect;
        private readonly BasicEffect _meshEffect;


        // -- Screen Scale -- //
        private float _screenScale;
        public float ScaleModifier { get; set; } = 1;

        // -- Position - Rotation -- //
        private Vector3 _position = Vector3.Zero;
        private Matrix _rotationMatrix = Matrix.Identity;

        public Matrix AxisMatrix
        {
            get { return _rotationMatrix; }
        }

        private Vector3 _localForward = Vector3.Forward;
        private Vector3 _localUp = Vector3.Up;
        private Vector3 _localRight;

        // -- Matrices -- //
        private Matrix _objectOrientedWorld;
        private Matrix _axisAlignedWorld;
        private Matrix[] _modelLocalSpace;

        // used for all drawing, assigned by local- or world-space matrices
        private Matrix _gizmoWorld = Matrix.Identity;

        // the matrix used to apply to your whole scene, usually matrix.identity (default scale, origin on 0,0,0 etc.)
        public Matrix SceneWorld;

        // -- Lines (Vertices) -- //
        private VertexPositionColor[] _translationLineVertices;
        private const float LINE_LENGTH = 3f;
        private const float LINE_OFFSET = 1f;

        // -- Colors -- //
        private Color[] _axisColors = new Color[3] { Color.Red, Color.Green, Color.Blue };
        private Color _highlightColor = Color.Gold;

        // -- UI Text -- //
        private string[] _axisText = new string[3] { "X", "Y", "Z" };
        private Vector3 _axisTextOffset = new Vector3(0, 0.5f, 0);

        // -- Modes & Selections -- //
        public GizmoAxis ActiveAxis = GizmoAxis.None;
        public GizmoMode ActiveMode = GizmoMode.Translate;
        public TransformSpace GizmoDisplaySpace = TransformSpace.World;
        public TransformSpace GizmoValueSpace = TransformSpace.Local;
        public PivotType ActivePivot = PivotType.SelectionCenter;


        #region BoundingSpheres

        private const float RADIUS = 1f;

        private BoundingSphere XSphere
        {
            get
            {
                return new BoundingSphere(Vector3.Transform(_translationLineVertices[1].Position, _gizmoWorld), RADIUS * _screenScale * ScaleModifier);
            }
        }

        private BoundingSphere YSphere
        {
            get
            {
                return new BoundingSphere(Vector3.Transform(_translationLineVertices[7].Position, _gizmoWorld), RADIUS * _screenScale * ScaleModifier);
            }
        }

        private BoundingSphere ZSphere
        {
            get
            {
                return new BoundingSphere(Vector3.Transform(_translationLineVertices[13].Position, _gizmoWorld), RADIUS * _screenScale * ScaleModifier);
            }
        }

        #endregion



        // -- Selection -- //
        public List<ITransformable> Selection = new List<ITransformable>();


        // -- Translation Variables -- //
        private Vector3 _lastIntersectionPosition;
        private Vector3 _intersectPosition;

        public bool SnapEnabled = false;
        public float RotationSnapValue = 30;
        private float _rotationSnapDelta;


        private readonly ArcBallCamera _camera;
        private readonly IMouseComponent _mouse;


        public Gizmo(ArcBallCamera camera, IMouseComponent mouse, GraphicsDevice graphics, RenderEngineComponent renderEngineComponent)
        {
            SceneWorld = Matrix.Identity;
            _graphics = graphics;
            _renderEngineComponent = renderEngineComponent;

            _camera = camera;
            _mouse = mouse;

            Enabled = true;

            _lineEffect = new BasicEffect(graphics) { VertexColorEnabled = true, AmbientLightColor = Vector3.One, EmissiveColor = Vector3.One };
            _meshEffect = new BasicEffect(graphics);

            Initialize();
        }

        private void Initialize()
        {
            // -- Set local-space offset -- //
            _modelLocalSpace = new Matrix[3];
            _modelLocalSpace[0] = Matrix.CreateWorld(new Vector3(LINE_LENGTH, 0, 0), Vector3.Left, Vector3.Up);
            _modelLocalSpace[1] = Matrix.CreateWorld(new Vector3(0, LINE_LENGTH, 0), Vector3.Down, Vector3.Left);
            _modelLocalSpace[2] = Matrix.CreateWorld(new Vector3(0, 0, LINE_LENGTH), Vector3.Forward, Vector3.Up);

            const float halfLineOffset = LINE_OFFSET / 2;


            // fill array with vertex-data
            var vertexList = new List<VertexPositionColor>(18);

            // helper to apply colors
            var xColor = _axisColors[0];
            var yColor = _axisColors[1];
            var zColor = _axisColors[2];


            // -- X Axis -- // index 0 - 5
            vertexList.Add(new VertexPositionColor(new Vector3(halfLineOffset, 0, 0), xColor));
            vertexList.Add(new VertexPositionColor(new Vector3(LINE_LENGTH, 0, 0), xColor));

            vertexList.Add(new VertexPositionColor(new Vector3(LINE_OFFSET, 0, 0), xColor));
            vertexList.Add(new VertexPositionColor(new Vector3(LINE_OFFSET, LINE_OFFSET, 0), xColor));

            vertexList.Add(new VertexPositionColor(new Vector3(LINE_OFFSET, 0, 0), xColor));
            vertexList.Add(new VertexPositionColor(new Vector3(LINE_OFFSET, 0, LINE_OFFSET), xColor));

            // -- Y Axis -- // index 6 - 11
            vertexList.Add(new VertexPositionColor(new Vector3(0, halfLineOffset, 0), yColor));
            vertexList.Add(new VertexPositionColor(new Vector3(0, LINE_LENGTH, 0), yColor));

            vertexList.Add(new VertexPositionColor(new Vector3(0, LINE_OFFSET, 0), yColor));
            vertexList.Add(new VertexPositionColor(new Vector3(LINE_OFFSET, LINE_OFFSET, 0), yColor));

            vertexList.Add(new VertexPositionColor(new Vector3(0, LINE_OFFSET, 0), yColor));
            vertexList.Add(new VertexPositionColor(new Vector3(0, LINE_OFFSET, LINE_OFFSET), yColor));

            // -- Z Axis -- // index 12 - 17
            vertexList.Add(new VertexPositionColor(new Vector3(0, 0, halfLineOffset), zColor));
            vertexList.Add(new VertexPositionColor(new Vector3(0, 0, LINE_LENGTH), zColor));

            vertexList.Add(new VertexPositionColor(new Vector3(0, 0, LINE_OFFSET), zColor));
            vertexList.Add(new VertexPositionColor(new Vector3(LINE_OFFSET, 0, LINE_OFFSET), zColor));

            vertexList.Add(new VertexPositionColor(new Vector3(0, 0, LINE_OFFSET), zColor));
            vertexList.Add(new VertexPositionColor(new Vector3(0, LINE_OFFSET, LINE_OFFSET), zColor));

            // -- Convert to array -- //
            _translationLineVertices = vertexList.ToArray();
        }

        public void ResetDeltas()
        {
            _lastIntersectionPosition = Vector3.Zero;
            _intersectPosition = Vector3.Zero;
        }

        public void Update(GameTime gameTime, bool enableMove)
        {
            if (_isActive && enableMove)
            {
                var translateScaleLocal = Vector3.Zero;
                var translateScaleWorld = Vector3.Zero;

                var rotationLocal = Matrix.Identity;
                var rotationWorld = Matrix.Identity;

                if (_mouse.IsMouseButtonDown(MouseButton.Left) && ActiveAxis != GizmoAxis.None)
                {
                    if (_mouse.LastState().LeftButton == ButtonState.Released)
                        StartEvent?.Invoke();

                    switch (ActiveMode)
                    {
                        case GizmoMode.UniformScale:
                        case GizmoMode.NonUniformScale:
                        case GizmoMode.Translate:
                            HandleTranslateAndScale(_mouse.Position(), out translateScaleLocal, out translateScaleWorld);
                            break;
                        case GizmoMode.Rotate:
                            HandleRotation(gameTime, out rotationLocal, out rotationWorld);
                            break;
                    }
                }
                else
                {
                    if (_mouse.LastState().LeftButton == ButtonState.Pressed && _mouse.State().LeftButton == ButtonState.Released)
                        StopEvent?.Invoke();

                    ResetDeltas();
                    if (_mouse.State().LeftButton == ButtonState.Released && _mouse.State().RightButton == ButtonState.Released)
                        SelectAxis(_mouse.Position());
                }

                UpdateGizmoPosition();

                // -- Trigger Translation, Rotation & Scale events -- //
                if (_mouse.IsMouseButtonDown(MouseButton.Left))
                {
                    if (translateScaleWorld != Vector3.Zero)
                    {
                        if (ActiveMode == GizmoMode.Translate)
                        {
                            foreach (var entity in Selection)
                                OnTranslateEvent(entity, translateScaleWorld);
                        }
                        else
                        {
                            foreach (var entity in Selection)
                                OnScaleEvent(entity, translateScaleWorld);
                        }
                    }
                    if (rotationWorld != Matrix.Identity)
                    {
                        foreach (var entity in Selection)
                            OnRotateEvent(entity, rotationWorld);
                    }
                }
            }

            if (Selection.Count == 0)
            {
                _isActive = false;
                ActiveAxis = GizmoAxis.None;
                return;
            }

            // helps solve visual lag (1-frame-lag) after selecting a new entity
            if (!_isActive)
                UpdateGizmoPosition();

            _isActive = true;

            // -- Scale Gizmo to fit on-screen -- //
            var vLength = _camera.Position - _position;
            const float scaleFactor = 25;

            _screenScale = vLength.Length() / scaleFactor;
            var screenScaleMatrix = Matrix.CreateScale(new Vector3(_screenScale * ScaleModifier));

            _localForward = Vector3.Transform(Vector3.Forward, Matrix.CreateFromQuaternion(Selection[0].Orientation)); //Selection[0].Forward;
            _localUp = Vector3.Transform(Vector3.Up, Matrix.CreateFromQuaternion(Selection[0].Orientation));  //Selection[0].Up;

            // -- Vector Rotation (Local/World) -- //
            _localForward.Normalize();
            _localRight = Vector3.Cross(_localForward, _localUp);
            _localUp = Vector3.Cross(_localRight, _localForward);
            _localRight.Normalize();
            _localUp.Normalize();

            // -- Create Both World Matrices -- //
            _objectOrientedWorld = screenScaleMatrix * Matrix.CreateWorld(_position, _localForward, _localUp);
            _axisAlignedWorld = screenScaleMatrix * Matrix.CreateWorld(_position, SceneWorld.Forward, SceneWorld.Up);

            // Assign World
            if (GizmoDisplaySpace == TransformSpace.World ||
                //ActiveMode == GizmoMode.Rotate ||
                //ActiveMode == GizmoMode.NonUniformScale ||
                ActiveMode == GizmoMode.UniformScale)
            {
                _gizmoWorld = _axisAlignedWorld;

                // align lines, boxes etc. with the grid-lines
                _rotationMatrix.Forward = SceneWorld.Forward;
                _rotationMatrix.Up = SceneWorld.Up;
                _rotationMatrix.Right = SceneWorld.Right;
            }
            else
            {
                _gizmoWorld = _objectOrientedWorld;

                // align lines, boxes etc. with the selected object
                _rotationMatrix.Forward = _localForward;
                _rotationMatrix.Up = _localUp;
                _rotationMatrix.Right = _localRight;
            }

            // -- Reset Colors to default -- //
            ApplyColor(GizmoAxis.X, _axisColors[0]);
            ApplyColor(GizmoAxis.Y, _axisColors[1]);
            ApplyColor(GizmoAxis.Z, _axisColors[2]);

            // -- Apply Highlight -- //
            ApplyColor(ActiveAxis, _highlightColor);
        }

        private void HandleTranslateAndScale(Vector2 mousePosition, out Vector3 out_transformLocal, out Vector3 out_transfromWorld)
        {
            Plane plane;
            switch (ActiveAxis)
            {
                case GizmoAxis.X:
                    plane = new Plane(Vector3.Forward, Vector3.Transform(_position, Matrix.Invert(_rotationMatrix)).Z);
                    break;
                case GizmoAxis.Z:
                case GizmoAxis.Y:
                    plane = new Plane(Vector3.Left, Vector3.Transform(_position, Matrix.Invert(_rotationMatrix)).X);
                    break;
                default:
                    throw new Exception("This should never happen - No axis inside HandleTranslateAndScale");
            }


            var ray = _camera.CreateCameraRay(mousePosition);
            var transform = Matrix.Invert(_rotationMatrix);
            ray.Position = Vector3.Transform(ray.Position, transform);
            ray.Direction = Vector3.TransformNormal(ray.Direction, transform);

            var deltaTransform = Vector3.Zero;
            var intersection = ray.Intersects(plane);
            if (intersection.HasValue)
            {
                _intersectPosition = ray.Position + ray.Direction * intersection.Value;
                var mouseDragDelta = Vector3.Zero;
                if (_lastIntersectionPosition != Vector3.Zero)
                    mouseDragDelta = _intersectPosition - _lastIntersectionPosition;

                var length = mouseDragDelta.Length();
                if (length > 0.5f)
                {
                    var direction = Vector3.Normalize(mouseDragDelta);
                    mouseDragDelta = direction * 0.5f;
                }
                switch (ActiveAxis)
                {
                    case GizmoAxis.X:
                        deltaTransform = new Vector3(mouseDragDelta.X, 0, 0);
                        break;
                    case GizmoAxis.Y:
                        deltaTransform = new Vector3(0, mouseDragDelta.Y, 0);
                        break;
                    case GizmoAxis.Z:
                        deltaTransform = new Vector3(0, 0, mouseDragDelta.Z);
                        break;
                }

                _lastIntersectionPosition = _intersectPosition;
            }

            if (ActiveMode == GizmoMode.Translate)
            {
                out_transformLocal = Vector3.Transform(deltaTransform, SceneWorld);  // local;
                out_transfromWorld = Vector3.Transform(deltaTransform, _rotationMatrix);  // World;
            }
            else if (ActiveMode == GizmoMode.NonUniformScale || ActiveMode == GizmoMode.UniformScale)
            {
                out_transformLocal = deltaTransform;
                out_transfromWorld = deltaTransform;
            }
            else
            {
                throw new Exception("This should never happen - Not scale or translate inside HandleTranslateAndScale");
            }
        }

        private void HandleRotation(GameTime gameTime, out Matrix out_transformLocal, out Matrix out_transfromWorld)
        {
            var delta = _mouse.DeltaPosition().X * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (SnapEnabled)
            {
                var snapValue = MathHelper.ToRadians(RotationSnapValue);
                _rotationSnapDelta += delta;
                var snapped = (int)(_rotationSnapDelta / snapValue) * snapValue;
                _rotationSnapDelta -= snapped;
                delta = snapped;
            }

            // rotation matrix to transform - if more than one objects selected, always use world-space.
            var rot = Matrix.Identity;
            rot.Forward = SceneWorld.Forward;
            rot.Up = SceneWorld.Up;
            rot.Right = SceneWorld.Right;

            var rotationMatrixLocal = Matrix.Identity;
            rotationMatrixLocal.Forward = SceneWorld.Forward;
            rotationMatrixLocal.Up = SceneWorld.Up;
            rotationMatrixLocal.Right = SceneWorld.Right;

            switch (ActiveAxis)
            {
                case GizmoAxis.X:
                    rot *= Matrix.CreateFromAxisAngle(_rotationMatrix.Right, delta);
                    rotationMatrixLocal *= Matrix.CreateFromAxisAngle(SceneWorld.Right, delta);
                    break;
                case GizmoAxis.Y:
                    rot *= Matrix.CreateFromAxisAngle(_rotationMatrix.Up, delta);
                    rotationMatrixLocal *= Matrix.CreateFromAxisAngle(SceneWorld.Up, delta);
                    break;
                case GizmoAxis.Z:
                    rot *= Matrix.CreateFromAxisAngle(_rotationMatrix.Forward, delta);
                    rotationMatrixLocal *= Matrix.CreateFromAxisAngle(SceneWorld.Forward, delta);
                    break;
            }

            out_transformLocal = rotationMatrixLocal;
            out_transfromWorld = rot;
        }


        /// <summary>
        /// Helper method for applying color to the gizmo lines.
        /// </summary>
        private void ApplyColor(GizmoAxis axis, Color color)
        {
            switch (ActiveMode)
            {
                case GizmoMode.NonUniformScale:
                case GizmoMode.Translate:
                    switch (axis)
                    {
                        case GizmoAxis.X:
                            ApplyLineColor(0, 6, color);
                            break;
                        case GizmoAxis.Y:
                            ApplyLineColor(6, 6, color);
                            break;
                        case GizmoAxis.Z:
                            ApplyLineColor(12, 6, color);
                            break;
                    }
                    break;
                case GizmoMode.Rotate:
                    switch (axis)
                    {
                        case GizmoAxis.X:
                            ApplyLineColor(0, 6, color);
                            break;
                        case GizmoAxis.Y:
                            ApplyLineColor(6, 6, color);
                            break;
                        case GizmoAxis.Z:
                            ApplyLineColor(12, 6, color);
                            break;
                    }
                    break;
                case GizmoMode.UniformScale:
                    ApplyLineColor(0, _translationLineVertices.Length,
                                   ActiveAxis == GizmoAxis.None ? _axisColors[0] : _highlightColor);
                    break;
            }
        }

        private void ApplyLineColor(int startindex, int count, Color color)
        {
            for (var i = startindex; i < startindex + count; i++)
                _translationLineVertices[i].Color = color;
        }

        /// <summary>
        /// Per-frame check to see if mouse is hovering over any axis.
        /// </summary>
        private void SelectAxis(Vector2 mousePosition)
        {
            if (!Enabled)
                return;

            var closestintersection = float.MaxValue;
            var ray = _camera.CreateCameraRay(mousePosition);

            var intersection = XSphere.Intersects(ray);
            if (intersection.HasValue)
                if (intersection.Value < closestintersection)
                {
                    ActiveAxis = GizmoAxis.X;
                    closestintersection = intersection.Value;
                }
            intersection = YSphere.Intersects(ray);
            if (intersection.HasValue)
                if (intersection.Value < closestintersection)
                {
                    ActiveAxis = GizmoAxis.Y;
                    closestintersection = intersection.Value;
                }
            intersection = ZSphere.Intersects(ray);
            if (intersection.HasValue)
                if (intersection.Value < closestintersection)
                {
                    ActiveAxis = GizmoAxis.Z;
                    closestintersection = intersection.Value;
                }

            if (closestintersection >= float.MaxValue || closestintersection <= float.MinValue)
                ActiveAxis = GizmoAxis.None;
        }


        /// <summary>
        /// Set position of the gizmo, position will be center of all selected entities.
        /// </summary>
        private void UpdateGizmoPosition()
        {
            switch (ActivePivot)
            {
                case PivotType.ObjectCenter:
                    if (Selection.Count > 0)
                        _position = Selection[0].GetObjectCentre();
                    break;
                case PivotType.SelectionCenter:
                    _position = GetSelectionCenter();
                    break;
                case PivotType.WorldOrigin:
                    _position = SceneWorld.Translation;
                    break;
            }
        }

        /// <summary>
        /// Returns center position of all selected objectes.
        /// </summary>
        /// <returns></returns>
        private Vector3 GetSelectionCenter()
        {
            if (Selection.Count == 0)
                return Vector3.Zero;

            var center = Vector3.Zero;
            foreach (var selected in Selection)
                center += selected.Position;
            return center / Selection.Count;
        }

        #region Draw
        public void Draw()
        {
            if (!_isActive)
                return;

            _graphics.BlendState = BlendState.AlphaBlend;
            _graphics.DepthStencilState = DepthStencilState.None;
            _graphics.RasterizerState = RasterizerState.CullNone;

            var view = _camera.ViewMatrix;
            var projection = _camera.ProjectionMatrix;

            // -- Draw Lines -- //
            _lineEffect.World = _gizmoWorld;
            _lineEffect.View = view;
            _lineEffect.Projection = projection;

            _lineEffect.CurrentTechnique.Passes[0].Apply();
            _graphics.DrawUserPrimitives(PrimitiveType.LineList, _translationLineVertices, 0, _translationLineVertices.Length / 2);


            // draw the 3d meshes
            for (var i = 0; i < 3; i++) //(order: x, y, z)
            {
                GizmoModel activeModel;
                switch (ActiveMode)
                {
                    case GizmoMode.Translate:
                        activeModel = Geometry.Translate;
                        break;
                    case GizmoMode.Rotate:
                        activeModel = Geometry.Rotate;
                        break;
                    default:
                        activeModel = Geometry.Scale;
                        break;
                }

                Vector3 color;
                switch (ActiveMode)
                {
                    case GizmoMode.UniformScale:
                        color = _axisColors[0].ToVector3();
                        break;
                    default:
                        color = _axisColors[i].ToVector3();
                        break;
                }

                _meshEffect.World = _modelLocalSpace[i] * _gizmoWorld;
                _meshEffect.View = view;
                _meshEffect.Projection = projection;

                _meshEffect.DiffuseColor = color;
                _meshEffect.EmissiveColor = color;

                _meshEffect.CurrentTechnique.Passes[0].Apply();

                _graphics.DrawUserIndexedPrimitives(PrimitiveType.TriangleList,
                    activeModel.Vertices, 0, activeModel.Vertices.Length,
                    activeModel.Indices, 0, activeModel.Indices.Length / 3);
            }

            _graphics.DepthStencilState = DepthStencilState.Default;
           
            Draw2D(view, projection);
        }

        private void Draw2D(Matrix view, Matrix projection)
        {
            _renderEngineComponent.CommonSpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            // -- Draw Axis identifiers ("X,Y,Z") -- // 
            for (var i = 0; i < 3; i++)
            {
                var screenPos =
                  _graphics.Viewport.Project(_modelLocalSpace[i].Translation + _modelLocalSpace[i].Backward + _axisTextOffset,
                                             projection, view, _gizmoWorld);

                if (screenPos.Z < 0f || screenPos.Z > 1.0f)
                    continue;

                var color = _axisColors[i];
                switch (i)
                {
                    case 0:
                        if (ActiveAxis == GizmoAxis.X)
                            color = _highlightColor;
                        break;
                    case 1:
                        if (ActiveAxis == GizmoAxis.Y)
                            color = _highlightColor;
                        break;
                    case 2:
                        if (ActiveAxis == GizmoAxis.Z)
                            color = _highlightColor;
                        break;
                }

                _renderEngineComponent.CommonSpriteBatch.DrawString(_renderEngineComponent.DefaultFont, _axisText[i], new Vector2(screenPos.X, screenPos.Y), color);
            }

            _renderEngineComponent.CommonSpriteBatch.End();
        }

        /// <summary>
        /// returns a string filled with status info of the gizmo component. (includes: mode/space/snapping/precision/pivot)
        /// </summary>
        /// <returns></returns>
        #endregion



        #region Event Triggers
        public event TransformationEventHandler TranslateEvent;
        public event TransformationEventHandler RotateEvent;
        public event TransformationEventHandler ScaleEvent;

        public event TransformationStartDelegate StartEvent;
        public event TransformationStopDelegate StopEvent;

        private void OnTranslateEvent(ITransformable transformable, Vector3 delta)
        {
            TranslateEvent?.Invoke(transformable, new TransformationEventArgs(delta, ActivePivot));
        }

        private void OnRotateEvent(ITransformable transformable, Matrix delta)
        {
            RotateEvent?.Invoke(transformable, new TransformationEventArgs(delta, ActivePivot));
        }

        private void OnScaleEvent(ITransformable transformable, Vector3 delta)
        {
            ScaleEvent?.Invoke(transformable, new TransformationEventArgs(delta, ActivePivot));
        }

        #endregion

        #region Helper Functions
        public void ToggleActiveSpace()
        {
            GizmoDisplaySpace = GizmoDisplaySpace == TransformSpace.Local ? TransformSpace.World : TransformSpace.Local;
        }

        public void Dispose()
        {
            _lineEffect.Dispose();
            _meshEffect.Dispose();
        }


        #endregion
    }


    #region Gizmo EventHandlers

    public class TransformationEventArgs
    {
        public ValueType Value;
        public PivotType Pivot;
        public TransformationEventArgs(ValueType value, PivotType pivot)
        {
            Value = value;
            Pivot = pivot;
        }
    }
    public delegate void TransformationStartDelegate();
    public delegate void TransformationStopDelegate();
    public delegate void TransformationEventHandler(ITransformable transformable, TransformationEventArgs e);

    #endregion

    #region Gizmo Enums

    public enum GizmoAxis
    {
        X,
        Y,
        Z,
        None
    }

    public enum GizmoMode
    {
        Translate,
        Rotate,
        NonUniformScale,
        UniformScale
    }

    public enum TransformSpace
    {
        Local,
        World
    }

    public enum PivotType
    {
        ObjectCenter,
        SelectionCenter,
        WorldOrigin
    }

    #endregion
}
