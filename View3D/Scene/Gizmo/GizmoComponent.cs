using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;
using View3D.Commands;
using View3D.Input;
using View3D.Rendering;
using View3D.Scene;

namespace View3D.Scene.Gizmo
{
    public delegate void GizmoUpdated();

    public class GizmoComponent : IGameComponent, IUpdateable, IDrawable
    {

        GraphicsArgs _graphicArgs;
        InputSystems _input;

        SelectionManager _selectionManager;
        CommandManager _commandManager;

        public event EventHandler<EventArgs> EnabledChanged;
        public event EventHandler<EventArgs> UpdateOrderChanged;
        public event EventHandler<EventArgs> DrawOrderChanged;
        public event EventHandler<EventArgs> VisibleChanged;

        public bool Enabled => true;
        public int UpdateOrder => (int)UpdateOrderEnum.Gizmo;
        public int DrawOrder => (int)DrawOrderEnum.Gizmo;
        public bool Visible => true;


        Gizmo _gizmo;
        SpriteBatch _spriteBatch;

        TransformCommand _activeCommand;

        public GizmoComponent(GraphicsArgs graphicArgs, InputSystems input, SelectionManager selectionManager, CommandManager commandManager)
        {
            _commandManager = commandManager;
            _graphicArgs = graphicArgs;
            _input = input;
            _selectionManager = selectionManager;
            _selectionManager.SelectionChanged += OnSelectionChanged;

            var font = graphicArgs.ResourceLibary.XnaContentManager.Load<SpriteFont>("Fonts\\DefaultFont");
            _spriteBatch = new SpriteBatch(graphicArgs.GraphicsDevice);
            _gizmo = new Gizmo(graphicArgs.Camera, _input.Mouse, graphicArgs.GraphicsDevice, _spriteBatch, font);

            _gizmo.TranslateEvent += GizmoTranslateEvent;
            _gizmo.RotateEvent += GizmoRotateEvent;
            _gizmo.ScaleEvent += GizmoScaleEvent;
            _gizmo.StartEvent += GizmoTransformStart;
            _gizmo.StopEvent += GizmoTransformEnd;
        }

        private void GizmoTransformStart()
        {
            _activeCommand = new TransformCommand(_selectionManager.CurrentSelection());
        }

        private void GizmoTransformEnd()
        {
            if (_activeCommand != null)
            {
                _commandManager.ExecuteCommand(_activeCommand);
                _activeCommand = null;
                _input.Mouse.ClearStates();
            }
        }


        private void OnSelectionChanged(System.Collections.Generic.IEnumerable<Rendering.RenderItem> items)
        {
            _gizmo.Selection.Clear();
            foreach (var item in items)
                _gizmo.Selection.Add(item);

            _gizmo.ResetDeltas();
        }

        public void Initialize()
        { 
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

        public void Update(GameTime gameTime)
        {
            _gizmo.UpdateCameraProperties();

            // Toggle transform mode:
            if (_input.Keyboard.IsKeyReleased(Keys.R))
                _gizmo.ActiveMode = GizmoMode.Rotate;

            if (_input.Keyboard.IsKeyReleased(Keys.T))
                _gizmo.ActiveMode = GizmoMode.Translate;

            if (_input.Keyboard.IsKeyReleased(Keys.Y))
                _gizmo.ActiveMode = GizmoMode.NonUniformScale;

            // Toggle space mode:
            if (_input.Keyboard.IsKeyReleased(Keys.Home))
                _gizmo.ToggleActiveSpace();

            // Workaround for camera roation causing movment
            if (!_input.Keyboard.IsKeyDown(Keys.LeftAlt))
                _gizmo.Update(gameTime);
        }

        public void Draw(GameTime gameTime)
        {
            _gizmo.Draw(false);
        }
    }
}
