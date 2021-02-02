using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using Serilog;
using Common;

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

namespace View3D.Scene.Gizmo
{
    public class Gizmo
    {
        ILogger _logger = Logging.Create<Gizmo>();

        /// <summary>
        /// only active if atleast one entity is selected.
        /// </summary>
        private bool _isActive = true;

        /// <summary>
        /// Enabled if gizmo should be able to select objects and axis.
        /// </summary>
        public bool Enabled { get; set; }

        private readonly GraphicsDevice _graphics;
        private readonly SpriteBatch _spriteBatch;
        private readonly BasicEffect _lineEffect;
        private readonly BasicEffect _meshEffect;
        private readonly SpriteFont _font;

        private Matrix _view = Matrix.Identity;
        private Matrix _projection = Matrix.Identity;
        private Vector3 _cameraPosition;

        // -- Screen Scale -- //
        private Matrix _screenScaleMatrix;
        private float _screenScale;

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

        // -- Quads -- //
        private Quad[] _quads;
        private readonly BasicEffect _quadEffect;

        // -- Colors -- //
        private Color[] _axisColors;
        private Color _highlightColor;

        // -- UI Text -- //
        private string[] _axisText;
        private Vector3 _axisTextOffset = new Vector3(0, 0.5f, 0);

        // -- Modes & Selections -- //
        public GizmoAxis ActiveAxis = GizmoAxis.None;
        public GizmoMode ActiveMode = GizmoMode.Translate;
        public TransformSpace GizmoDisplaySpace = TransformSpace.Local;
        public TransformSpace GizmoValueSpace = TransformSpace.Local;
        public PivotType ActivePivot = PivotType.SelectionCenter;

        // -- BoundingBoxes -- //

        #region BoundingBoxes

        private const float MULTI_AXIS_THICKNESS = 0.05f;
        private const float SINGLE_AXIS_THICKNESS = 0.2f;

        private static BoundingBox XAxisBox
        {
            get
            {
                return new BoundingBox(new Vector3(LINE_OFFSET, 0, 0),
                                       new Vector3(LINE_OFFSET + LINE_LENGTH, SINGLE_AXIS_THICKNESS, SINGLE_AXIS_THICKNESS));
            }
        }

        private static BoundingBox YAxisBox
        {
            get
            {
                return new BoundingBox(new Vector3(0, LINE_OFFSET, 0),
                                       new Vector3(SINGLE_AXIS_THICKNESS, LINE_OFFSET + LINE_LENGTH, SINGLE_AXIS_THICKNESS));
            }
        }

        private static BoundingBox ZAxisBox
        {
            get
            {
                return new BoundingBox(new Vector3(0, 0, LINE_OFFSET),
                                       new Vector3(SINGLE_AXIS_THICKNESS, SINGLE_AXIS_THICKNESS, LINE_OFFSET + LINE_LENGTH));
            }
        }

        private static BoundingBox XZAxisBox
        {
            get
            {
                return new BoundingBox(Vector3.Zero,
                                       new Vector3(LINE_OFFSET, MULTI_AXIS_THICKNESS, LINE_OFFSET));
            }
        }

        private BoundingBox XYBox
        {
            get
            {
                return new BoundingBox(Vector3.Zero,
                                       new Vector3(LINE_OFFSET, LINE_OFFSET, MULTI_AXIS_THICKNESS));
            }
        }

        private BoundingBox YZBox
        {
            get
            {
                return new BoundingBox(Vector3.Zero,
                                       new Vector3(MULTI_AXIS_THICKNESS, LINE_OFFSET, LINE_OFFSET));
            }
        }

        #endregion

        // -- BoundingSpheres -- //

        #region BoundingSpheres

        private const float RADIUS = 1f;

        private BoundingSphere XSphere
        {
            get
            {
                return new BoundingSphere(Vector3.Transform(_translationLineVertices[1].Position, _gizmoWorld),
                                          RADIUS * _screenScale);
            }
        }

        private BoundingSphere YSphere
        {
            get
            {
                return new BoundingSphere(Vector3.Transform(_translationLineVertices[7].Position, _gizmoWorld),
                                          RADIUS * _screenScale);
            }
        }

        private BoundingSphere ZSphere
        {
            get
            {
                return new BoundingSphere(Vector3.Transform(_translationLineVertices[13].Position, _gizmoWorld),
                                          RADIUS * _screenScale);
            }
        }

        #endregion

        /// <summary>
        /// The value to adjust all transformation when precisionMode is active.
        /// </summary>
        private const float PRECISION_MODE_SCALE = 0.1f;

        // -- Selection -- //
        public List<ITransformable> Selection = new List<ITransformable>();

        private Vector3 _translationDelta = Vector3.Zero;
        private Matrix _rotationDelta = Matrix.Identity;
        private Vector3 _scaleDelta = Vector3.Zero;

        // -- Translation Variables -- //
        private Vector3 _tDelta;
        private Vector3 _lastIntersectionPosition;
        private Vector3 _intersectPosition;

        public bool SnapEnabled = false;
        public bool PrecisionModeEnabled;
        public float TranslationSnapValue = 5;
        public float RotationSnapValue = 30;
        public float ScaleSnapValue = 0.5f;

        private Vector3 _translationScaleSnapDelta;
        private float _rotationSnapDelta;

       // private MouseState _lastMouseState, _currentMouseState;
        ArcBallCamera _camera;
        Input.MouseComponent _mouse;


        public Gizmo(ArcBallCamera camera, Input.MouseComponent mouse, GraphicsDevice graphics, SpriteBatch spriteBatch, SpriteFont font)
        {
            SceneWorld = Matrix.Identity;
            _graphics = graphics;
            _spriteBatch = spriteBatch;
            _font = font;
            _camera = camera;
            _mouse = mouse;

            Enabled = true;

            _lineEffect = new BasicEffect(graphics) { VertexColorEnabled = true, AmbientLightColor = Vector3.One, EmissiveColor = Vector3.One };
            _meshEffect = new BasicEffect(graphics);
            _quadEffect = new BasicEffect(graphics) { World = Matrix.Identity, DiffuseColor = _highlightColor.ToVector3(), Alpha = 0.5f };
            _quadEffect.EnableDefaultLighting();

            Initialize();
        }

        private void Initialize()
        {
            // -- Set local-space offset -- //
            _modelLocalSpace = new Matrix[3];
            _modelLocalSpace[0] = Matrix.CreateWorld(new Vector3(LINE_LENGTH, 0, 0), Vector3.Left, Vector3.Up);
            _modelLocalSpace[1] = Matrix.CreateWorld(new Vector3(0, LINE_LENGTH, 0), Vector3.Down, Vector3.Left);
            _modelLocalSpace[2] = Matrix.CreateWorld(new Vector3(0, 0, LINE_LENGTH), Vector3.Forward, Vector3.Up);

            _axisColors = new Color[3] { Color.Red, Color.Green, Color.Blue};
            _highlightColor = Color.Gold;
            _axisText = new string[3] { "X", "Y", "Z"};

            // translucent quads

            #region Translucent Quads

            const float halfLineOffset = LINE_OFFSET / 2;
            _quads = new Quad[3];
            _quads[0] = new Quad(new Vector3(halfLineOffset, halfLineOffset, 0), Vector3.Backward, Vector3.Up, LINE_OFFSET,
                                 LINE_OFFSET); //XY
            _quads[1] = new Quad(new Vector3(halfLineOffset, 0, halfLineOffset), Vector3.Up, Vector3.Right, LINE_OFFSET,
                                 LINE_OFFSET); //XZ
            _quads[2] = new Quad(new Vector3(0, halfLineOffset, halfLineOffset), Vector3.Right, Vector3.Up, LINE_OFFSET,
                                 LINE_OFFSET); //ZY 

            #endregion

            // fill array with vertex-data

            #region Fill Axis-Line array

            var vertexList = new List<VertexPositionColor>(18);

            // helper to apply colors
            Color xColor = _axisColors[0];
            Color yColor = _axisColors[1];
            Color zColor = _axisColors[2];


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

            #endregion
        }



        public void NextPivotType()
        {
            if (ActivePivot == PivotType.WorldOrigin)
                ActivePivot = PivotType.ObjectCenter;
            else
                ActivePivot++;
        }


        public void ResetDeltas()
        {
            _tDelta = Vector3.Zero;
            _lastIntersectionPosition = Vector3.Zero;
            _intersectPosition = Vector3.Zero;
        }

        public void Update(GameTime gameTime)
        {
            Vector2 mousePosition = _mouse.Position();

            if (_isActive)
            {
                _lastIntersectionPosition = _intersectPosition;
                var rotationMatrixLocal = Matrix.Identity;
                var translationVectorLocal = Vector3.Zero;
                
                if (_mouse.IsMouseButtonPressed(Input.MouseButton.Left) && ActiveAxis != GizmoAxis.None)
                {
                    if (_mouse.LastState().LeftButton == ButtonState.Released)
                        StartEvent?.Invoke();

                    switch (ActiveMode)
                    {
                        case GizmoMode.UniformScale:
                        case GizmoMode.NonUniformScale:
                        case GizmoMode.Translate:
                            translationVectorLocal = HandleTranslateAndScale(mousePosition, translationVectorLocal);
                            break;
                        case GizmoMode.Rotate:
                            rotationMatrixLocal = HandleRotation(gameTime);
                            break;
                    }
                }
                else
                {
                    if (_mouse.LastState().LeftButton == ButtonState.Pressed && _mouse.State().LeftButton == ButtonState.Released)
                        StopEvent?.Invoke();

                    ResetDeltas();
                    if (_mouse.State().LeftButton == ButtonState.Released && _mouse.State().RightButton == ButtonState.Released)
                        SelectAxis(mousePosition);
                }

                SetGizmoPosition();

                // -- Trigger Translation, Rotation & Scale events -- //
                if (_mouse.IsMouseButtonPressed(Input.MouseButton.Left))
                {
                    if (_translationDelta != Vector3.Zero)
                    {
                        foreach (var entity in Selection)
                            OnTranslateEvent(entity, _translationDelta, translationVectorLocal);
                        _translationDelta = Vector3.Zero;
                    }
                    if (_rotationDelta != Matrix.Identity)
                    {
                        foreach (var entity in Selection)
                            OnRotateEvent(entity, _rotationDelta, rotationMatrixLocal);
                        _rotationDelta = Matrix.Identity;
                    }
                    if (_scaleDelta != Vector3.Zero)
                    {
                        foreach (var entity in Selection)
                            OnScaleEvent(entity, _scaleDelta);
                        _scaleDelta = Vector3.Zero;
                    }
                }
            }


            if (Selection.Count < 1)
            {
                _isActive = false;
                ActiveAxis = GizmoAxis.None;
                return;
            }
            // helps solve visual lag (1-frame-lag) after selecting a new entity
            if (!_isActive)
                SetGizmoPosition();

            _isActive = true;

            // -- Scale Gizmo to fit on-screen -- //
            Vector3 vLength = _cameraPosition - _position;
            const float scaleFactor = 25;

            _screenScale = vLength.Length() / scaleFactor;
            _screenScaleMatrix = Matrix.CreateScale(new Vector3(_screenScale));

            _localForward = Selection[0].Forward;
            _localUp = Selection[0].Up;
            // -- Vector Rotation (Local/World) -- //
            _localForward.Normalize();
            _localRight = Vector3.Cross(_localForward, _localUp);
            _localUp = Vector3.Cross(_localRight, _localForward);
            _localRight.Normalize();
            _localUp.Normalize();

            // -- Create Both World Matrices -- //
            _objectOrientedWorld = _screenScaleMatrix * Matrix.CreateWorld(_position, _localForward, _localUp);
            _axisAlignedWorld = _screenScaleMatrix * Matrix.CreateWorld(_position, SceneWorld.Forward, SceneWorld.Up);

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

        private Vector3 HandleTranslateAndScale(Vector2 mousePosition, Vector3 translationVectorLocal)
        {
            Vector3 delta = Vector3.Zero;
            Ray ray = _camera.CreateCameraRay(mousePosition); 

            Matrix transform = Matrix.Invert(_rotationMatrix);
            ray.Position = Vector3.Transform(ray.Position, transform);
            ray.Direction = Vector3.TransformNormal(ray.Direction, transform);
            switch (ActiveAxis)
            {
                case GizmoAxis.XY:
                case GizmoAxis.X:
                    {
                        Plane plane = new Plane(Vector3.Forward,
                                                Vector3.Transform(_position, Matrix.Invert(_rotationMatrix)).Z);

                        float? intersection = ray.Intersects(plane);
                        if (intersection.HasValue)
                        {
                            _intersectPosition = (ray.Position + (ray.Direction * intersection.Value));
                            if (_lastIntersectionPosition != Vector3.Zero)
                            {
                                _tDelta = _intersectPosition - _lastIntersectionPosition;
                            }
                            delta = ActiveAxis == GizmoAxis.X
                                      ? new Vector3(_tDelta.X, 0, 0)
                                      : new Vector3(_tDelta.X, _tDelta.Y, 0);
                        }
                    }
                    break;
                case GizmoAxis.Z:
                case GizmoAxis.YZ:
                case GizmoAxis.Y:
                    {
                        Plane plane = new Plane(Vector3.Left, Vector3.Transform(_position, Matrix.Invert(_rotationMatrix)).X);

                        float? intersection = ray.Intersects(plane);
                        if (intersection.HasValue)
                        {
                            _intersectPosition = (ray.Position + (ray.Direction * intersection.Value));
                            if (_lastIntersectionPosition != Vector3.Zero)
                            {
                                _tDelta = _intersectPosition - _lastIntersectionPosition;
                            }
                            switch (ActiveAxis)
                            {
                                case GizmoAxis.Y:
                                    delta = new Vector3(0, _tDelta.Y, 0);
                                    break;
                                case GizmoAxis.Z:
                                    delta = new Vector3(0, 0, _tDelta.Z);
                                    break;
                                default:
                                    delta = new Vector3(0, _tDelta.Y, _tDelta.Z);
                                    break;
                            }
                        }
                    }
                    break;
                case GizmoAxis.ZX:
                    {
                        Plane plane = new Plane(Vector3.Down, Vector3.Transform(_position, Matrix.Invert(_rotationMatrix)).Y);

                        float? intersection = ray.Intersects(plane);
                        if (intersection.HasValue)
                        {
                            _intersectPosition = (ray.Position + (ray.Direction * intersection.Value));
                            if (_lastIntersectionPosition != Vector3.Zero)
                            {
                                _tDelta = _intersectPosition - _lastIntersectionPosition;
                            }
                        }
                        delta = new Vector3(_tDelta.X, 0, _tDelta.Z);
                    }
                    break;
            }


            if (SnapEnabled)
            {
                float snapValue = TranslationSnapValue;
                if (ActiveMode == GizmoMode.UniformScale || ActiveMode == GizmoMode.NonUniformScale)
                    snapValue = ScaleSnapValue;
                if (PrecisionModeEnabled)
                {
                    delta *= PRECISION_MODE_SCALE;
                    snapValue *= PRECISION_MODE_SCALE;
                }

                _translationScaleSnapDelta += delta;

                delta = new Vector3(
                  (int)(_translationScaleSnapDelta.X / snapValue) * snapValue,
                  (int)(_translationScaleSnapDelta.Y / snapValue) * snapValue,
                  (int)(_translationScaleSnapDelta.Z / snapValue) * snapValue);

                _translationScaleSnapDelta -= delta;
            }
            else if (PrecisionModeEnabled)
                delta *= PRECISION_MODE_SCALE;


            if (ActiveMode == GizmoMode.Translate)
            {
                // transform (local or world)
                var tempDelta = Vector3.Transform(delta, _rotationMatrix);
                translationVectorLocal = Vector3.Transform(delta, SceneWorld);
                delta = tempDelta;
                _translationDelta = delta;
            }
            else if (ActiveMode == GizmoMode.NonUniformScale || ActiveMode == GizmoMode.UniformScale)
            {
                // -- Apply Scale -- //
                _scaleDelta += delta;
            }
            return translationVectorLocal;
        }

        private Matrix HandleRotation(GameTime gameTime)
        {
            Matrix rotationMatrixLocal;

            float delta = _mouse.DeltaPosition().X;
            delta *= (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (SnapEnabled)
            {
                float snapValue = MathHelper.ToRadians(RotationSnapValue);
                if (PrecisionModeEnabled)
                {
                    delta *= PRECISION_MODE_SCALE;
                    snapValue *= PRECISION_MODE_SCALE;
                }

                _rotationSnapDelta += delta;

                float snapped = (int)(_rotationSnapDelta / snapValue) * snapValue;
                _rotationSnapDelta -= snapped;

                delta = snapped;
            }
            else if (PrecisionModeEnabled)
                delta *= PRECISION_MODE_SCALE;

            // rotation matrix to transform - if more than one objects selected, always use world-space.
            Matrix rot = Matrix.Identity;
            rot.Forward = SceneWorld.Forward;
            rot.Up = SceneWorld.Up;
            rot.Right = SceneWorld.Right;

            Matrix rot2 = Matrix.Identity;
            rot2.Forward = SceneWorld.Forward;
            rot2.Up = SceneWorld.Up;
            rot2.Right = SceneWorld.Right;
            //delta = 0;
            switch (ActiveAxis)
            {
                case GizmoAxis.X:
                    rot *= Matrix.CreateFromAxisAngle(_rotationMatrix.Right, delta);
                    rot2 *= Matrix.CreateFromAxisAngle(SceneWorld.Right, delta);
                    break;
                case GizmoAxis.Y:
                    rot *= Matrix.CreateFromAxisAngle(_rotationMatrix.Up, delta);
                    rot2 *= Matrix.CreateFromAxisAngle(SceneWorld.Up, delta);
                    break;
                case GizmoAxis.Z:
                    rot *= Matrix.CreateFromAxisAngle(_rotationMatrix.Forward, delta);
                    rot2 *= Matrix.CreateFromAxisAngle(SceneWorld.Forward, delta);
                    break;
            }
            _rotationDelta = rot;
            rotationMatrixLocal = rot2;
            return rotationMatrixLocal;
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
                        case GizmoAxis.XY:
                            ApplyLineColor(0, 4, color);
                            ApplyLineColor(6, 4, color);
                            break;
                        case GizmoAxis.YZ:
                            ApplyLineColor(6, 2, color);
                            ApplyLineColor(12, 2, color);
                            ApplyLineColor(10, 2, color);
                            ApplyLineColor(16, 2, color);
                            break;
                        case GizmoAxis.ZX:
                            ApplyLineColor(0, 2, color);
                            ApplyLineColor(4, 2, color);
                            ApplyLineColor(12, 4, color);
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

        /// <summary>
        /// Apply color on the lines associated with translation mode (re-used in Scale)
        /// </summary>
        private void ApplyLineColor(int startindex, int count, Color color)
        {
            for (int i = startindex; i < (startindex + count); i++)
            {
                _translationLineVertices[i].Color = color;
            }
        }

        /// <summary>
        /// Per-frame check to see if mouse is hovering over any axis.
        /// </summary>
        private void SelectAxis(Vector2 mousePosition)
        {
            if (!Enabled)
                return;

            float closestintersection = float.MaxValue;
            Ray ray = _camera.CreateCameraRay(mousePosition);

            if (ActiveMode == GizmoMode.Translate)
            {
                // transform ray into local-space of the boundingboxes.
                ray.Direction = Vector3.TransformNormal(ray.Direction, Matrix.Invert(_gizmoWorld));
                ray.Position = Vector3.Transform(ray.Position, Matrix.Invert(_gizmoWorld));
            }

            #region X,Y,Z Boxes
            float? intersection = XAxisBox.Intersects(ray);
            if (intersection.HasValue)
                if (intersection.Value < closestintersection)
                {
                    ActiveAxis = GizmoAxis.X;
                    closestintersection = intersection.Value;
                }
            intersection = YAxisBox.Intersects(ray);
            if (intersection.HasValue)
            {
                if (intersection.Value < closestintersection)
                {
                    ActiveAxis = GizmoAxis.Y;
                    closestintersection = intersection.Value;
                }
            }
            intersection = ZAxisBox.Intersects(ray);
            if (intersection.HasValue)
            {
                if (intersection.Value < closestintersection)
                {
                    ActiveAxis = GizmoAxis.Z;
                    closestintersection = intersection.Value;
                }
            }
            #endregion

            if (ActiveMode == GizmoMode.Rotate || ActiveMode == GizmoMode.UniformScale ||
                ActiveMode == GizmoMode.NonUniformScale)
            {
                #region BoundingSpheres

                intersection = XSphere.Intersects(ray);
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

                #endregion
            }
            if (ActiveMode == GizmoMode.Translate || ActiveMode == GizmoMode.NonUniformScale ||
                ActiveMode == GizmoMode.UniformScale)
            {
                // if no axis was hit (x,y,z) set value to lowest possible to select the 'farthest' intersection for the XY,XZ,YZ boxes. 
                // This is done so you may still select multi-axis if you're looking at the gizmo from behind!
                if (closestintersection >= float.MaxValue)
                    closestintersection = float.MinValue;

                #region BoundingBoxes
                intersection = XYBox.Intersects(ray);
                if (intersection.HasValue)
                    if (intersection.Value > closestintersection)
                    {
                        ActiveAxis = GizmoAxis.XY;
                        closestintersection = intersection.Value;
                    }
                intersection = XZAxisBox.Intersects(ray);
                if (intersection.HasValue)
                    if (intersection.Value > closestintersection)
                    {
                        ActiveAxis = GizmoAxis.ZX;
                        closestintersection = intersection.Value;
                    }
                intersection = YZBox.Intersects(ray);
                if (intersection.HasValue)
                    if (intersection.Value > closestintersection)
                    {
                        ActiveAxis = GizmoAxis.YZ;
                        closestintersection = intersection.Value;
                    }
                #endregion
            }
            if (closestintersection >= float.MaxValue || closestintersection <= float.MinValue)
                ActiveAxis = GizmoAxis.None;
        }


        /// <summary>
        /// Set position of the gizmo, position will be center of all selected entities.
        /// </summary>
        private void SetGizmoPosition()
        {
            switch (ActivePivot)
            {
                case PivotType.ObjectCenter:
                    if (Selection.Count > 0)
                        _position = Selection[0].Position;
                    break;
                case PivotType.SelectionCenter:
                    _position = GetSelectionCenter();
                    break;
                case PivotType.WorldOrigin:
                    _position = SceneWorld.Translation;
                    break;
            }
            _position += _translationDelta;
        }

        /// <summary>
        /// Returns center position of all selected objectes.
        /// </summary>
        /// <returns></returns>
        private Vector3 GetSelectionCenter()
        {
            if (Selection.Count == 0)
                return Vector3.Zero;

            Vector3 center = Vector3.Zero;
            foreach (var selected in Selection)
                center += selected.Position;
            return center / Selection.Count;
        }

        public void UpdateCameraProperties()
        {
            _view = _camera.ViewMatrix;
            _projection = _camera.ProjectionMatrix;
            _cameraPosition = _camera.Position;
        }

        #region Draw
        public void Draw(bool drawHelperText)
        {
            if (!_isActive) return;
            _graphics.DepthStencilState = DepthStencilState.None;

            if (_view == Matrix.Identity || _projection == Matrix.Identity)
                throw new Exception("Error: Must call .UpdateCameraProperties() before .Draw()");

            #region Draw: Axis-Lines

            // -- Draw Lines -- //
            _lineEffect.World = _gizmoWorld;
            _lineEffect.View = _view;
            _lineEffect.Projection = _projection;

            _lineEffect.CurrentTechnique.Passes[0].Apply();
            _graphics.DrawUserPrimitives(PrimitiveType.LineList, _translationLineVertices, 0,
                                         _translationLineVertices.Length / 2);

            #endregion

            switch (ActiveMode)
            {
                case GizmoMode.NonUniformScale:
                case GizmoMode.Translate:
                    switch (ActiveAxis)
                    {
                        #region Draw Quads
                        case GizmoAxis.ZX:
                        case GizmoAxis.YZ:
                        case GizmoAxis.XY:
                            {
                                _graphics.BlendState = BlendState.AlphaBlend;
                                _graphics.RasterizerState = RasterizerState.CullNone;

                                _quadEffect.World = _gizmoWorld;
                                _quadEffect.View = _view;
                                _quadEffect.Projection = _projection;

                                _quadEffect.CurrentTechnique.Passes[0].Apply();

                                Quad activeQuad = new Quad();
                                switch (ActiveAxis)
                                {
                                    case GizmoAxis.XY:
                                        activeQuad = _quads[0];
                                        break;
                                    case GizmoAxis.ZX:
                                        activeQuad = _quads[1];
                                        break;
                                    case GizmoAxis.YZ:
                                        activeQuad = _quads[2];
                                        break;
                                }

                                _graphics.DrawUserIndexedPrimitives(PrimitiveType.TriangleList,
                                                                    activeQuad.Vertices, 0, 4,
                                                                    activeQuad.Indexes, 0, 2);

                                _graphics.BlendState = BlendState.Opaque;
                                _graphics.RasterizerState = RasterizerState.CullCounterClockwise;
                            }
                            break;
                        #endregion
                    }
                    break;
                case GizmoMode.UniformScale:
                    #region Draw Quads
                    if (ActiveAxis != GizmoAxis.None)
                    {
                        _graphics.BlendState = BlendState.AlphaBlend;
                        _graphics.RasterizerState = RasterizerState.CullNone;

                        _quadEffect.World = _gizmoWorld;
                        _quadEffect.View = _view;
                        _quadEffect.Projection = _projection;
                        _quadEffect.CurrentTechnique.Passes[0].Apply();

                        for (int i = 0; i < _quads.Length; i++)
                            _graphics.DrawUserIndexedPrimitives(PrimitiveType.TriangleList,
                                                                _quads[i].Vertices, 0, 4,
                                                                _quads[i].Indexes, 0, 2);
                        _graphics.BlendState = BlendState.Opaque;
                        _graphics.RasterizerState = RasterizerState.CullCounterClockwise;
                    }
                    #endregion
                    break;
            }

            // draw the 3d meshes
            for (int i = 0; i < 3; i++) //(order: x, y, z)
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
                _meshEffect.View = _view;
                _meshEffect.Projection = _projection;

                _meshEffect.DiffuseColor = color;
                _meshEffect.EmissiveColor = color;

                _meshEffect.CurrentTechnique.Passes[0].Apply();

                _graphics.DrawUserIndexedPrimitives(PrimitiveType.TriangleList,
                    activeModel.Vertices, 0, activeModel.Vertices.Length,
                    activeModel.Indices, 0, activeModel.Indices.Length / 3);
            }

            _graphics.DepthStencilState = DepthStencilState.Default;
            Draw2D();
        }

        private void Draw2D()
        {
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            // -- Draw Axis identifiers ("X,Y,Z") -- // 
            for (int i = 0; i < 3; i++)
            {
                Vector3 screenPos =
                  _graphics.Viewport.Project(_modelLocalSpace[i].Translation + _modelLocalSpace[i].Backward + _axisTextOffset,
                                             _projection, _view, _gizmoWorld);

                if (screenPos.Z < 0f || screenPos.Z > 1.0f)
                    continue;

                Color color = _axisColors[i];
                switch (i)
                {
                    case 0:
                        if (ActiveAxis == GizmoAxis.X || ActiveAxis == GizmoAxis.XY || ActiveAxis == GizmoAxis.ZX)
                            color = _highlightColor;
                        break;
                    case 1:
                        if (ActiveAxis == GizmoAxis.Y || ActiveAxis == GizmoAxis.XY || ActiveAxis == GizmoAxis.YZ)
                            color = _highlightColor;
                        break;
                    case 2:
                        if (ActiveAxis == GizmoAxis.Z || ActiveAxis == GizmoAxis.YZ || ActiveAxis == GizmoAxis.ZX)
                            color = _highlightColor;
                        break;
                }

                _spriteBatch.DrawString(_font, _axisText[i], new Vector2(screenPos.X, screenPos.Y), color);
            }

            // -- Draw StatusInfo -- //
            //string statusInfo = GetStatusInfo();
            //Vector2 stringDims = _font.MeasureString(statusInfo);
            //Vector2 position = new Vector2(_graphics.Viewport.Width - stringDims.X, _graphics.Viewport.Height - stringDims.Y);
            //
            //_spriteBatch.DrawString(_font, statusInfo, position, Color.White);
            _spriteBatch.End();
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


        private void OnTranslateEvent(ITransformable transformable, Vector3 delta, Vector3 delta2)
        {
            TranslateEvent?.Invoke(transformable, new TransformationEventArgs(delta, delta2));
        }

        private void OnRotateEvent(ITransformable transformable, Matrix delta, Matrix deleta2)
        {
      
            RotateEvent?.Invoke(transformable, new TransformationEventArgs(delta, deleta2));
        }

        private void OnScaleEvent(ITransformable transformable, Vector3 delta)
        {
            ScaleEvent?.Invoke(transformable, new TransformationEventArgs(delta));
        }

        #endregion

        #region Private Quad Struct

        private struct Quad
        {
            public Vector3 Origin;
            public Vector3 UpperLeft;
            public Vector3 LowerLeft;
            public Vector3 UpperRight;
            public Vector3 LowerRight;
            public Vector3 Normal;
            public Vector3 Up;
            public Vector3 Left;

            public VertexPositionNormalTexture[] Vertices;
            public short[] Indexes;

            public Quad(Vector3 origin, Vector3 normal, Vector3 up,
                        float width, float height)
            {
                Vertices = new VertexPositionNormalTexture[4];
                Indexes = new short[6];
                Origin = origin;
                Normal = normal;
                Up = up;

                // Calculate the quad corners
                Left = Vector3.Cross(normal, Up);
                Vector3 uppercenter = (Up * height / 2) + origin;
                UpperLeft = uppercenter + (Left * width / 2);
                UpperRight = uppercenter - (Left * width / 2);
                LowerLeft = UpperLeft - (Up * height);
                LowerRight = UpperRight - (Up * height);

                FillVertices();
            }

            private void FillVertices()
            {
                // Fill in texture coordinates to display full texture
                // on quad
                Vector2 textureUpperLeft = new Vector2(0.0f, 0.0f);
                Vector2 textureUpperRight = new Vector2(1.0f, 0.0f);
                Vector2 textureLowerLeft = new Vector2(0.0f, 1.0f);
                Vector2 textureLowerRight = new Vector2(1.0f, 1.0f);

                // Provide a normal for each vertex
                for (int i = 0; i < Vertices.Length; i++)
                {
                    Vertices[i].Normal = Normal;
                }

                // Set the position and texture coordinate for each
                // vertex
                Vertices[0].Position = LowerLeft;
                Vertices[0].TextureCoordinate = textureLowerLeft;
                Vertices[1].Position = UpperLeft;
                Vertices[1].TextureCoordinate = textureUpperLeft;
                Vertices[2].Position = LowerRight;
                Vertices[2].TextureCoordinate = textureLowerRight;
                Vertices[3].Position = UpperRight;
                Vertices[3].TextureCoordinate = textureUpperRight;

                // Set the index buffer for each vertex, using
                // clockwise winding
                Indexes[0] = 0;
                Indexes[1] = 1;
                Indexes[2] = 2;
                Indexes[3] = 2;
                Indexes[4] = 1;
                Indexes[5] = 3;
            }
        }
        #endregion

        #region Helper Functions
        public void ToggleActiveSpace()
        {
            GizmoDisplaySpace = GizmoDisplaySpace == TransformSpace.Local ? TransformSpace.World : TransformSpace.Local;
        }

        #endregion
    }

    // An enum of buttons on the mouse, since XNA doesn't provide one
    internal enum MouseButtons { Left, Right, Middle, X1, X2 };

    #region Gizmo EventHandlers

    public class TransformationEventArgs
    {
        public ValueType Value;
        public ValueType Value2;
        public TransformationEventArgs(ValueType value, ValueType value2 = null)
        {
            Value = value;
            Value2 = value2;
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
        XY,
        ZX,
        YZ,
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
