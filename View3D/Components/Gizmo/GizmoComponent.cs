using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Framework.WpfInterop;
using System;
using System.Linq;
using View3D.Commands;
using View3D.Rendering;
using View3D.Components.Component;
using System.Collections.Generic;
using View3D.Components.Rendering;
using View3D.Components.Input;
using View3D.Components.Component.Selection;
using View3D.Rendering.Geometry;
using View3D.Commands.Vertex;
using View3D.Utility;

namespace View3D.Components.Gizmo
{
    public class VertexTransformationWrapper : ITransformable
    {
        Vector3 _pos;
        public Vector3 Position { get=> _pos; set { _pos = value; HandlePositionUpdate(); } }

        Vector3 _scale = Vector3.One;
        public Vector3 Scale { get => _scale; set { _scale = value; HandlePositionUpdate(); } }

        Quaternion _orientation = Quaternion.Identity;
        public Quaternion Orientation { get => _orientation; set { _orientation = value; HandlePositionUpdate(); } }

        IGeometry _geo;
        List<int> _vertexList;


        Vector3 _startPos;
        Vector3 _startScale;
        Quaternion _startOrientation;

        public VertexTransformationWrapper(IGeometry geo, List<int> vertexList, Vector3 startPos)
        {
            _geo = geo;
            _vertexList = vertexList;
            _startPos = startPos;
            _pos = _startPos;

            _startScale = Scale;
            _startOrientation = Orientation;
        }

        void HandlePositionUpdate()
        {
            foreach (var vertexId in _vertexList)
            {
               //var posDif = (Position - _startPos);
               //var rotDif = Orientation - Quaternion.Inverse(_startOrientation);
               //var scaleDif = (Scale - _startScale);
               //var m = Matrix.CreateScale(Vector3.One + scaleDif) * Matrix.CreateFromQuaternion(rotDif) * Matrix.CreateTranslation(posDif);
                var dpos = (Position - _startPos) + _geo.GetVertexById(vertexId);
                //var newPos = Vector3.Transform(_geo.GetVertexById(vertexId), m);
                _geo.UpdateVertexPosition(vertexId, dpos);
            }

            _startPos = Position;
            _startScale = Scale;
            _startOrientation = Orientation;
            _geo.RebuildVertexBuffer();
        }
    }


    public delegate void GizmoUpdated();

    public class GizmoComponent : BaseComponent
    {
        MouseComponent _mouse;
        KeyboardComponent _keyboard;
        SelectionManager _selectionManager;

        ICommand _activeCommand;
        CommandExecutor _commandManager;
       
        Gizmo _gizmo;
        bool _isEnabled = false;

        public GizmoComponent(WpfGame game) : base(game)
        {
            UpdateOrder = (int)ComponentUpdateOrderEnum.Gizmo;
            DrawOrder = (int)ComponentDrawOrderEnum.Gizmo;
        }

        public override void Initialize()
        {
            _commandManager = GetComponent<CommandExecutor>();
            _selectionManager = GetComponent<SelectionManager>();
            _keyboard = GetComponent<KeyboardComponent>();
            _mouse = GetComponent<MouseComponent>();
            var camera = GetComponent<ArcBallCamera>();
            var resourceLibary = GetComponent<ResourceLibary>();

            _selectionManager.SelectionChanged += OnSelectionChanged;

            var font = resourceLibary.Content.Load<SpriteFont>("Fonts\\DefaultFont");
            _gizmo = new Gizmo(camera, _mouse, GraphicsDevice, new SpriteBatch(GraphicsDevice), font);

            _gizmo.TranslateEvent += GizmoTranslateEvent;
            _gizmo.RotateEvent += GizmoRotateEvent;
            _gizmo.ScaleEvent += GizmoScaleEvent;
            _gizmo.StartEvent += GizmoTransformStart;
            _gizmo.StopEvent += GizmoTransformEnd;
        }

        private void OnSelectionChanged(ISelectionState state)
        {
            _gizmo.Selection.Clear();

            if (state is ObjectSelectionState objectSelectionState)
            {
                foreach (ITransformable item in objectSelectionState.CurrentSelection().Where(x => x is ITransformable))
                    _gizmo.Selection.Add(item);
            }
            else if (state is VertexSelectionState vertexSelectionState)
            {
                var position = Vector3.Zero;
                for (int i = 0; i < vertexSelectionState.SelectedVertices.Count; i++)
                    position += vertexSelectionState.RenderObject.Geometry.GetVertexById(vertexSelectionState.SelectedVertices[i]);
                position = position / vertexSelectionState.SelectedVertices.Count;
                position = Vector3.Transform(position, vertexSelectionState.RenderObject.ModelMatrix);

                var wrapper = new VertexTransformationWrapper(vertexSelectionState.RenderObject.Geometry, vertexSelectionState.SelectedVertices, position);

                _gizmo.Selection.Add(wrapper);
            }

            _gizmo.ResetDeltas();
        }

        private void GizmoTransformStart()
        {
            if (_selectionManager.GetState() is ObjectSelectionState objectSelectionState)
            {
                _mouse.MouseOwner = this;

                var selection = objectSelectionState.CurrentSelection();
                _activeCommand = new TransformCommand(selection.Where(x => x is ITransformable).Select(x => (ITransformable)x).ToList());
            }
            if (_selectionManager.GetState() is VertexSelectionState vertexSelectionState)
            {
                _mouse.MouseOwner = this;
                _activeCommand = new TransformVertexCommand(vertexSelectionState.RenderObject);
            }
        }
        private void GizmoTransformEnd()
        {
            if (_activeCommand != null)
                _commandManager.ExecuteCommand(_activeCommand);

            _mouse.MouseOwner = null;
            _activeCommand = null;
        }


        private void GizmoTranslateEvent(ITransformable transformable, TransformationEventArgs e)
        {
            transformable.Position += (Vector3)e.Value;
        }  

        private void GizmoRotateEvent(ITransformable transformable, TransformationEventArgs e)
        {
            transformable.Orientation = Quaternion.CreateFromRotationMatrix(Matrix.CreateFromQuaternion(transformable.Orientation) * (Matrix)e.Value);
        }

        private void GizmoScaleEvent(ITransformable transformable, TransformationEventArgs e)
        {
            transformable.Scale += (Vector3)e.Value;
        }

        public override void Update(GameTime gameTime)
        {
            if ( !(_selectionManager.GetState().Mode == GeometrySelectionMode.Object || _selectionManager.GetState().Mode == GeometrySelectionMode.Vertex))
                return;

            if (!_isEnabled)
                return;

            _gizmo.UpdateCameraProperties();

            // Toggle space mode:
            if (_keyboard.IsKeyReleased(Keys.Home))
                _gizmo.ToggleActiveSpace();

            // Workaround for camera roation causing movment
            if (!_keyboard.IsKeyDown(Keys.LeftAlt))
                _gizmo.Update(gameTime);
        }

        public void SetGizmoMode(GizmoMode mode)
        {
            _gizmo.ActiveMode = mode;
            _isEnabled = true;
        }

        public void Disable()
        {
            _isEnabled = false;
        }

        public override void Draw(GameTime gameTime)
        {
            if (!(_selectionManager.GetState().Mode == GeometrySelectionMode.Object || _selectionManager.GetState().Mode == GeometrySelectionMode.Vertex))
                return;
            if (!_isEnabled)
                return;

            _gizmo.Draw(false);
        }
    }
}
